// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.Adapters;

public class QifAdapterFacts : AdapterTestBase<QifAdapter>
{
	private const string Simple1DataFileName = "Simple1.qif";
	private const string CategoriesDataFileName = "categories.qif";
	private const string SecuritiesDataFileName = "securities.qif";
	private const string RealWorldSamplesDataFileName = "RealWorldSamples.qif";
	private const string BankAndInvestmentTransactionsDataFileName = "BankAndInvestmentTransactions.qif";
	private const string TransfersDataFileName = "Transfers.qif";
	private QifAdapter adapter;

	public QifAdapterFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.adapter = new QifAdapter(this.DocumentViewModel);
		this.Adapter.TraceSource.Listeners.Add(new XunitTraceListener(this.Logger));
		this.EnableSqlLogging();
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
		const int ExpectedCategoriesCount = 4;
		Assert.Equal(DefaultCategoryCount + ExpectedCategoriesCount, this.DocumentViewModel.CategoriesPanel.Categories.Count(t => t.IsPersisted));

		int transactionIndex = 0;
		BankingTransactionViewModel tx = this.Checking.Transactions[transactionIndex++];
		Assert.Equal(ExpectedDateTime(2022, 3, 24), tx.When);
		Assert.Equal(-17.35m, tx.Amount);
		Assert.Equal("Spotify", tx.Payee);
		Assert.True(string.IsNullOrEmpty(tx.Memo));
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = this.Checking.Transactions[transactionIndex++];
		Assert.Equal(ExpectedDateTime(2022, 3, 24), tx.When);
		Assert.Equal(-671.00m, tx.Amount);
		Assert.Equal("Xtreme Xperience Llc", tx.Payee);
		Assert.Equal("gift for Fuzzy", tx.Memo);
		Assert.Equal(ClearedState.Cleared, tx.Cleared);

		tx = this.Checking.Transactions[transactionIndex++];
		Assert.Equal(ExpectedDateTime(2022, 3, 25), tx.When);
		Assert.Equal(-11.10m, tx.Amount);
		Assert.Equal("Whole Foods", tx.Payee);
		Assert.Equal("5146: WHOLEFDS RMD 10260 REDMOND WA 98052 US", tx.Memo);
		Assert.Equal(ClearedState.None, tx.Cleared);

		tx = this.Checking.Transactions[transactionIndex++];
		Assert.Equal(ExpectedDateTime(2022, 5, 15), tx.When);
		Assert.Equal(12.81m, tx.Amount);
		Assert.Equal("SPEEDOUSA.COM 888-4SPEEDO", tx.Payee);
		Assert.Equal("Gifts Given", tx.OtherAccount?.Name);
		Assert.Null(tx.Memo);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		Assert.Equal(transactionIndex, this.Checking.Transactions.Count(t => t.IsPersisted));
		Assert.Equal(transactionIndex + ExpectedCategoriesCount, count);
		static DateTime ExpectedDateTime(int year, int month, int day) => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
	}

	[Fact]
	public async Task ImportCategories()
	{
		int count = await this.ImportAsync(CategoriesDataFileName);
		Assert.Equal(3, count);

		Assert.Equal(DefaultCategoryCount + 3, this.DocumentViewModel.CategoriesPanel.Categories.Count);
		Assert.Equal("Bonus", this.DocumentViewModel.CategoriesPanel.Categories[0].Name);
		Assert.Equal("Citi Cards Credit Card", this.DocumentViewModel.CategoriesPanel.Categories[1].Name);
		Assert.Equal("Commission", this.DocumentViewModel.CategoriesPanel.Categories[2].Name);
		Assert.Equal("Consulting", this.DocumentViewModel.CategoriesPanel.Categories[3].Name);

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
	public async Task Transfers()
	{
		await this.ImportAsync(TransfersDataFileName);
		BankingAccountViewModel checking = Assert.IsType<BankingAccountViewModel>(this.DocumentViewModel.GetAccount("Checking"));
		InvestingAccountViewModel brokerage1 = Assert.IsType<InvestingAccountViewModel>(this.DocumentViewModel.GetAccount("Brokerage 1"));
		InvestingAccountViewModel brokerage2 = Assert.IsType<InvestingAccountViewModel>(this.DocumentViewModel.GetAccount("Brokerage 2"));
		AssetViewModel? msft = this.DocumentViewModel.AssetsPanel.FindAsset("Microsoft Corp");
		Assert.NotNull(msft);

		// Checking account assertions
		Assert.Equal(2, checking.Transactions.Count(t => t.IsPersisted));
		Assert.Equal(100, checking.Transactions[0].Amount);
		Assert.Equal(-80, checking.Transactions[1].Amount);
		Assert.Same(brokerage1, checking.Transactions[1].OtherAccount);
		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(checking);
		Assert.Equal(20m, balances[checking.CurrencyAsset!.Id]);

		// Brokerage 1 account assertions
		Assert.Equal(4, brokerage1.Transactions.Count(t => t.IsPersisted));

		Assert.Equal(TransactionAction.Transfer, brokerage1.Transactions[0].Action);
		Assert.Equal(80, brokerage1.Transactions[0].DepositAmount);
		Assert.Same(checking, brokerage1.Transactions[0].WithdrawAccount);

		Assert.Equal(TransactionAction.Buy, brokerage1.Transactions[1].Action);
		Assert.Equal(2, brokerage1.Transactions[1].DepositAmount);

		Assert.Equal(TransactionAction.Transfer, brokerage1.Transactions[2].Action);
		Assert.Equal(1, brokerage1.Transactions[2].WithdrawAmount);
		Assert.Same(msft, brokerage1.Transactions[2].WithdrawAsset);
		Assert.Same(brokerage2, brokerage1.Transactions[2].DepositAccount);

		Assert.Equal(TransactionAction.Transfer, brokerage1.Transactions[3].Action);
		Assert.Equal(10, brokerage1.Transactions[3].WithdrawAmount);
		Assert.Equal(10, brokerage1.Transactions[3].DepositAmount);
		Assert.Same(brokerage2, brokerage1.Transactions[3].DepositAccount);

		balances = this.Money.GetBalances(brokerage1);
		Assert.Equal(10m, balances[brokerage1.CurrencyAsset!.Id]);
		Assert.Equal(1, balances[msft.Id]);

		// Brokerage 2 account assertions
		Assert.Equal(2, brokerage2.Transactions.Count(t => t.IsPersisted));

		Assert.Equal(TransactionAction.Transfer, brokerage2.Transactions[0].Action);
		Assert.Equal(1, brokerage2.Transactions[0].DepositAmount);
		Assert.Same(msft, brokerage2.Transactions[0].DepositAsset);
		Assert.Same(brokerage1, brokerage2.Transactions[0].WithdrawAccount);

		Assert.Equal(TransactionAction.Transfer, brokerage2.Transactions[1].Action);
		Assert.Equal(10, brokerage2.Transactions[1].DepositAmount);
		Assert.Same(brokerage1, brokerage2.Transactions[1].WithdrawAccount);

		balances = this.Money.GetBalances(brokerage2);
		Assert.Equal(10m, balances[brokerage2.CurrencyAsset!.Id]);
		Assert.Equal(1, balances[msft.Id]);
	}

	[Fact]
	public async Task InvestmentBasics()
	{
		await this.ImportAsync(BankAndInvestmentTransactionsDataFileName);
		Asset? msft = this.DocumentViewModel.AssetsPanel.FindAsset("Microsoft");
		Assert.NotNull(msft);
		InvestingAccountViewModel? brokerage = (InvestingAccountViewModel?)this.DocumentViewModel.GetAccount("Brokerage");
		Assert.Equal(19, brokerage?.Transactions.Count(tx => tx.IsPersisted));

		int transactionCounter = 0;

		InvestingTransactionViewModel tx = brokerage!.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2006, 6, 13), tx.When);
		Assert.Equal(2000, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Deposit, tx.Action);
		Assert.Equal("NetBank Checking", tx.SimpleAccount?.Name);
		Assert.Same(tx.WithdrawAccount, tx.SimpleAccount);

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
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2006, 10, 6), tx.When);
		Assert.Equal(2, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Sell, tx.Action);
		Assert.Equal(41.2655m, tx.SimplePrice);
		Assert.Same(msft, tx.SimpleAsset?.Model);
		Assert.Equal("YOU SOLD", tx.Memo);
		Assert.Equal(9.34m, tx.Commission);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		// A BuyX record that used money from a category.
		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2007, 5, 9), tx.When);
		Assert.Equal(20, tx.SimpleAmount);
		Assert.Equal(30.75m, tx.SimplePrice);
		Assert.Equal(TransactionAction.Buy, tx.Action);
		Assert.Equal("Microsoft Stock Awards (Cash)", tx.SimpleAccount?.Name);
		Assert.Equal(Account.AccountType.Category, tx.SimpleAccount?.Type);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2007, 10, 8), tx.When);
		Assert.Equal(100, tx.SimpleAmount);
		Assert.Equal(1.3999m, tx.SimplePrice);
		Assert.Equal(TransactionAction.ShortSale, tx.Action);
		Assert.Equal("CALL (MSQ) MICROSOFT CORP JAN 30 (100 SHS)", tx.SimpleAsset?.Name);
		Assert.Equal(8, tx.Commission);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2007, 10, 26), tx.When);
		Assert.Equal(100, tx.SimpleAmount);
		Assert.Equal(5.7075m, tx.SimplePrice);
		Assert.Equal(TransactionAction.CoverShort, tx.Action);
		Assert.Equal("CALL (MSQ) MICROSOFT CORP JAN 30 (100 SHS)", tx.SimpleAsset?.Name);
		Assert.Equal(8, tx.Commission);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2007, 12, 14), tx.When);
		Assert.Equal(TransactionAction.Dividend, tx.Action);
		Assert.Equal(0.101m, tx.SimpleAmount);
		Assert.Equal(19.31m, tx.SimplePrice);
		Assert.Equal("Janus Contrarian Fund", tx.SimpleAsset?.Name);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2007, 12, 15), tx.When);
		Assert.Equal(TransactionAction.Dividend, tx.Action);
		Assert.Equal(10.607m, tx.SimpleAmount);
		Assert.Equal(19.29m, tx.SimplePrice);
		Assert.Equal("Janus Contrarian Fund", tx.SimpleAsset?.Name);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2008, 1, 4), tx.When);
		Assert.Equal(-1000, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Transfer, tx.Action);
		Assert.Same(this.Checking, tx.SimpleAccount);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2013, 4, 12), tx.When);
		Assert.Equal(-105, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Transfer, tx.Action);
		Assert.Same(this.Checking, tx.SimpleAccount);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2014, 4, 1), tx.When);
		Assert.Equal(2, tx.SimpleAmount);
		Assert.Equal("Fidelity International Real Estate", tx.SimpleAsset?.Name);
		Assert.Equal(TransactionAction.Add, tx.Action);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2014, 4, 4), tx.When);
		Assert.Equal(0.5m, tx.SimpleAmount);
		Assert.Equal("SPARTAN 500 INDEX FD ADVANTAGE CLASS (FUSVX)", tx.SimpleAsset?.Name);
		Assert.Equal(50m, tx.SimplePrice);
		Assert.Equal(TransactionAction.Dividend, tx.Action);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2017, 5, 16), tx.When);
		Assert.Equal(1.8m, tx.SimpleAmount);
		Assert.Equal("NH PORTFOLIO 2030 (FIDELITY FUNDS)", tx.SimpleAsset?.Name);
		Assert.Equal(17.10m, tx.SimplePrice);
		Assert.Equal(TransactionAction.Buy, tx.Action);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2017, 5, 17), tx.When);
		Assert.Equal(1.8m, tx.SimpleAmount);
		Assert.Equal("NH PORTFOLIO 2030 (FIDELITY FUNDS)", tx.SimpleAsset?.Name);
		Assert.Equal(17.50m, tx.SimplePrice);
		Assert.Equal(TransactionAction.Sell, tx.Action);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2021, 1, 31), tx.When);
		Assert.Equal(0.02m, tx.SimpleAmount);
		Assert.Equal("USD Coin (USDC)", tx.SimpleAsset?.Name);
		Assert.Equal(TransactionAction.Dividend, tx.Action); // This ReinvInt record adds a security rather than a currency, so we consider it a dividend instead of interest.
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2021, 2, 11), tx.When);
		Assert.Equal(100, tx.SimpleAmount);
		Assert.Equal("USD Coin (USDC)", tx.SimpleAsset?.Name);
		Assert.Equal(TransactionAction.Remove, tx.Action);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);
		Assert.Equal("Xfr To: Brokerage 2", tx.Memo);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2021, 4, 11), tx.When);
		Assert.Equal(1.99m, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Interest, tx.Action);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);

		tx = brokerage.Transactions[transactionCounter++];
		Assert.Equal(new DateTime(2021, 4, 30), tx.When);
		Assert.Equal(1.23m, tx.SimpleAmount);
		Assert.Equal(TransactionAction.Dividend, tx.Action);
		Assert.Equal("PIMCO INCOME FUND CL D", tx.RelatedAsset?.Name);
		Assert.Equal("DIVIDEND RECEIVED", tx.Memo);
		Assert.Equal(ClearedState.Reconciled, tx.Cleared);
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
