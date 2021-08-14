// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using SQLite;
	using Validation;

	/// <summary>
	/// Manages the database that stores accounts, transactions, and other entities.
	/// </summary>
	public class MoneyFile : IDisposable
	{
		/// <summary>
		/// The SQLite database connection.
		/// </summary>
		private readonly SQLiteConnection connection;

		/// <summary>
		/// Initializes a new instance of the <see cref="MoneyFile"/> class.
		/// </summary>
		/// <param name="connection">The database connection.</param>
		private MoneyFile(SQLiteConnection connection)
		{
			Requires.NotNull(connection, nameof(connection));
			this.connection = connection;
		}

		public TextWriter? Logger { get; set; }

		public TableQuery<Account> Accounts => this.connection.Table<Account>();

		public TableQuery<Transaction> Transactions => this.connection.Table<Transaction>();

		public TableQuery<SplitTransaction> SplitTransactions => this.connection.Table<SplitTransaction>();

		public TableQuery<Category> Categories => this.connection.Table<Category>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MoneyFile"/> class
		/// that persists to a given file path.
		/// </summary>
		/// <param name="path">The path of the file to open or create.</param>
		/// <returns>The new instance of <see cref="MoneyFile"/>.</returns>
		public static MoneyFile Load(string path)
		{
			Requires.NotNullOrEmpty(path, nameof(path));
			var db = new SQLiteConnection(path);
			try
			{
				// Define all the tables (in case this is a new file).
				db.CreateTable<Account>();
				db.CreateTable<Transaction>();
				db.CreateTable<SplitTransaction>();
				db.CreateTable<Payee>();
				db.CreateTable<Category>();

				return new MoneyFile(db);
			}
			catch
			{
				db.Dispose();
				throw;
			}
		}

		public T Get<T>(object primaryKey)
			where T : new()
		{
			return this.connection.Get<T>(primaryKey);
		}

		public int Insert(object obj) => this.connection.Insert(obj);

		public void InsertAll(params object[] objects) => this.connection.InsertAll(objects);

		public int Update(object obj) => this.connection.Update(obj);

		/// <summary>
		/// Calculates the sum of all accounts' final balances.
		/// </summary>
		/// <param name="options">Query options.</param>
		/// <returns>The net worth.</returns>
		public decimal GetNetWorth(NetWorthQueryOptions options = default(NetWorthQueryOptions))
		{
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
			Requires.Argument(account.Id > 0, nameof(account), "Account must be saved to the database first.");

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
			this.connection.Dispose();
		}

		internal IEnumerable<IntegrityChecks.SplitTransactionTotalMismatch> FindBadSplitTransactions(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string sql = $@"SELECT t.*, (SELECT SUM(""{nameof(SplitTransaction.Amount)}"") FROM [{nameof(SplitTransaction)}] s WHERE s.[{nameof(SplitTransaction.TransactionId)}] == t.[{nameof(Transaction.Id)}]) AS ""{nameof(TransactionAndSplitTotal.SplitTotal)}"""
				+ $@" FROM ""{nameof(Transaction)}"" t"
				+ $@" WHERE t.[{nameof(Transaction.CategoryId)}] == {Category.Split} AND t.[{nameof(Transaction.Amount)}] != [{nameof(TransactionAndSplitTotal.SplitTotal)}]";

			foreach (TransactionAndSplitTotal badSplit in this.connection.Query<TransactionAndSplitTotal>(sql))
			{
				yield return new IntegrityChecks.SplitTransactionTotalMismatch(badSplit, badSplit.SplitTotal);
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		private static string SqlJoinConditionWithOperator(string op, IEnumerable<string> constraints) => string.Join($" {op} ", constraints.Select(c => $"({c})"));

		private static string SqlAnd(IEnumerable<string> constraints) => SqlJoinConditionWithOperator("AND", constraints);

		private static string SqlWhere(string condition) => string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";

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

		private class TransactionAndSplitTotal : Transaction
		{
			public decimal SplitTotal { get; set; }
		}
	}
}
