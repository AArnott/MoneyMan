namespace Nerdbank.MoneyManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using SQLite;
    using Validation;

    public class MoneyFile : IDisposable
    {
        private readonly SQLiteConnection connection;

        private MoneyFile(SQLiteConnection connection)
        {
            Requires.NotNull(connection, nameof(connection));
            this.connection = connection;
        }

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

        public int Update(object obj) => this.connection.Update(obj);

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
