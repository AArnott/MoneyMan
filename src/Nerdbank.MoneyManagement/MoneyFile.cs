namespace Nerdbank.MoneyManagement
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
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

            // Define all the tables (in case this is a new).
            db.CreateTable<Account>();
            db.CreateTable<Transaction>();

            return new MoneyFile(db);
        }

        public TableQuery<Account> Accounts => this.connection.Table<Account>();

        public TableQuery<Transaction> Transactions => this.connection.Table<Transaction>();

        public T Get<T>(object primaryKey) where T : new() => this.connection.Get<T>(primaryKey);

        public int Insert(object obj) => this.connection.Insert(obj);

        public void InsertAll(params object[] objects) => this.connection.InsertAll(objects);

        public int Update(object obj) => this.connection.Update(obj);

        /// <summary>
        /// Calculates the sum of all accounts' final balances.
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>The net worth.</returns>
        public decimal GetNetWorth(NetWorthQueryOptions options = default(NetWorthQueryOptions))
        {
            var constraints = ImmutableList<string>.Empty;
            var args = ImmutableList<object>.Empty;
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
            decimal credits = this.connection.ExecuteScalar<decimal>(sql, args.ToArray());

            sql = $@"SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}"""
                + $@" INNER JOIN {nameof(Account)} a ON a.""{nameof(Account.Id)}"" = ""{nameof(Transaction.DebitAccountId)}"""
                + SqlWhere(SqlAnd(constraints.Add(netDebitConstraint)));
            decimal debits = this.connection.ExecuteScalar<decimal>(sql, args.ToArray());

            decimal sum = credits - debits;
            return sum;
        }

        public decimal GetBalance(Account account)
        {
            Requires.NotNull(account, nameof(account));
            Requires.Argument(account.Id > 0, nameof(account), "Account must be saved to the database first.");

            decimal credits = this.connection.ExecuteScalar<decimal>($@"
                SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}""
                WHERE ""{nameof(Transaction.CreditAccountId)}"" = ?",
                account.Id);
            decimal debits = this.connection.ExecuteScalar<decimal>($@"
                SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}""
                WHERE ""{nameof(Transaction.DebitAccountId)}"" = ?",
                account.Id);
            decimal sum = credits - debits;
            return sum;
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }

        private static string SqlJoinConditionWithOperator(string op, IEnumerable<string> constraints) => string.Join($" {op} ", constraints.Select(c => $"({c})"));

        private static string SqlAnd(IEnumerable<string> constraints) => SqlJoinConditionWithOperator("AND", constraints);

        private static string SqlWhere(string condition) => string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";

        public struct NetWorthQueryOptions
        {
            /// <summary>
            /// If specified, calculates the net worth as of a specific date, considering all transactions that occurred on the specified date.
            /// </summary>
            public DateTime? AsOfDate { get; set; }

            /// <summary>
            /// Gets a value indicating whether to consider the balance of closed accounts when calculating net worth.
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
    }
}
