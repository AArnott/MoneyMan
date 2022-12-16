// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.ViewModels;
using NuGet.Frameworks;
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
		Assert.Equal(MoneyTestBase.DefaultCategoryCount + 2, documentViewModel.CategoriesPanel.Categories.Count);
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

INSERT INTO [Transaction] ([Id], [ParentTransactionId], [When], Amount, Memo, Payee, CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (4, NULL, 637835085886150235, 0,  'top memo', 'Microsoft', -1, 1, 1,    2);
INSERT INTO [Transaction] ([Id], [ParentTransactionId], [When], Amount, Memo,        CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (5, 4,    637835085886150235, 2,  'split1',                 1, 1, NULL, 0);
INSERT INTO [Transaction] ([Id], [ParentTransactionId], [When], Amount, Memo,        CategoryId, CreditAccountId, DebitAccountId, Cleared) VALUES (6, 4,    637835085886150235, 6,  'split2',                 2, 1, NULL, 0);
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

		Assert.Equal(2, checking.Transactions.Count(tx => tx.IsPersisted));
		BankingTransactionViewModel tx1 = checking.Transactions[0];
		Assert.Equal(new DateTime(637834085886150235), tx1.When);
		Assert.Equal(-10, tx1.Amount);
		Assert.Equal("top memo", tx1.Memo);
		Assert.Equal("Microsoft", tx1.Payee);
		Assert.Equal(ClearedState.Cleared, tx1.Cleared);
		Assert.Equal(-10, tx1.Balance);

		Assert.Equal(2, tx1.Splits.Count(s => s.IsPersisted));
		TransactionEntryViewModel split1a = tx1.Splits[0];
		Assert.Equal(-3, split1a.Amount);
		Assert.Equal("split1", split1a.Memo);
		Assert.Same(cat1, split1a.Account);
		Assert.Equal(ClearedState.Cleared, split1a.Cleared); // V2 didn't store cleared status per-split, so it inherits from the parent

		TransactionEntryViewModel split1b = tx1.Splits[1];
		Assert.Equal(-7, split1b.Amount);
		Assert.Equal("split2", split1b.Memo);
		Assert.Same(cat2, split1b.Account);
		Assert.Equal(ClearedState.Cleared, split1b.Cleared); // V2 didn't store cleared status per-split, so it inherits from the parent

		BankingTransactionViewModel tx2 = checking.Transactions[1];
		Assert.Equal(new DateTime(637835085886150235), tx2.When);
		Assert.Equal(8, tx2.Amount);
		Assert.Equal("top memo", tx2.Memo);
		Assert.Equal("Microsoft", tx2.Payee);
		Assert.Equal(ClearedState.Reconciled, tx2.Cleared);
		Assert.Equal(-2, tx2.Balance);

		Assert.Equal(2, tx2.Splits.Count(s => s.IsPersisted));
		TransactionEntryViewModel split2a = tx2.Splits[0];
		Assert.Equal(2, split2a.Amount);
		Assert.Equal("$2.00", split2a.AmountFormatted);
		Assert.Equal("split1", split2a.Memo);
		Assert.Same(cat1, split2a.Account);
		Assert.Equal(ClearedState.Reconciled, split2a.Cleared); // V2 didn't store cleared status per-split, so it inherits from the parent

		TransactionEntryViewModel split2b = tx2.Splits[1];
		Assert.Equal(6, split2b.Amount);
		Assert.Equal("split2", split2b.Memo);
		Assert.Same(cat2, split2b.Account);
		Assert.Equal(ClearedState.Reconciled, split2b.Cleared); // V2 didn't store cleared status per-split, so it inherits from the parent
	}

	[Fact]
	public void UpgradeFromV4()
	{
		using SQLiteConnection connection = this.CreateDatabase(4);
		string sql = @"
INSERT INTO Account (Id, Name, Type, IsClosed) VALUES (2, 'Checking', 0, 0);
INSERT INTO Account (Id, Name, Type, IsClosed) VALUES (1, 'Brokerage', 1, 0);

INSERT INTO Asset (Id, Name, TickerSymbol, Type, CurrencySymbol, CurrencyDecimalDigits) VALUES (9, 'Bitcoin', 'BTC', 1, 'B', 9);

INSERT INTO [Transaction] ([When], Action, Memo, CreditAccountId, CreditAmount, CreditAssetId, DebitAccountId, DebitAmount, DebitAssetId) VALUES (637834085886150235, 4, 'top memo', 1, 3, 9, 1, 90, 1);
INSERT INTO [Transaction] ([When], Action, Memo, DebitAccountId, DebitAmount, DebitAssetId) VALUES (637834085886150236, 2, 'top memo', 1, 2, 9);
INSERT INTO [Transaction] ([When], Action, Memo, CreditAccountId, CreditAmount, CreditAssetId, DebitAccountId, DebitAmount, DebitAssetId) VALUES (637834075886150236, 3, 'xferFromChecking', 1, 100, 1, 2, 100, 1);
";
		this.ExecuteSql(connection, sql);
		MoneyFile file = MoneyFile.Load(connection);
		DocumentViewModel documentViewModel = new(file);
		Assert.Equal(1, documentViewModel.ConfigurationPanel.PreferredAsset!.Id);

		AssetViewModel btc = Assert.Single(documentViewModel.AssetsPanel.Assets, a => a.Name == "Bitcoin");
		Assert.Equal("BTC", btc.TickerSymbol);
		Assert.Equal("Bitcoin", btc.Name);

		var brokerage = (InvestingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Brokerage");
		var checking = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Checking");

		Assert.Equal(3, brokerage.Transactions.Count(tx => tx.IsPersisted));
		InvestingTransactionViewModel tx1 = brokerage.Transactions[0];
		Assert.Equal(new DateTime(637834075886150236), tx1.When);
		Assert.Equal(TransactionAction.Transfer, tx1.Action);
		Assert.Equal(100, tx1.SimpleAmount);
		Assert.Equal(file.PreferredAssetId, tx1.SimpleAsset!.Id);
		Assert.Same(checking, tx1.SimpleAccount);
		Assert.Equal("xferFromChecking", tx1.Memo);

		InvestingTransactionViewModel tx2 = brokerage.Transactions[1];
		Assert.Equal(new DateTime(637834085886150235), tx2.When);
		Assert.Equal(TransactionAction.Buy, tx2.Action);
		Assert.Equal(3, tx2.SimpleAmount);
		Assert.Same(btc, tx2.SimpleAsset);
		Assert.Equal("top memo", tx2.Memo);

		InvestingTransactionViewModel tx3 = brokerage.Transactions[2];
		Assert.Equal(new DateTime(637834085886150236), tx3.When);
		Assert.Equal(TransactionAction.Withdraw, tx3.Action);
		Assert.Equal(2, tx3.SimpleAmount);
		Assert.Same(btc, tx3.SimpleAsset);
		Assert.Equal("top memo", tx3.Memo);
	}

	[Fact]
	public void UpgradeFromV5()
	{
		using SQLiteConnection connection = this.CreateDatabase(5);
		string sql = @"
INSERT INTO Account (Id, Name, Type, IsClosed) VALUES (2, 'Checking', 0, 0);
INSERT INTO Account (Id, Name, Type, IsClosed) VALUES (9, 'Salary',   2, 0);

INSERT INTO [Transaction] (Id, [When], Action, Memo) VALUES (1, 637834085886150235, 3, 'top memo');
INSERT INTO TransactionEntry (TransactionId, Memo, AccountId, Amount, AssetId, Cleared) VALUES (1, 'm1', 2, 123, 1, 1);
INSERT INTO TransactionEntry (TransactionId, Memo, AccountId, Amount, AssetId, Cleared) VALUES (1, NULL, 9, -123, 1, 1);
"
;
		this.ExecuteSql(connection, sql);
		MoneyFile file = MoneyFile.Load(connection);
		DocumentViewModel documentViewModel = new(file);

		var checking = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Checking");
		BankingTransactionViewModel tx1 = checking.Transactions[0];
		Assert.Equal(123, tx1.Amount);
		Assert.Equal(123, tx1.Balance);
		Assert.Equal(9, tx1.OtherAccount?.Id);
		Assert.False(tx1.ContainsSplits);
	}

	[Fact]
	public void UpgradeFromV6()
	{
		using SQLiteConnection connection = this.CreateDatabase(6);
		string sql = @"
INSERT INTO Account (Id, Name, Type, IsClosed, OfxBankId, OfxAcctId) VALUES (2, 'Checking', 0, 0, 'routnum', 'accountnum');

INSERT INTO [Transaction] (Id, [When], Action, Memo) VALUES (1, 637834085886150235, 3, 'top memo');
INSERT INTO TransactionEntry (TransactionId, Memo, AccountId, Amount, AssetId, Cleared, OfxFitId) VALUES (1, 'm1', 2, 123, 1, 1, 'fitid');
"
;
		this.ExecuteSql(connection, sql);
		MoneyFile file = MoneyFile.Load(connection);
		DocumentViewModel documentViewModel = new(file);

		var checking = (BankingAccountViewModel)Assert.Single(documentViewModel.AccountsPanel.Accounts, a => a.Name == "Checking");
		Assert.Equal("routnum", checking.OfxBankId);
		Assert.Equal("accountnum", checking.OfxAcctId);
		BankingTransactionViewModel tx1 = checking.Transactions[0];
		Assert.Equal("fitid", tx1.Entries[0].OfxFitId);
	}

	[Fact]
	public void UpgradeFromV7()
	{
		using SQLiteConnection connection = this.CreateDatabase(7);
		string sql = @"
INSERT INTO Account (Name, IsClosed, Type) VALUES ('My commission', 0, 2);
UPDATE Configuration SET CommissionAccountId = last_insert_rowid();
";

		this.ExecuteSql(connection, sql);
		MoneyFile file = MoneyFile.Load(connection);
		DocumentViewModel documentViewModel = new(file);

		Assert.Equal("My commission", documentViewModel.ConfigurationPanel.CommissionCategory?.Name);
	}

	[Fact]
	public void UpgradeFromV8()
	{
		using SQLiteConnection connection = this.CreateDatabase(8);
		string sql = @"
INSERT INTO [Account] ([Id], [Name], [IsClosed], [Type], [CurrencyAssetId]) VALUES (100, 'Brokerage', 0, 1, 1);
INSERT INTO [Asset] ([Id], [Name]) VALUES (101, 'MSFT');

-- Add first lot with cost basis
INSERT INTO [Transaction] ([Id], [When], [Action]) VALUES (200, 1234, 9);
INSERT INTO [TransactionEntry] ([Id], [TransactionId], [AccountId], [Amount], [AssetId]) VALUES (201, 200, 100, 7, 101);
INSERT INTO [TaxLot] ([Id], [CreatingTransactionEntryId], [AcquiredDate], [Amount], [CostBasisAmount], [CostBasisAssetId]) VALUES (210, 201, 1000, 7, 35, 1);

-- Add second lot with cost basis
INSERT INTO [Transaction] ([Id], [When], [Action]) VALUES (300, 1468, 9);
INSERT INTO [TransactionEntry] ([Id], [TransactionId], [AccountId], [Amount], [AssetId]) VALUES (301, 300, 100, 9, 101);
INSERT INTO [TaxLot] ([Id], [CreatingTransactionEntryId], [AcquiredDate], [CostBasisAmount], [CostBasisAssetId]) VALUES (310, 301, 800, 50, 1);

-- Sell all of one tax lot and part of another tax lot.
INSERT INTO [Transaction] ([Id], [When], [Action]) VALUES (400, 2000, 5);
INSERT INTO [TransactionEntry] ([Id], [TransactionId], [AccountId], [Amount], [AssetId]) VALUES (401, 400, 100, -9, 101);
INSERT INTO [TransactionEntry] ([Id], [TransactionId], [AccountId], [Amount], [AssetId]) VALUES (402, 400, 100, 200, 1);
INSERT INTO [TaxLotAssignment] ([Id], [TaxLotId], [ConsumingTransactionEntryId], [Amount], [Pinned]) VALUES (410, 210, 401, -7, 1);
INSERT INTO [TaxLotAssignment] ([Id], [TaxLotId], [ConsumingTransactionEntryId], [Amount], [Pinned]) VALUES (411, 310, 401, -2, 0);
";
		this.ExecuteSql(connection, sql);
		MoneyFile file = MoneyFile.Load(connection);
		DocumentViewModel documentViewModel = new(file);

		var brokerage = (InvestingAccountViewModel)documentViewModel.GetAccount(100);
		InvestingTransactionViewModel? addTx1 = brokerage.FindTransaction(200);
		Assert.NotNull(addTx1);
		TransactionEntryViewModel addTx1Entry = addTx1.Entries.Single(e => e.Id == 201);
		Assert.Equal(35, addTx1Entry.CreatedTaxLots?.Single().CostBasisAmount);
		Assert.Equal(7, addTx1Entry.CreatedTaxLots?.Single().Amount);
		Assert.Equal(brokerage.CurrencyAsset, addTx1Entry.CreatedTaxLots?.Single().CostBasisAsset);

		TableQuery<TaxLotAssignment> taxLotAssignments = file.GetTaxLotAssignments(401);
		TaxLotAssignment tla410 = Assert.Single(taxLotAssignments, tla => tla.Amount == -7);
		TaxLotAssignment tla411 = Assert.Single(taxLotAssignments, tla => tla.Amount == -2);
		Assert.True(tla410.Pinned);
		Assert.False(tla411.Pinned);
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
