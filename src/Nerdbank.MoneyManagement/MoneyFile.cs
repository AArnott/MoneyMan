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
            return new MoneyFile(db);
        }

        public SQLiteConnection Connection => this.connection;

        public TableQuery<Account> Accounts => this.connection.Table<Account>();

        public void Dispose()
        {
            this.connection.Dispose();
        }
    }
}
