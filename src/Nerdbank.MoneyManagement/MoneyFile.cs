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
	private readonly Stack<(string SavepointName, string Activity)> undoStack = new();

	private bool inUndoableTransaction;

	/// <summary>
	/// Initializes a new instance of the <see cref="MoneyFile"/> class.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	private MoneyFile(SQLiteConnection connection)
	{
		Requires.NotNull(connection, nameof(connection));
		this.connection = connection;
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

	public IEnumerable<(string Savepoint, string Activity)> UndoStack => this.undoStack;

	public bool IsDisposed => this.connection.Handle is null;

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

	public void Undo()
	{
		(string SavepointName, string Activity) savepoint = this.undoStack.Count > 0 ? this.undoStack.Pop() : throw new InvalidOperationException("Nothing to undo.");
		this.OnPropertyChanged(nameof(this.UndoStack));
		this.Logger?.WriteLine("Rolling back: {0}", savepoint.Activity);
		this.connection.RollbackTo(savepoint.SavepointName);
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

		ImmutableList<string> constraints = ImmutableList<string>.Empty;
		ImmutableList<object> args = ImmutableList<object>.Empty;
		if (options.BeforeDate.HasValue)
		{
			constraints = constraints.Add($@"""{nameof(Transaction.When)}"" < ?");
			args = args.Add(options.BeforeDate);
		}

		if (!options.IncludeClosedAccounts)
		{
			constraints = constraints.Add($@"a.""{nameof(Account.IsClosed)}"" = 0");
		}

		string netCreditConstraint = $@"""{nameof(Transaction.CreditAccountId)}"" IS NOT NULL AND ""{nameof(Transaction.DebitAccountId)}"" IS NULL";
		string netDebitConstraint = $@"""{nameof(Transaction.CreditAccountId)}"" IS NULL AND ""{nameof(Transaction.DebitAccountId)}"" IS NOT NULL";

		string sql = $@"SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}"""
			+ $@" INNER JOIN {nameof(Account)} a ON a.""{nameof(Account.Id)}"" = ""{nameof(Transaction.CreditAccountId)}"""
			+ SqlWhere(SqlAnd(constraints.Add(netCreditConstraint)));
		decimal credits = this.ExecuteScalar<decimal>(sql, args.ToArray());

		sql = $@"SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}"""
			+ $@" INNER JOIN {nameof(Account)} a ON a.""{nameof(Account.Id)}"" = ""{nameof(Transaction.DebitAccountId)}"""
			+ SqlWhere(SqlAnd(constraints.Add(netDebitConstraint)));
		decimal debits = this.ExecuteScalar<decimal>(sql, args.ToArray());

		decimal sum = credits - debits;
		return sum;
	}

	public decimal GetBalance(Account account)
	{
		Requires.NotNull(account, nameof(account));
		Requires.Argument(account.IsPersisted, nameof(account), "Account must be saved to the database first.");
		Verify.NotDisposed(this);

		decimal credits = this.ExecuteScalar<decimal>(
			$@"SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}""
                WHERE ""{nameof(Transaction.CreditAccountId)}"" = ?",
			account.Id);
		decimal debits = this.ExecuteScalar<decimal>(
			$@"SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}""
                WHERE ""{nameof(Transaction.DebitAccountId)}"" = ?",
			account.Id);
		decimal sum = credits - debits;
		return sum;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		this.Save();
		this.connection.Dispose();
	}

	internal IDisposable? UndoableTransaction(string description)
	{
		if (this.inUndoableTransaction)
		{
			// We only want top-level user actions to be reversible.
			return null;
		}

		this.inUndoableTransaction = true;
		this.RecordSavepoint(description);
		return new ActionOnDispose(() => this.inUndoableTransaction = false);
	}

	internal IEnumerable<IntegrityChecks.SplitTransactionTotalMismatch> FindBadSplitTransactions(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		string sql = $@"SELECT * FROM ""{nameof(Transaction)}"" t"
			+ $@" WHERE t.[{nameof(Transaction.CategoryId)}] == {Category.Split} AND t.[{nameof(Transaction.Amount)}] != 0";

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

	internal void RecordSavepoint(string nextActivityDescription)
	{
		this.Logger?.WriteLine("Writing savepoint before: {0}", nextActivityDescription);
		this.undoStack.Push((this.connection.SaveTransactionPoint(), nextActivityDescription));
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
}
