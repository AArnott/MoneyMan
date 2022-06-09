// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Nerdbank.MoneyManagement.ViewModels;
using PCLCommandBase;
using SQLite;
using Validation;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Manages the database that stores accounts, transactions, and other entities.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class MoneyFile : BindableBase, IDisposableObservable
{
	private const string TEMPTableModifier = "TEMP";

	/// <summary>
	/// The SQLite database connection.
	/// </summary>
	private readonly SQLiteConnection connection;

	/// <summary>
	/// The undo stack.
	/// </summary>
	private readonly Stack<(string SavepointName, string Activity, ISelectableView? ViewModel)> undoStack = new();

	private bool inUndoableTransaction;

	private long version;
	private (long Version, NetWorthQueryOptions Options)? lastRefreshedBalances;
	private AggregateData? aggregateData;

	/// <summary>
	/// Indicates whether a <see cref="TriggerAggregateDataRefresh"/> was deferred because we were in an undoable transaction
	/// and should be run at its conclusion.
	/// </summary>
	private bool pendingAggregateDataRefresh;

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFile"/> class.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	private MoneyFile(SQLiteConnection connection)
	{
		Requires.NotNull(connection, nameof(connection));
		this.connection = connection;

		this.CurrentConfiguration = this.connection.Table<Configuration>().First();
		this.Action = new TransactionOperations(this);
	}

	/// <summary>
	/// Occurs when one or more entities are inserted, updated, and/or removed.
	/// </summary>
	public event EventHandler<EntitiesChangedEventArgs>? EntitiesChanged;

	public enum EventType
	{
		/// <summary>
		/// Notifies of a SELECT query that was sent to the database.
		/// </summary>
		SelectQuery,

		/// <summary>
		/// Notifies of an INSERT query that was sent to the database.
		/// </summary>
		InsertQuery,

		/// <summary>
		/// Notifies of a DELETE query that was sent to the database.
		/// </summary>
		DeleteQuery,

		/// <summary>
		/// Notifies of an UPDATE query that was sent to the database.
		/// </summary>
		UpdateQuery,

		/// <summary>
		/// Carries an actual SQL query that was sent to the database.
		/// </summary>
		Sql,

		/// <summary>
		/// Writing a savepoint.
		/// </summary>
		Savepoint,

		/// <summary>
		/// Rolling back to a prior <see cref="Savepoint"/>.
		/// </summary>
		Rollback,

		/// <summary>
		/// Commits a transaction or prior savepoints.
		/// </summary>
		Commit,
	}

	public TraceSource TraceSource { get; } = new(nameof(MoneyFile));

	public string Path => this.connection.DatabasePath;

	/// <summary>
	/// Gets an object that describes aggregate data that may be expensive to calculate, such as net worth or account balances.
	/// </summary>
	public AggregateData? AggregateData
	{
		get
		{
			if (this.aggregateData is null)
			{
				this.TriggerAggregateDataRefresh();
			}

			return this.aggregateData;
		}

		private set
		{
			this.SetProperty(ref this.aggregateData, value);
		}
	}

	public TransactionOperations Action { get; }

	public TableQuery<Account> Accounts
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<Account>().Where(a => a.Type != Account.AccountType.Category);
		}
	}

	public TableQuery<Asset> Assets
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<Asset>();
		}
	}

	public TableQuery<AssetPrice> AssetPrices
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<AssetPrice>();
		}
	}

	public TableQuery<Transaction> Transactions
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<Transaction>();
		}
	}

	public TableQuery<TransactionEntry> TransactionEntries
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<TransactionEntry>();
		}
	}

	public TableQuery<Account> Categories
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<Account>().Where(a => a.Type == Account.AccountType.Category);
		}
	}

	public int PreferredAssetId => this.CurrentConfiguration.PreferredAssetId;

	public bool IsDisposed => this.connection.Handle is null;

	internal Configuration CurrentConfiguration { get; }

	internal IEnumerable<(string Savepoint, string Activity, ISelectableView? ViewModel)> UndoStack => this.undoStack;

	private string DebuggerDisplay => this.Path;

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFile"/> class
	/// that persists to a given file path.
	/// </summary>
	/// <param name="path">The path of the file to open or create.</param>
	/// <returns>The new instance of <see cref="MoneyFile"/>.</returns>
	public static MoneyFile Load(string path)
	{
		Requires.NotNullOrEmpty(path, nameof(path));
		SQLiteConnection db = new(path);
		try
		{
			return Load(db);
		}
		catch
		{
			db.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFile"/> class
	/// starting with the given connection.
	/// </summary>
	/// <param name="db">A connection to the database to connect to. Ownership of this connection is transferred to the returned <see cref="MoneyFile"/> instance.</param>
	/// <returns>The new instance of <see cref="MoneyFile"/>.</returns>
	public static MoneyFile Load(SQLiteConnection db)
	{
		Requires.NotNull(db, nameof(db));

		switch (DatabaseSchemaManager.IsUpgradeRequired(db))
		{
			case DatabaseSchemaManager.SchemaCompatibility.RequiresAppUpgrade:
				throw new InvalidOperationException("This file was created with a newer version of the application. Please upgrade your application first.");
			case DatabaseSchemaManager.SchemaCompatibility.RequiresDatabaseUpgrade:
				if (db.DatabasePath != ":memory:")
				{
					string databasePath = db.DatabasePath;
					db.Dispose();
					DatabaseSchemaManager.Upgrade(databasePath);
					db = new SQLiteConnection(databasePath);
				}
				else
				{
					DatabaseSchemaManager.Upgrade(db);
				}

				break;
		}

		return new MoneyFile(db);
	}

	/// <summary>
	/// Commits all changes to the database and clears the undo stack.
	/// </summary>
	public void Save()
	{
		if (this.undoStack.Count > 0)
		{
			this.undoStack.Clear();
			this.connection.Commit();
			this.LogSqlEvent(EventType.Commit, null, null);
			this.OnPropertyChanged(nameof(this.UndoStack));
		}
	}

	/// <summary>
	/// Reverts the database to the last savepoint as recorded with <see cref="RecordSavepoint(string, ISelectableView?)"/>.
	/// </summary>
	/// <returns>A model that may have been impacted by the rollback.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the undo stack is empty.</exception>
	public ISelectableView? Undo()
	{
		(string SavepointName, string Activity, ISelectableView? ViewModel) savepoint = this.undoStack.Count > 0 ? this.undoStack.Pop() : throw new InvalidOperationException("Nothing to undo.");
		this.OnPropertyChanged(nameof(this.UndoStack));
		this.connection.RollbackTo(savepoint.SavepointName);
		this.LogSqlEvent(EventType.Rollback, savepoint.Activity, null);
		this.IncrementDataVersion();
		return savepoint.ViewModel;
	}

	public T Get<T>(object primaryKey)
		where T : new()
	{
		this.LogSqlEvent(EventType.SelectQuery, typeof(T).Name, null);
		return this.connection.Get<T>(primaryKey);
	}

	public T Insert<T>(T model)
		where T : ModelBase
	{
		Verify.NotDisposed(this);
		this.connection.Insert(model);
		this.LogInsert(model);
		this.IncrementDataVersion();
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: new[] { model }));
		return model;
	}

	public void InsertAll(params ModelBase[] models)
	{
		Verify.NotDisposed(this);
		this.connection.InsertAll(models);
		this.LogInsert(models);
		this.IncrementDataVersion();
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: models));
	}

	public void InsertAll(IReadOnlyList<ModelBase> models, string? extra = null)
	{
		this.connection.InsertAll(models, extra);
		this.LogInsert(models);
		this.IncrementDataVersion();
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: models));
	}

	public void Update(ModelBase model)
	{
		Verify.NotDisposed(this);
		ModelBase before = (ModelBase)this.connection.Get(model.Id, this.GetTableMapping(model));
		this.connection.Update(model);
		this.LogUpdate(model);
		this.IncrementDataVersion();
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(changed: new[] { (before, model) }));
	}

	public void InsertOrReplace(ModelBase model)
	{
		Requires.Argument(model.Id >= 0, nameof(model), "This model has a negative ID and therefore should never be persisted.");
		Verify.NotDisposed(this);

		ModelBase? before = model.Id > 0 ? (ModelBase)this.connection.Find(model.Id, this.GetTableMapping(model)) : null;
		if (before is not null && this.connection.Update(model) > 0)
		{
			this.LogUpdate(model);
			this.IncrementDataVersion();
			this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(changed: new[] { (before, model) }));
		}
		else
		{
			this.connection.Insert(model);
			this.LogInsert(model);
			this.IncrementDataVersion();
			this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: new[] { model }));
		}
	}

	public void InsertOrReplace(IReadOnlyList<ModelBase> models)
	{
		Verify.NotDisposed(this);

		foreach (ModelBase model in models)
		{
			Requires.Argument(model.Id >= 0, nameof(model), "This model has a negative ID and therefore should never be persisted.");
		}

		List<ModelBase> inserted = new(models.Count);
		List<(ModelBase, ModelBase)> changed = new(models.Count);
		foreach (ModelBase model in models)
		{
			ModelBase? before = model.Id > 0 ? (ModelBase)this.connection.Find(model.Id, this.GetTableMapping(model)) : null;
			if (before is not null && this.connection.Update(model) > 0)
			{
				this.LogUpdate(model);
				changed.Add((before, model));
			}
			else
			{
				this.connection.Insert(model);
				this.LogInsert(model);
				inserted.Add(model);
			}
		}

		if (changed.Count > 0 || inserted.Count > 0)
		{
			this.IncrementDataVersion();
		}

		if (changed.Count > 0)
		{
			this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(changed: changed));
		}

		if (inserted.Count > 0)
		{
			this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: inserted));
		}
	}

	public bool Delete(ModelBase model)
	{
		Verify.NotDisposed(this);
		int deletedCount = model.IsPersisted ? this.connection.Delete(model) : 0;
		this.LogDelete(model);
		this.IncrementDataVersion();
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(deleted: new[] { model }));
		return deletedCount > 0;
	}

	/// <summary>
	/// Calculates the sum of all accounts' final balances.
	/// </summary>
	/// <param name="options">Query options.</param>
	/// <returns>The net worth.</returns>
	public decimal GetNetWorth(NetWorthQueryOptions options = default(NetWorthQueryOptions))
	{
		Verify.NotDisposed(this);

		options.AsOfDate ??= DateTime.Today;
		this.RefreshBalances(options);
		this.RefreshAssetValues(options.AsOfDate.Value);

		string closedAccountFilter = options.IncludeClosedAccounts ? string.Empty : "WHERE [Account].[IsClosed] = 0";

		string sql = $@"
SELECT SUM([Balances].[Balance] * [AssetValue].[Value])
FROM [Balances]
INNER JOIN [AssetValue] ON [AssetValue].[AssetId] = [Balances].[AssetId]
INNER JOIN [Account] ON [Account].[Id] = [Balances].[AccountId]
{closedAccountFilter}
";

		decimal netWorth = this.connection.ExecuteScalar<decimal>(sql);
		this.LogSqlEvent(EventType.SelectQuery, "Balances (GetNetWorth)", sql);
		return netWorth;
	}

	/// <summary>
	/// Gets the balance of each asset held in this account.
	/// </summary>
	/// <param name="account">The account to query.</param>
	/// <param name="asOfDate">When specified, considers the value of the <paramref name="account"/> as of the specified date.</param>
	/// <returns>A map of asset IDs and the balance held of that asset in the <paramref name="account"/>.</returns>
	public IReadOnlyDictionary<int, decimal> GetBalances(Account account, DateTime? asOfDate = null)
	{
		Requires.NotNull(account, nameof(account));
		Requires.Argument(account.IsPersisted, nameof(account), "Account must be saved to the database first.");
		Verify.NotDisposed(this);

		this.RefreshBalances(new NetWorthQueryOptions { AsOfDate = asOfDate });
		string sql = "SELECT [AssetId], TOTAL([Balance]) AS [Balance] FROM [Balances] WHERE [AccountId] = ? AND [AssetId] IS NOT NULL GROUP BY [AssetId]";
		List<BalancesRow> balances = this.connection.Query<BalancesRow>(sql, account.Id);
		this.LogSqlEvent(EventType.SelectQuery, $"Balances (GetBalances({account.Id}))", sql);
		return balances.ToDictionary(b => b.AssetId, b => b.Balance);
	}

	/// <summary>
	/// Gets the value held in this account.
	/// The value is the sum of all assets held in the account after multiplying their count by their individual value relative to the user's preferred asset.
	/// </summary>
	/// <param name="account">The account to query.</param>
	/// <param name="asOfDate">When specified, considers the value of the <paramref name="account"/> as of the specified date.</param>
	/// <returns>The value of the account, measured in the user's preferred units.</returns>
	public decimal GetValue(Account account, DateTime? asOfDate = null)
	{
		asOfDate ??= DateTime.Today;
		this.RefreshBalances(new NetWorthQueryOptions { AsOfDate = asOfDate });
		this.RefreshAssetValues(asOfDate.Value);

		string sql = @"
SELECT SUM([Balances].[Balance] * [AssetValue].[Value])
FROM [Balances]
INNER JOIN [AssetValue] ON [AssetValue].[AssetId] = [Balances].[AssetId]
WHERE [Balances].[AccountId] = ?
";
		decimal value = this.connection.ExecuteScalar<decimal>(sql, account.Id);
		this.LogSqlEvent(EventType.SelectQuery, $"Balances (GetValue({account.Id}))", sql);
		return value;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		this.Save();
		this.connection.Dispose();
	}

	internal void PurgeTransactionEntries(int transactionId, IEnumerable<int> entryIdsToPreserve)
	{
		string sql = $"DELETE FROM [TransactionEntry] WHERE [TransactionId] = {transactionId} AND [Id] NOT IN ({string.Join(',', entryIdsToPreserve)})";
		this.ExecuteSql(sql);
		this.LogSqlEvent(EventType.DeleteQuery, nameof(TransactionEntry), sql);
		this.IncrementDataVersion();
	}

	/// <summary>
	/// Starts a reversible transaction.
	/// </summary>
	/// <param name="description"><inheritdoc cref="RecordSavepoint(string, ISelectableView?)" path="/param[@name='nextActivityDescription']"/></param>
	/// <param name="viewModel"><inheritdoc cref="RecordSavepoint(string, ISelectableView?)" path="/param[@name='model']"/></param>
	/// <returns>A value to dispose of at the conclusion of the operation.</returns>
	internal IDisposable? UndoableTransaction(string description, ISelectableView? viewModel)
	{
		if (this.inUndoableTransaction)
		{
			// We only want top-level user actions to be reversible.
			return null;
		}

		this.inUndoableTransaction = true;
		this.RecordSavepoint(description, viewModel);
		return new ActionOnDispose(() =>
		{
			this.inUndoableTransaction = false;
			if (this.pendingAggregateDataRefresh)
			{
				this.TriggerAggregateDataRefresh();
			}
		});
	}

	internal bool IsAccountInUse(int accountId)
	{
		if (accountId <= 0)
		{
			return false;
		}

		string sql = $@"SELECT [Id] FROM ""{nameof(TransactionEntry)}"" WHERE ""{nameof(TransactionEntry.AccountId)}"" == ? LIMIT 1";
		bool result = this.connection.Query<int>(sql, accountId).Any();
		this.LogSqlEvent(EventType.SelectQuery, nameof(TransactionEntry), sql);
		return result;
	}

	internal bool IsAssetInUse(int assetId)
	{
		if (assetId == 0)
		{
			return false;
		}

		string sql = $@"SELECT COUNT(*) FROM ""{nameof(Account)}"" WHERE ""{nameof(Account.CurrencyAssetId)}"" == ? LIMIT 1";
		this.LogSqlEvent(EventType.SelectQuery, nameof(Account), sql);
		if (this.connection.ExecuteScalar<int>(sql, assetId) > 0)
		{
			return true;
		}

		sql = $@"SELECT COUNT(*) FROM ""{nameof(TransactionEntry)}"" WHERE ""{nameof(TransactionEntry.AssetId)}"" == ? LIMIT 1";
		this.LogSqlEvent(EventType.SelectQuery, nameof(TransactionEntry), sql);
		if (this.connection.ExecuteScalar<int>(sql, assetId) > 0)
		{
			return true;
		}

		return false;
	}

	internal void ReassignCategory(IEnumerable<int> oldCategoryIds, int? newId)
	{
		Requires.Range(newId is null or > 0, nameof(newId), "Category ID must be a positive integer or null.");

		if (newId is null)
		{
			string sql = $@"DELETE FROM ""{nameof(TransactionEntry)}""
WHERE ""{nameof(TransactionEntry.AccountId)}"" IN ({string.Join(", ", oldCategoryIds.Select(c => c.ToString(CultureInfo.InvariantCulture)))})";
			this.connection.Execute(sql);
			this.LogSqlEvent(EventType.DeleteQuery, nameof(TransactionEntry), sql);
		}
		else
		{
			string sql = $@"UPDATE ""{nameof(TransactionEntry)}""
SET ""{nameof(TransactionEntry.AccountId)}"" = ?
WHERE ""{nameof(TransactionEntry.AccountId)}"" IN ({string.Join(", ", oldCategoryIds.Select(c => c.ToString(CultureInfo.InvariantCulture)))})";
			this.connection.Execute(sql, newId == 0 ? null : newId);
			this.LogSqlEvent(EventType.UpdateQuery, nameof(TransactionEntry), sql);
		}
	}

	internal List<TransactionAndEntry> GetTopLevelTransactionsFor(int accountId)
	{
		string sql = $@"SELECT * FROM ""{nameof(TransactionAndEntry)}"""
			+ $@" WHERE [ContextAccountId] == ?";
		List<TransactionAndEntry> result = this.connection.Query<TransactionAndEntry>(sql, accountId);
		this.LogSqlEvent(EventType.SelectQuery, nameof(TransactionAndEntry), sql);
		return result;
	}

	internal List<TransactionAndEntry> GetTransactionDetails(int transactionId)
	{
		string sql = $@"SELECT * FROM ""{nameof(TransactionAndEntry)}"""
			+ $@" WHERE [TransactionId] == ?";
		List<TransactionAndEntry> result = this.connection.Query<TransactionAndEntry>(sql, transactionId);
		this.LogSqlEvent(EventType.SelectQuery, nameof(TransactionAndEntry), sql);
		return result;
	}

	internal List<TransactionAndEntry> GetTransactionDetails(int transactionId, int accountId)
	{
		string sql = $@"SELECT * FROM ""{nameof(TransactionAndEntry)}"""
			+ $@" WHERE [ContextAccountId] == ? AND [TransactionId] == ?";
		List<TransactionAndEntry> result = this.connection.Query<TransactionAndEntry>(sql, accountId, transactionId);
		this.LogSqlEvent(EventType.SelectQuery, nameof(TransactionAndEntry), sql);
		return result;
	}

	internal List<TransactionEntry> GetTransactionEntries(int transactionId) => this.TransactionEntries.Where(te => te.TransactionId == transactionId).ToList();

	/// <summary>
	/// Gets a list of IDs to the accounts whose ledgers would display any transactions with the given IDs.
	/// </summary>
	/// <param name="transactionIds">The IDs of the transactions.</param>
	/// <returns>The list of account IDs.</returns>
	internal List<int> GetAccountIdsImpactedByTransaction(IReadOnlyCollection<int> transactionIds)
	{
		const string query = "SELECT DISTINCT [ContextAccountId] AS [AccountId] FROM [TransactionAndEntry] WHERE [TransactionId] IN ?";
		List<int> result = this.connection.Query<int>(query, transactionIds);
		this.LogSqlEvent(EventType.SelectQuery, nameof(TransactionAndEntry), query);
		return result;
	}

	/// <summary>
	/// Marks this particular version of the database so that it may later be recovered
	/// by a call to <see cref="Undo"/>.
	/// </summary>
	/// <param name="nextActivityDescription">A human-readable description of the operation that is about to be applied to the database. Use present tense with no trailing period.</param>
	/// <param name="viewModel">The model that is about to be changed, that should be selected if the database is ever rolled back to this point.</param>
	internal void RecordSavepoint(string nextActivityDescription, ISelectableView? viewModel)
	{
		string savepoint = this.connection.SaveTransactionPoint();
		this.LogSqlEvent(EventType.Savepoint, savepoint, null);
		this.undoStack.Push((savepoint, nextActivityDescription, viewModel));
		this.OnPropertyChanged(nameof(this.UndoStack));
	}

	private static string SqlJoinConditionWithOperator(string op, IEnumerable<string> constraints) => string.Join($" {op} ", constraints.Select(c => $"({c})"));

	private static string SqlAnd(IEnumerable<string> constraints) => SqlJoinConditionWithOperator("AND", constraints);

	private static string SqlWhere(string condition) => string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";

	private static DateTime GetSqlBeforeDateOperand(DateTime asOfDate) => asOfDate.AddDays(1).Date;

	/// <summary>
	/// Increments the <see cref="version"/> field and triggers a refresh of <see cref="AggregateData"/>.
	/// </summary>
	private void IncrementDataVersion()
	{
		this.version++;
		this.TriggerAggregateDataRefresh();
	}

	private AggregateData ComputeAggregateData(NetWorthQueryOptions netWorthQueryOptions = default)
	{
		long version = this.version;
		decimal netWorth = this.GetNetWorth(netWorthQueryOptions);
		Dictionary<int, decimal> accountBalances = new();
		foreach (Account account in this.Accounts)
		{
			accountBalances.Add(account.Id, this.GetValue(account, netWorthQueryOptions.AsOfDate));
		}

		return new AggregateData(accountBalances, netWorth, version);
	}

	private TableMapping GetTableMapping(ModelBase model) => this.connection.GetMapping(model.GetType());

	private void LogSqlEvent(EventType queryType, string? message, string? sql)
	{
		this.TraceSource.TraceEvent(TraceEventType.Information, (int)queryType, message);
		if (sql is not null)
		{
			this.TraceSource.TraceData(TraceEventType.Verbose, (int)EventType.Sql, sql);
		}
	}

	private void LogUpdate(ModelBase model)
	{
		this.TraceSource.TraceEvent(TraceEventType.Verbose, (int)EventType.UpdateQuery, "{0} {1}", model.GetType().Name, model.Id);
	}

	private void LogDelete(ModelBase model)
	{
		if (model.IsPersisted)
		{
			this.TraceSource.TraceEvent(TraceEventType.Verbose, (int)EventType.DeleteQuery, "{0} {1}", model.GetType().Name, model.Id);
		}
	}

	private void LogInsert(ModelBase model)
	{
		this.TraceSource.TraceEvent(TraceEventType.Verbose, (int)EventType.InsertQuery, "{0} {1}", model.GetType().Name, model.Id);
	}

	private void LogInsert(IReadOnlyList<ModelBase> models)
	{
		if (models.Count > 0)
		{
			this.TraceSource.TraceEvent(TraceEventType.Verbose, (int)EventType.InsertQuery, "{0} {1}", models[0].GetType().Name, string.Join(", ", models.Select(m => m.Id)));
		}
	}

	private void RefreshBalances(NetWorthQueryOptions options = default(NetWorthQueryOptions))
	{
		(long, NetWorthQueryOptions) currentVersion = (this.version, options);
		if (this.lastRefreshedBalances == currentVersion)
		{
			return;
		}

		this.lastRefreshedBalances = currentVersion;
		List<object> args = new();
		string dateFilter = string.Empty;
		if (options.BeforeDate.HasValue)
		{
			dateFilter = "WHERE t.[When] < ?";
			args.Add(options.BeforeDate.Value);
		}

		object[] argsArray = args.ToArray();

		string sql = "DROP TABLE IF EXISTS [Balances]";
		this.connection.Execute(sql);
		sql = $@"
CREATE {TEMPTableModifier} TABLE [Balances] (
  [AccountId] INTEGER,
  [AssetId] INTEGER,
  [Balance] REAL
)";
		this.connection.Execute(sql);
		sql = $@"
INSERT INTO [Balances]
SELECT te.[AccountId] AS [AccountId], te.[AssetId] AS [AssetId], TOTAL(te.[Amount]) AS [Amount]
FROM [TransactionEntry] te
INNER JOIN [Transaction] t ON t.[Id] = te.[TransactionId]
{dateFilter}
GROUP BY [AccountId], [AssetId]";
		this.connection.Execute(sql, argsArray);
	}

	private void RefreshAssetValues(DateTime asOfDate)
	{
		string sql = $@"
DROP TABLE IF EXISTS [AssetValue];

CREATE {TEMPTableModifier} TABLE [AssetValue] (
	[AssetId] INTEGER UNIQUE,
	[Value] REAL
);";
		this.ExecuteSql(sql);

		sql = "INSERT INTO [AssetValue] VALUES (?, 1)";
		this.connection.Execute(sql, this.PreferredAssetId);

		sql = @"
INSERT INTO [AssetValue]
SELECT
	[Id] AS [AssetId],
	(
		SELECT [PriceInReferenceAsset]
		FROM [AssetPrice]
		WHERE [a].[Id] = [AssetId] AND [When] < ? AND [ReferenceAssetId] = ?
		ORDER BY [When] DESC
		LIMIT 1
	) AS [Value]
FROM [Asset] a
WHERE [Id] != ?
";

		DateTime when = GetSqlBeforeDateOperand(asOfDate);
		this.connection.Execute(sql, when, this.PreferredAssetId, this.PreferredAssetId);
	}

	private void ExecuteSql(string sql)
	{
		int result = SQLitePCL.raw.sqlite3_exec(this.connection.Handle, sql);
		if (result is not SQLitePCL.raw.SQLITE_OK or SQLitePCL.raw.SQLITE_DONE)
		{
			string errMsg = SQLitePCL.raw.sqlite3_errmsg(this.connection.Handle).utf8_to_string();
			throw new Exception(errMsg);
		}
	}

	/// <summary>
	/// Refreshes <see cref="AggregateData"/>, or records that one should occur if we're in an undoable transaction.
	/// </summary>
	private void TriggerAggregateDataRefresh()
	{
		if (this.inUndoableTransaction)
		{
			this.pendingAggregateDataRefresh = true;
		}
		else
		{
			this.AggregateData = this.ComputeAggregateData(new NetWorthQueryOptions { AsOfDate = DateTime.Today });
			this.pendingAggregateDataRefresh = false;
		}
	}

	public record struct NetWorthQueryOptions
	{
		/// <summary>
		/// Gets or sets the date to calculate the net worth for, considering all transactions that occurred on the specified date.
		/// If null, all transactions are considered regardless of when they occurred.
		/// </summary>
		public DateTime? AsOfDate { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to consider the balance of closed accounts when calculating net worth.
		/// </summary>
		public bool IncludeClosedAccounts { get; set; }

		/// <summary>
		/// Gets the date to use as the constraint argument for when transactions must have posted *before*
		/// in order to be included in the query.
		/// </summary>
		/// <remarks>
		/// Because we want to consider transaction no matter what *time* of day they came in on the "as of date",
		/// add a whole day, then drop the time component. We'll look for transactions that happened before that.
		/// </remarks>
		internal DateTime? BeforeDate => this.AsOfDate.HasValue ? GetSqlBeforeDateOperand(this.AsOfDate.Value) : null;
	}

	public class EntitiesChangedEventArgs : EventArgs
	{
		public EntitiesChangedEventArgs(
			IReadOnlyCollection<ModelBase>? inserted = null,
			IReadOnlyCollection<(ModelBase Before, ModelBase After)>? changed = null,
			IReadOnlyCollection<ModelBase>? deleted = null)
		{
			this.Inserted = inserted ?? Array.Empty<ModelBase>();
			this.Changed = changed ?? Array.Empty<(ModelBase, ModelBase)>();
			this.Deleted = deleted ?? Array.Empty<ModelBase>();
		}

		public IReadOnlyCollection<ModelBase> Inserted { get; }

		public IReadOnlyCollection<(ModelBase Before, ModelBase After)> Changed { get; }

		public IReadOnlyCollection<ModelBase> Deleted { get; }
	}

	public class TransactionOperations
	{
		private readonly MoneyFile money;

		internal TransactionOperations(MoneyFile money)
		{
			this.money = money;
		}

		public void Deposit(Account account, decimal amount, DateTime? when = null)
		{
			Requires.Argument(account.CurrencyAssetId.HasValue, nameof(account), "Account has no default currency set.");
			this.Deposit(account, new Amount(amount, account.CurrencyAssetId.Value), when);
		}

		public void Deposit(Account account, Amount amount, DateTime? when = null)
		{
			Transaction tx = new()
			{
				Action = TransactionAction.Deposit,
				When = when ?? DateTime.Today,
			};
			this.money.Insert(tx);
			this.money.InsertAll(
				new TransactionEntry
				{
					AccountId = account.Id,
					Amount = amount.Value,
					AssetId = amount.AssetId,
					TransactionId = tx.Id,
				});
		}

		public void Withdraw(Account account, decimal amount, DateTime? when = null)
		{
			Requires.Argument(account.CurrencyAssetId.HasValue, nameof(account), "Account has no default currency set.");
			this.Withdraw(account, new Amount(amount, account.CurrencyAssetId.Value), when);
		}

		public void Withdraw(Account account, Amount amount, DateTime? when = null)
		{
			Transaction tx = new()
			{
				Action = TransactionAction.Withdraw,
				When = when ?? DateTime.Today,
			};
			this.money.Insert(tx);
			this.money.InsertAll(
				new TransactionEntry
				{
					AccountId = account.Id,
					Amount = -amount.Value,
					AssetId = amount.AssetId,
					TransactionId = tx.Id,
				});
		}

		public void Transfer(Account from, Account to, decimal amount, DateTime? when = null)
		{
			Requires.Argument(from.CurrencyAssetId.HasValue, nameof(from), "Account has no default currency set.");
			this.Transfer(from, to, new Amount(amount, from.CurrencyAssetId.Value), when);
		}

		public void Transfer(Account from, Account to, Amount amount, DateTime? when = null)
		{
			Transaction tx = new()
			{
				Action = TransactionAction.Transfer,
				When = when ?? DateTime.Today,
			};
			this.money.Insert(tx);
			this.money.InsertAll(
				new TransactionEntry
				{
					AccountId = from.Id,
					Amount = -amount.Value,
					AssetId = amount.AssetId,
					TransactionId = tx.Id,
				},
				new TransactionEntry
				{
					AccountId = to.Id,
					Amount = amount.Value,
					AssetId = amount.AssetId,
					TransactionId = tx.Id,
				});
		}

		public void Add(Account account, Amount amount, DateTime? when = null)
		{
			Transaction tx = new()
			{
				When = when ?? DateTime.Today,
				Action = TransactionAction.Add,
			};
			this.money.Insert(tx);
			this.money.InsertAll(
				new TransactionEntry
				{
					TransactionId = tx.Id,
					AccountId = account.Id,
					Amount = amount.Value,
					AssetId = amount.AssetId,
				});
		}

		public void Remove(Account account, Amount amount, DateTime? when = null)
		{
			Transaction tx = new()
			{
				When = when ?? DateTime.Today,
				Action = TransactionAction.Remove,
			};
			this.money.Insert(tx);
			this.money.InsertAll(
				new TransactionEntry
				{
					TransactionId = tx.Id,
					AccountId = account.Id,
					Amount = -amount.Value,
					AssetId = amount.AssetId,
				});
		}

		public void AssetPrice(Asset asset, DateTime when, decimal price)
		{
			this.money.Insert(new AssetPrice { AssetId = asset.Id, ReferenceAssetId = this.money.PreferredAssetId, When = when, PriceInReferenceAsset = price });
		}
	}

	private class BalancesRow
	{
#pragma warning disable CS0649 // unset fields will be set by sqlite-pcl
		public int AccountId { get; set; }

		public int AssetId { get; set; }

		public decimal Balance { get; set; }
#pragma warning restore CS0649
	}

	private class AccountValueRow
	{
#pragma warning disable CS0649 // unset fields will be set by sqlite-pcl
		public int AccountId { get; set; }

		public decimal Value { get; set; }
#pragma warning restore CS0649
	}
}
