// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;
using Nerdbank.MoneyManagement.Adapters;

public class QifAdapterFacts : AdapterTestBase<QifAdapter>
{
	private const string Simple1DataFileName = "Simple1.qif";
	private const string CategoriesDataFileName = "categories.qif";
	private const string SecuritiesDataFileName = "securities.qif";
	private const string RealWorldSamplesDataFileName = "RealWorldSamples.qif";
	private const string BankAndInvestmentTransactionsDataFileName = "BankAndInvestmentTransactions.qif";
	private QifAdapter adapter;

	public QifAdapterFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.adapter = new QifAdapter(this.DocumentViewModel);
	}

	protected override QifAdapter Adapter => this.adapter;

	[Fact]
	public async Task ImportAsync_ValidatesArgs()
	{
		await Assert.ThrowsAsync<ArgumentNullException>("filePath", () => this.Adapter.ImportAsync(null!, this.TimeoutToken));
		await Assert.ThrowsAsync<ArgumentException>("filePath", () => this.Adapter.ImportAsync(string.Empty, this.TimeoutToken));
	}

	[Fact]
	public async Task ImportTransactions()
	{
		int chooseAccountFuncInvocationCount = 0;
		this.UserNotification.ChooseAccountFunc = (string prompt, AccountViewModel? defaultAccount, CancellationToken cancellationToken) =>
		{
			chooseAccountFuncInvocationCount++;
			return Task.FromResult<AccountViewModel?>(this.Checking);
		};
		int count = await this.ImportAsync(Simple1DataFileName);
		Assert.Equal(1, chooseAccountFuncInvocationCount);
		Assert.Equal(6, count); // Transactions + Categories
		Assert.Equal(3, this.Checking.Transactions.Count(t => t.IsPersisted));
		Assert.Equal(3, this.DocumentViewModel.CategoriesPanel.Categories.Count(t => t.IsPersisted));

		BankingTransactionViewModel tx1 = this.Checking.Transactions[0];
		Assert.Equal(ExpectedDateTime(2022, 3, 24), tx1.When);
		Assert.Equal(-17.35m, tx1.Amount);
		Assert.Equal("Spotify", tx1.Payee);
		Assert.True(string.IsNullOrEmpty(tx1.Memo));
		Assert.Equal(ClearedState.Reconciled, tx1.Cleared);

		BankingTransactionViewModel tx2 = this.Checking.Transactions[1];
		Assert.Equal(ExpectedDateTime(2022, 3, 24), tx2.When);
		Assert.Equal(-671.00m, tx2.Amount);
		Assert.Equal("Xtreme Xperience Llc", tx2.Payee);
		Assert.Equal("gift for Fuzzy", tx2.Memo);
		Assert.Equal(ClearedState.Cleared, tx2.Cleared);

		BankingTransactionViewModel tx3 = this.Checking.Transactions[2];
		Assert.Equal(ExpectedDateTime(2022, 3, 25), tx3.When);
		Assert.Equal(-11.10m, tx3.Amount);
		Assert.Equal("Whole Foods", tx3.Payee);
		Assert.Equal("5146: WHOLEFDS RMD 10260 REDMOND WA 98052 US", tx3.Memo);
		Assert.Equal(ClearedState.None, tx3.Cleared);

		static DateTime ExpectedDateTime(int year, int month, int day) => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
	}

	[Fact]
	public async Task ImportCategories()
	{
		int count = await this.ImportAsync(CategoriesDataFileName);
		Assert.Equal(3, count);

		Assert.Equal(3, this.DocumentViewModel.CategoriesPanel.Categories.Count);
		Assert.Equal("Bonus", this.DocumentViewModel.CategoriesPanel.Categories[0].Name);
		Assert.Equal("Citi Cards Credit Card", this.DocumentViewModel.CategoriesPanel.Categories[1].Name);
		Assert.Equal("Consulting", this.DocumentViewModel.CategoriesPanel.Categories[2].Name);

		// Delete a category and re-import to see if it will import just the missing one.
		this.DocumentViewModel.CategoriesPanel.DeleteCategory(this.DocumentViewModel.CategoriesPanel.Categories[1]);
		count = await this.ImportAsync(CategoriesDataFileName);
		Assert.Equal(1, count);
		Assert.Equal("Citi Cards Credit Card", this.DocumentViewModel.CategoriesPanel.Categories[1].Name);
	}

	/// <summary>
	/// Verifies that a transfer where to and from are the same still gets imported,
	/// since this is how Quicken represents an account's opening balance.
	/// </summary>
	[Fact]
	public async Task OpeningBalanceRetained()
	{
		await this.ImportAsync(RealWorldSamplesDataFileName);
		BankingAccountViewModel? myHouse = (BankingAccountViewModel?)this.DocumentViewModel.GetAccount("My House");
		Assert.Equal(2, myHouse?.Transactions.Count(tx => tx.IsPersisted));
		Assert.Equal("Opening Balance", myHouse!.Transactions[0].Payee);
		Assert.Equal(10_000, myHouse.Transactions[0].Amount);
		Assert.Null(myHouse.Transactions[0].OtherAccount);
		Assert.Equal(-1_000, myHouse.Transactions[1].Amount);
		Assert.Equal("Mortgage Payment", myHouse.Transactions[1].OtherAccount?.Name);
	}

	[Fact]
	public async Task InvestmentBasics()
	{
		await this.ImportAsync(BankAndInvestmentTransactionsDataFileName);
		Asset? msft = this.DocumentViewModel.AssetsPanel.FindAsset("Microsoft");
		Assert.NotNull(msft);
		InvestingAccountViewModel? brokerage = (InvestingAccountViewModel?)this.DocumentViewModel.GetAccount("Brokerage");
		Assert.Equal(8, brokerage?.Transactions.Count(tx => tx.IsPersisted));

		int transactionCounter = 0;

		InvestingTransactionViewModel tx = brokerage!.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2006, 6, 13), tx.When);
		Assert.Equal(2000, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Deposit, tx.Action);
		Assert.Null(tx.SimpleAccount);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2006, 10, 4), tx.When);
		Assert.Equal(500, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Transfer, tx.Action);
		Assert.Same(this.Checking, tx.SimpleAccount);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2006, 10, 5), tx.When);
		Assert.Equal(4, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Buy, tx.Action);
		Assert.Equal(115.09m, tx.SimplePrice);
		Assert.Same(msft, tx.SimpleAsset?.Model);
		Assert.Equal("YOU BOUGHT           ESPP###", tx.Memo);
		////Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2006, 10, 6), tx.When);
		Assert.Equal(2, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Sell, tx.Action);
		Assert.Equal(45.9355m, tx.SimplePrice); // 41.2655m after commission is supported
		Assert.Same(msft, tx.SimpleAsset?.Model);
		Assert.Equal("YOU SOLD", tx.Memo);
		////Assert.Equal(9.34m, tx.Commission);
		////Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2008, 1, 4), tx.When);
		Assert.Equal(-1000, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Transfer, tx.Action);
		Assert.Same(this.Checking, tx.SimpleAccount);
		////Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2013, 4, 12), tx.When);
		Assert.Equal(-105, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Transfer, tx.Action);
		Assert.Same(this.Checking, tx.SimpleAccount);
		////Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2021, 4, 11), tx.When);
		Assert.Equal(1.99m, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Interest, tx.Action);
		////Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2021, 4, 30), tx.When);
		Assert.Equal(1.23m, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Dividend, tx.Action);
		Assert.Equal("PIMCO INCOME FUND CL D", tx.RelatedAsset?.Name);
		Assert.Equal("DIVIDEND RECEIVED", tx.Memo);
		////Assert.Equal(ClearedState.Reconciled, tx.Cleared);
	}

	[Fact]
	public async Task ImportSecurities()
	{
		int importedCount = await this.ImportAsync(SecuritiesDataFileName);
		Assert.Equal(5, importedCount);

		AssetViewModel? msft = this.DocumentViewModel.AssetsPanel.FindAssetByTicker("MSFT");
		Assert.Equal("Microsoft", msft?.Name);
		Assert.Equal(Asset.AssetType.Security, msft?.Type);

		AssetViewModel? magjx = this.DocumentViewModel.AssetsPanel.FindAssetByTicker("MAGJX");
		Assert.Equal("MFS Growth Allocation Fund Class R4", magjx?.Name);
		Assert.Equal(Asset.AssetType.Security, magjx?.Type);
	}

	[Fact]
	public async Task ImportPrices()
	{
		int importedCount = await this.ImportAsync(SecuritiesDataFileName);
		Assert.Equal(5, importedCount);

		AssetViewModel? msft = this.DocumentViewModel.AssetsPanel.FindAssetByTicker("MSFT");
		this.DocumentViewModel.AssetsPanel.SelectedAsset = msft;
		List<(DateTime When, decimal Price)> actualPrices = this.DocumentViewModel.AssetsPanel.AssetPrices.Select(vm => (vm.When, vm.Price)).ToList();
		List<(DateTime When, decimal Price)> expectedPrices = new()
		{
			(new DateTime(2015, 3, 2), 11.94m),
			(new DateTime(2015, 3, 3), 11.91m),
		};

		// The Prices table may be huge, and loading it all to understand how many records were actually unique is probably not worth it.
		// As a result, the imported count may include duplicate price records.
		importedCount = await this.ImportAsync(SecuritiesDataFileName);
		Assert.Equal(2, importedCount);
	}

	protected override void RefetchViewModels()
	{
		base.RefetchViewModels();
		this.adapter = new QifAdapter(this.DocumentViewModel);
	}
}
