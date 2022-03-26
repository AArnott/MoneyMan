// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using SQLite;

/// <summary>
/// Tests to verify that data of all kinds get upgraded correctly.
/// </summary>
/// <remarks>
/// Each schema version gets its own test.
/// Each test only needs to exercise the new functionality added by that schema version
/// because any pre-existing functionality, even if in a different schema shape, gets tested
/// by virtue of having upgraded through all later schema versions.
/// </remarks>
public class DatabaseSchemaManagerTests : IDisposable
{
	private string dbPath;

	public DatabaseSchemaManagerTests(ITestOutputHelper logger)
	{
		this.dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		this.Logger = logger;
	}

	public ITestOutputHelper Logger { get; }

	public void Dispose()
	{
		File.Delete(this.dbPath);
	}

	[Fact]
	public void UpgradeFromV1()
	{
		using SQLiteConnection connection = this.CreateDatabase(1);
		string sql = @"
INSERT INTO Account (Name, IsClosed) VALUES ('Checking', 0);
INSERT INTO Account (Name, IsClosed) VALUES ('Savings', 0);
INSERT INTO Account (Name, IsClosed) VALUES ('Old', 1);

INSERT INTO Category (Name) VALUES ('cat1');
INSERT INTO Category (Name) VALUES ('cat2');

INSERT INTO [Transaction] ([When], CheckNumber, Amount, Memo, Payee, CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (637834085886150235, NULL, 500,     'first paycheck', 'Microsoft',    2, 1, 0, 2);
INSERT INTO [Transaction] ([When], CheckNumber, Amount, Memo, Payee, CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (637834085888150235, 76,   -123.45, 'memo1',          'payee1',       1, 1, 0, 1);
INSERT INTO [Transaction] ([When], CheckNumber, Amount, Memo, Payee, CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (637834085889150235, NULL, 100,     'transfer',       NULL,        NULL, 2, 1, 0);
-- No splits are added. Although the schema would seem to permit it, the app itself at schema v1 did not support splits.
-- The v2 schema upgrade therefore dropped the SplitTransaction table without any migration.
";
		this.ExecuteSql(connection, sql);
		MoneyFile file = MoneyFile.Load(connection);
		DocumentViewModel documentViewModel = new(file);

		// Account assertions
		Assert.Equal(3, documentViewModel.AccountsPanel.Accounts.Count);
		var checking = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Checking");
		var savings = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Savings");
		var old = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Old");
		Assert.False(checking.IsClosed);
		Assert.True(old.IsClosed);

		// Category assertions
		Assert.Equal(2, documentViewModel.CategoriesPanel.Categories.Count);
		CategoryAccountViewModel cat1 = Assert.Single(documentViewModel.CategoriesPanel.Categories, cat => cat.Name == "cat1");
		CategoryAccountViewModel cat2 = Assert.Single(documentViewModel.CategoriesPanel.Categories, cat => cat.Name == "cat2");

		Assert.Equal(3, checking.Transactions.Count(tx => tx.IsPersisted));
		BankingTransactionViewModel tx1 = checking.Transactions[0];
		Assert.Equal(new DateTime(637834085886150235), tx1.When);
		Assert.Null(tx1.CheckNumber);
		Assert.Equal(500, tx1.Amount);
		Assert.Equal("first paycheck", tx1.Memo);
		Assert.Equal("Microsoft", tx1.Payee);
		Assert.Equal(ClearedState.Reconciled, tx1.Cleared);
		Assert.Equal(500, tx1.Balance);

		BankingTransactionViewModel tx2 = checking.Transactions[1];
		Assert.Equal(new DateTime(637834085888150235), tx2.When);
		Assert.Equal(76, tx2.CheckNumber);
		Assert.Equal(-123.45m, tx2.Amount);
		Assert.Equal("memo1", tx2.Memo);
		Assert.Equal("payee1", tx2.Payee);
		Assert.Equal(ClearedState.Cleared, tx2.Cleared);
		Assert.Equal(376.55m, tx2.Balance);

		BankingTransactionViewModel tx3a = checking.Transactions[2];
		Assert.Equal(new DateTime(637834085889150235), tx3a.When);
		Assert.Null(tx3a.CheckNumber);
		Assert.Equal(-100, tx3a.Amount);
		Assert.Equal("transfer", tx3a.Memo);
		Assert.Null(tx3a.Payee);
		Assert.Equal(ClearedState.None, tx3a.Cleared);
		Assert.Equal(276.55m, tx3a.Balance);

		Assert.Equal(1, savings.Transactions.Count(tx => tx.IsPersisted));
		BankingTransactionViewModel tx3b = savings.Transactions[0];
		Assert.Equal(new DateTime(637834085889150235), tx3b.When);
		Assert.Null(tx3b.CheckNumber);
		Assert.Equal(100, tx3b.Amount);
		Assert.Equal("transfer", tx3b.Memo);
		Assert.Null(tx3b.Payee);
		Assert.Equal(ClearedState.None, tx3b.Cleared);
		Assert.Equal(100, tx3b.Balance);
	}

	[Fact]
	public void UpgradeFromV2()
	{
		using SQLiteConnection connection = this.CreateDatabase(2);
		string sql = @"
INSERT INTO Account (Name, IsClosed) VALUES ('Checking', 0);
INSERT INTO Account (Name, IsClosed) VALUES ('Savings', 0);

INSERT INTO Category (Name) VALUES ('cat1');
INSERT INTO Category (Name) VALUES ('cat2');

INSERT INTO [Transaction] ([Id], [ParentTransactionId], [When], Amount, Memo, Payee, CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (1, NULL, 637834085886150235, 0,  'top memo', 'Microsoft', -1,    1, 1, 1);
INSERT INTO [Transaction] ([Id], [ParentTransactionId], [When], Amount, Memo,        CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (2, 1,    637834085886150235, 3,  'split1',                 1, NULL, 1, 0);
INSERT INTO [Transaction] ([Id], [ParentTransactionId], [When], Amount, Memo,        CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (3, 1,    637834085886150235, 7,  'split2',                 2, NULL, 1, 0);
";
		this.ExecuteSql(connection, sql);
		MoneyFile file = MoneyFile.Load(connection);
		DocumentViewModel documentViewModel = new(file);

		// Account assertions
		Assert.Equal(2, documentViewModel.AccountsPanel.Accounts.Count);
		var checking = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Checking");
		var savings = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Savings");

		// Category assertions
		CategoryAccountViewModel cat1 = Assert.Single(documentViewModel.CategoriesPanel.Categories, cat => cat.Name == "cat1");
		CategoryAccountViewModel cat2 = Assert.Single(documentViewModel.CategoriesPanel.Categories, cat => cat.Name == "cat2");

		Assert.Equal(1, checking.Transactions.Count(tx => tx.IsPersisted));
		BankingTransactionViewModel tx1 = checking.Transactions[0];
		Assert.Equal(new DateTime(637834085886150235), tx1.When);
		Assert.Equal(-10, tx1.Amount);
		Assert.Equal("top memo", tx1.Memo);
		Assert.Equal("Microsoft", tx1.Payee);
		Assert.Equal(ClearedState.Cleared, tx1.Cleared);
		Assert.Equal(-10, tx1.Balance);

		Assert.Equal(2, tx1.Splits.Count(s => s.IsPersisted));
		TransactionEntryViewModel split1 = tx1.Splits[0];
		Assert.Equal(-3, split1.Amount);
		Assert.Equal("split1", split1.Memo);
		Assert.Same(cat1, split1.Account);
		Assert.Equal(ClearedState.Cleared, split1.Cleared); // V2 didn't store cleared status per-split, so it inherits from the parent

		TransactionEntryViewModel split2 = tx1.Splits[1];
		Assert.Equal(-7, split2.Amount);
		Assert.Equal("split2", split2.Memo);
		Assert.Same(cat2, split2.Account);
		Assert.Equal(ClearedState.Cleared, split2.Cleared); // V2 didn't store cleared status per-split, so it inherits from the parent
	}

	private SQLiteConnection CreateDatabase(int schemaVersion)
	{
		SQLiteConnection connection = new(Debugger.IsAttached ? this.dbPath : ":memory:");
		DatabaseSchemaManager.Upgrade(connection, schemaVersion);
		return connection;
	}

	private void ExecuteSql(SQLiteConnection connection, string sql)
	{
		int result = SQLitePCL.raw.sqlite3_exec(connection.Handle, sql);
		if (result is not SQLitePCL.raw.SQLITE_OK or SQLitePCL.raw.SQLITE_DONE)
		{
			string errMsg = SQLitePCL.raw.sqlite3_errmsg(connection.Handle).utf8_to_string();
			throw new Exception(errMsg);
		}
	}
}
