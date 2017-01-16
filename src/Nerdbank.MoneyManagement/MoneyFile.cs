namespace Nerdbank.MoneyManagement
{
    using System;
    using System.Collections.Generic;
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

        public decimal GetNetWorth(DateTime asOfDate)
        {
            // Because we want to consider transaction no matter what *time* of day they came in on the "as of date",
            // add a whole day, then drop the time component. We'll look for transactions that happened before that.
            DateTime dayAfter = asOfDate.AddDays(1).Date;
            decimal credits = this.connection.ExecuteScalar<decimal>($@"
                SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}""
                WHERE ""{nameof(Transaction.CreditAccountId)}"" IS NOT NULL AND ""{nameof(Transaction.DebitAccountId)}"" IS NULL
                AND ""{nameof(Transaction.When)}"" < ?",
                dayAfter);
            decimal debits = this.connection.ExecuteScalar<decimal>($@"
                SELECT TOTAL(""{nameof(Transaction.Amount)}"") FROM ""{nameof(Transaction)}""
                WHERE ""{nameof(Transaction.CreditAccountId)}"" IS NULL AND ""{nameof(Transaction.DebitAccountId)}"" IS NOT NULL
                AND ""{nameof(Transaction.When)}"" < ?",
                dayAfter);
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
    }
}
