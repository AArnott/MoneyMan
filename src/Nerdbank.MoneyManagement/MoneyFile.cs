// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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
	/// <summary>
	/// The SQLite database connection.
	/// </summary>
	private readonly SQLiteConnection connection;

	/// <summary>
	/// The undo stack.
	/// </summary>
	private readonly Stack<(string SavepointName, string Activity, ModelBase? Model)> undoStack = new();

	private int preferredAssetId;

	private bool inUndoableTransaction;

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFile"/> class.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	private MoneyFile(SQLiteConnection connection)
	{
		Requires.NotNull(connection, nameof(connection));
		this.connection = connection;

		this.preferredAssetId = this.connection.ExecuteScalar<int>("SELECT [PreferredAssetId] FROM [Configuration] LIMIT 1");
	}

	/// <summary>
	/// Occurs when one or more entities are inserted, updated, and/or removed.
	/// </summary>
	public event EventHandler<EntitiesChangedEventArgs>? EntitiesChanged;

	public string Path => this.connection.DatabasePath;

	public TextWriter? Logger { get; set; }

	public TableQuery<Account> Accounts
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<Account>();
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

	public TableQuery<Transaction> Transactions
	{
		get
		{
			Verify.NotDisposed(this);
			return this.connection.Table<Transaction>();
		}
	}

	public TableQuery<Category> Categories
	{
		get
		{
			Verify.NotDisposed(this);

			// Omit the special categories like those used for splits.
			return this.connection.Table<Category>().Where(cat => cat.Id > 0);
		}
	}

	public int PreferredAssetId
	{
		get => this.preferredAssetId;
		set
		{
			if (this.connection.Execute("UPDATE [Configuration] SET [PreferredAssetId] = ?", value) != 1)
			{
				throw new InvalidOperationException("Failure writing configuration.");
			}

			this.preferredAssetId = value;
		}
	}

	public bool IsDisposed => this.connection.Handle is null;

	internal IEnumerable<(string Savepoint, string Activity, ModelBase? Model)> UndoStack => this.undoStack;

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
			switch (DatabaseSchemaUpgradeManager.IsUpgradeRequired(db))
			{
				case DatabaseSchemaUpgradeManager.SchemaCompatibility.RequiresAppUpgrade:
					throw new InvalidOperationException("This file was created with a newer version of the application. Please upgrade your application first.");
				case DatabaseSchemaUpgradeManager.SchemaCompatibility.RequiresDatabaseUpgrade:
					if (path != ":memory:")
					{
						db.Dispose();
						DatabaseSchemaUpgradeManager.Upgrade(path);
						db = new SQLiteConnection(path);
					}
					else
					{
						DatabaseSchemaUpgradeManager.Upgrade(db);
					}

					break;
			}

			return new MoneyFile(db);
		}
		catch
		{
			db.Dispose();
			throw;
		}
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
			this.OnPropertyChanged(nameof(this.UndoStack));
		}
	}

	/// <summary>
	/// Reverts the database to the last savepoint as recorded with <see cref="RecordSavepoint(string, ModelBase?)"/>.
	/// </summary>
	/// <returns>A model that may have been impacted by the rollback.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the undo stack is empty.</exception>
	public ModelBase? Undo()
	{
		(string SavepointName, string Activity, ModelBase? Model) savepoint = this.undoStack.Count > 0 ? this.undoStack.Pop() : throw new InvalidOperationException("Nothing to undo.");
		this.OnPropertyChanged(nameof(this.UndoStack));
		this.Logger?.WriteLine("Rolling back: {0}", savepoint.Activity);
		this.connection.RollbackTo(savepoint.SavepointName);
		return savepoint.Model;
	}

	public T Get<T>(object primaryKey)
		where T : new()
	{
		return this.connection.Get<T>(primaryKey);
	}

	public T Insert<T>(T model)
		where T : ModelBase
	{
		Verify.NotDisposed(this);
		this.connection.Insert(model);
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: new[] { model }));
		return model;
	}

	public void InsertAll(params ModelBase[] models)
	{
		Verify.NotDisposed(this);
		this.connection.InsertAll(models);
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: models));
	}

	public void Update(ModelBase model)
	{
		Verify.NotDisposed(this);
		ModelBase before = (ModelBase)this.connection.Get(model.Id, this.GetTableMapping(model));
		this.connection.Update(model);
		this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(changed: new[] { (before, model) }));
	}

	public void InsertOrReplace(ModelBase model)
	{
		Verify.NotDisposed(this);
		ModelBase before = (ModelBase)this.connection.Find(model.Id, this.GetTableMapping(model));
		if (this.connection.Update(model) > 0)
		{
			this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(changed: new[] { (before, model) }));
		}
		else
		{
			this.connection.Insert(model);
			this.EntitiesChanged?.Invoke(this, new EntitiesChangedEventArgs(inserted: new[] { model }));
		}
	}

	public bool Delete(ModelBase model)
	{
		Verify.NotDisposed(this);
		int deletedCount = this.connection.Delete(model);
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

		this.RefreshBalances(options);

		string closedAccountFilter = options.IncludeClosedAccounts ? string.Empty : "WHERE [Account].[IsClosed] = 0";

		string sql = $@"
SELECT [Balances].[AssetId], TOTAL([Balances].[Balance]) AS [Balance]
FROM [Balances]
INNER JOIN [Asset] ON [Asset].[Id] = [Balances].[AssetId]
INNER JOIN [Account] ON [Account].[Id] = [Balances].[AccountId]
{closedAccountFilter}
GROUP BY [AssetId]";

		// When we have a table of historical values for each asset, we can look up each asset's value
		// as of the given date to multiply by the count of the asset
		// to find out the worth of each held asset.
		// TODO: code here

		List<BalancesRow> balances = this.connection.Query<BalancesRow>(sql);
		return balances.Sum(b => b.Balance);
	}

	/// <summary>
	/// Gets the balance of each asset held in this account.
	/// </summary>
	/// <param name="account">The account to query.</param>
	/// <returns>A map of asset IDs and the balance held of that asset in the <paramref name="account"/>.</returns>
	public IReadOnlyDictionary<int, decimal> GetBalances(Account account)
	{
		Requires.NotNull(account, nameof(account));
		Requires.Argument(account.IsPersisted, nameof(account), "Account must be saved to the database first.");
		Verify.NotDisposed(this);

		this.RefreshBalances();
		string sql = "SELECT [AssetId], TOTAL([Balance]) AS [Balance] FROM [Balances] WHERE [AccountId] = ? AND [AssetId] IS NOT NULL GROUP BY [AssetId]";
		List<BalancesRow> balances = this.connection.Query<BalancesRow>(sql, account.Id);
		return balances.ToDictionary(b => b.AssetId, b => b.Balance);
	}

	/// <summary>
	/// Gets the value held in this account.
	/// The value is the sum of all assets held in the account after multiplying their count by their individual value relative to the user's preferred asset.
	/// </summary>
	/// <param name="account">The account to query.</param>
	/// <returns>The value of the account, measured in the user's preferred units.</returns>
	public decimal GetValue(Account account)
	{
		IReadOnlyDictionary<int, decimal> balances = this.GetBalances(account);

		// TODO: Consider all assets.
		return balances.TryGetValue(this.PreferredAssetId, out decimal value) ? value : 0m;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		this.Save();
		this.connection.Dispose();
	}

	/// <summary>
	/// Starts a reversible transaction.
	/// </summary>
	/// <param name="description"><inheritdoc cref="RecordSavepoint(string, ModelBase?)" path="/param[@name='nextActivityDescription']"/></param>
	/// <param name="model"><inheritdoc cref="RecordSavepoint(string, ModelBase?)" path="/param[@name='model']"/></param>
	/// <returns>A value to dispose of at the conclusion of the operation.</returns>
	internal IDisposable? UndoableTransaction(string description, ModelBase? model)
	{
		if (this.inUndoableTransaction)
		{
			// We only want top-level user actions to be reversible.
			return null;
		}

		this.inUndoableTransaction = true;
		this.RecordSavepoint(description, model);
		return new ActionOnDispose(() => this.inUndoableTransaction = false);
	}

	internal IEnumerable<IntegrityChecks.SplitTransactionTotalMismatch> FindBadSplitTransactions(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		string sql = $@"SELECT * FROM ""{nameof(Transaction)}"" t"
			+ $@" WHERE t.[{nameof(Transaction.CategoryId)}] == {Category.Split} AND (t.[{nameof(Transaction.CreditAmount)}] != 0 OR t.[{nameof(Transaction.DebitAmount)}] != 0)";

		foreach (Transaction badSplit in this.connection.Query<Transaction>(sql))
		{
			yield return new IntegrityChecks.SplitTransactionTotalMismatch(badSplit);
			cancellationToken.ThrowIfCancellationRequested();
		}
	}

	internal bool IsCategoryInUse(int categoryId)
	{
		string sql = $@"SELECT COUNT(*) FROM ""{nameof(Transaction)}"" WHERE ""{nameof(Transaction.CategoryId)}"" == ?";
		return this.ExecuteScalar<int>(sql, categoryId) > 0;
	}

	internal bool IsAssetInUse(int assetId)
	{
		string sql = $@"SELECT COUNT(*) FROM ""{nameof(Account)}"" WHERE ""{nameof(Account.CurrencyAssetId)}"" == ? LIMIT 1";
		if (this.ExecuteScalar<int>(sql, assetId) > 0)
		{
			return true;
		}

		sql = $@"SELECT COUNT(*) FROM ""{nameof(Transaction)}"" WHERE ""{nameof(Transaction.CreditAssetId)}"" == ? OR ""{nameof(Transaction.DebitAssetId)}"" == ? LIMIT 1";
		if (this.ExecuteScalar<int>(sql, assetId, assetId) > 0)
		{
			return true;
		}

		return false;
	}

	internal void ReassignCategory(IEnumerable<int> oldCategoryIds, int? newId)
	{
		string sql = $@"UPDATE ""{nameof(Transaction)}""
SET ""{nameof(Transaction.CategoryId)}"" = ?
WHERE ""{nameof(Transaction.CategoryId)}"" IN ({string.Join(", ", oldCategoryIds.Select(c => c.ToString(CultureInfo.InvariantCulture)))})";
		this.connection.Execute(sql, newId);
	}

	internal List<Transaction> GetTopLevelTransactionsFor(int accountId)
	{
		// List all transactions that credit/debit to this account so long as they either are not split members or are splits of a transaction native to another account.
		string sql = $@"SELECT * FROM ""{nameof(Transaction)}"" t"
			+ $@" WHERE (t.[{nameof(Transaction.CreditAccountId)}] == ? OR t.[{nameof(Transaction.DebitAccountId)}] == ?)"
			+ $@" AND (t.[{nameof(Transaction.ParentTransactionId)}] IS NULL OR (SELECT [{nameof(Transaction.CreditAccountId)}] FROM ""{nameof(Transaction)}"" WHERE [{nameof(Transaction.Id)}] == t.[{nameof(Transaction.ParentTransactionId)}]) != ?)";
		return this.connection.Query<Transaction>(sql, accountId, accountId, accountId);
	}

	/// <summary>
	/// Marks this particular version of the database so that it may later be recovered
	/// by a call to <see cref="Undo"/>.
	/// </summary>
	/// <param name="nextActivityDescription">A human-readable description of the operation that is about to be applied to the database. Use present tense with no trailing period.</param>
	/// <param name="model">The model that is about to be changed, that should be selected if the database is ever rolled back to this point.</param>
	internal void RecordSavepoint(string nextActivityDescription, ModelBase? model)
	{
		this.Logger?.WriteLine("Writing savepoint before: {0}", nextActivityDescription);
		this.undoStack.Push((this.connection.SaveTransactionPoint(), nextActivityDescription, model));
		this.OnPropertyChanged(nameof(this.UndoStack));
	}

	private static string SqlJoinConditionWithOperator(string op, IEnumerable<string> constraints) => string.Join($" {op} ", constraints.Select(c => $"({c})"));

	private static string SqlAnd(IEnumerable<string> constraints) => SqlJoinConditionWithOperator("AND", constraints);

	private static string SqlWhere(string condition) => string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";

	private TableMapping GetTableMapping(ModelBase model)
	{
		return this.connection.TableMappings.Single(tm => tm.TableName == model.GetType().Name);
	}

	private T ExecuteScalar<T>(string query, params object[] args)
	{
		this.Logger?.WriteLine(query);
		if (args?.Length > 0)
		{
			this.Logger?.WriteLine("With parameters: " + string.Join(", ", args));
		}

		return this.connection.ExecuteScalar<T>(query, args);
	}

	private void RefreshBalances(NetWorthQueryOptions options = default(NetWorthQueryOptions))
	{
		List<object> args = new();
		string dateFilter = string.Empty;
		if (options.BeforeDate.HasValue)
		{
			dateFilter = "AND [When] < ?";
			args.Add(options.BeforeDate.Value);
		}

		object[] argsArray = args.ToArray();

		string sql = "DROP TABLE IF EXISTS [Balances]";
		this.connection.Execute(sql);
		sql = @"
CREATE TEMP TABLE [Balances] (
  [AccountId] INTEGER,
  [AssetId] INTEGER,
  [Balance] REAL
)";
		this.connection.Execute(sql);
		sql = $@"
INSERT INTO [Balances]
SELECT [CreditAccountId] AS [AccountId], [CreditAssetId] AS [AssetId], TOTAL([CreditAmount]) AS [Amount]
FROM [Transaction]
WHERE [CreditAccountId] IS NOT NULL {dateFilter}
GROUP BY [CreditAccountId], [CreditAssetId]";
		this.connection.Execute(sql, argsArray);

		sql = $@"INSERT INTO [Balances]
SELECT [DebitAccountId] AS [AccountId], [DebitAssetId] AS [AssetId], -TOTAL([DebitAmount]) AS [Amount]
FROM [Transaction]
WHERE [DebitAccountId] IS NOT NULL {dateFilter}
GROUP BY [DebitAccountId], [DebitAssetId]";
		this.connection.Execute(sql, argsArray);
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

	public struct NetWorthQueryOptions
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
		internal DateTime? BeforeDate => this.AsOfDate?.AddDays(1).Date;
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

	private class BalancesRow
	{
#pragma warning disable CS0649 // unset fields will be set by sqlite-pcl
		public int AccountId { get; set; }

		public int AssetId { get; set; }

		public decimal Balance { get; set; }
#pragma warning restore CS0649
	}
}
