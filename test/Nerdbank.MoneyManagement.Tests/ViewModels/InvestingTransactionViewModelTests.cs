// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class InvestingTransactionViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel account;
	private InvestingTransactionViewModel viewModel;
	private DateTime when = DateTime.Now - TimeSpan.FromDays(3);
	private AssetViewModel msft;
	private AssetViewModel appl;

	public InvestingTransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		Account thisAccountModel = this.Money.Insert(new Account { Name = "this", Type = Account.AccountType.Investing, CurrencyAssetId = this.Money.PreferredAssetId });
		this.account = (InvestingAccountViewModel)this.DocumentViewModel.GetAccount(thisAccountModel.Id);
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.viewModel = this.account.Transactions[^1];
		this.msft = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft");
		this.appl = this.DocumentViewModel.AssetsPanel.NewAsset("Apple");
	}

	[Fact]
	public void When()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.When = this.when,
			nameof(this.viewModel.When));
		Assert.Equal(this.when, this.viewModel.When);
	}

	[Fact]
	public void Action()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Action = TransactionAction.Withdraw,
			nameof(this.viewModel.Action));
		Assert.Equal(TransactionAction.Withdraw, this.viewModel.Action);
	}

	[Fact]
	public void Action_SetToBuy()
	{
		InvestingTransactionViewModel buy = this.account.Transactions[^1];
		buy.Action = TransactionAction.Buy;
		Assert.Same(this.account, buy.CreditAccount);
		Assert.Same(this.account, buy.DebitAccount);
		Assert.Same(this.DocumentViewModel.DefaultCurrency, buy.DebitAsset);

		Assert.Null(buy.CreditAmount);
		Assert.Null(buy.DebitAmount);
		Assert.Null(buy.CreditAsset);
	}

	[Fact]
	public void Action_SetToSell()
	{
		InvestingTransactionViewModel sell = this.account.Transactions[^1];
		sell.Action = TransactionAction.Sell;
		Assert.Same(this.account, sell.CreditAccount);
		Assert.Same(this.account, sell.DebitAccount);
		Assert.Same(this.account.CurrencyAsset, sell.CreditAsset);

		Assert.Null(sell.CreditAmount);
		Assert.Null(sell.DebitAmount);
		Assert.Null(sell.DebitAsset);
	}

	[Fact]
	public void Action_SetToExchange()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];
		exchange.Action = TransactionAction.Exchange;
		Assert.Same(this.account, exchange.CreditAccount);
		Assert.Same(this.account, exchange.DebitAccount);

		Assert.Null(exchange.CreditAmount);
		Assert.Null(exchange.DebitAmount);
		Assert.Null(exchange.DebitAsset);
		Assert.Null(exchange.CreditAsset);
	}

	[Fact]
	public void Action_SetToWithdraw()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.CreditAccount = this.account;
		exchange.CreditAmount = 2;
		exchange.CreditAsset = this.appl;

		exchange.Action = TransactionAction.Withdraw;
		Assert.Same(this.account, exchange.DebitAccount);
		Assert.Same(this.DocumentViewModel.DefaultCurrency, exchange.DebitAsset);

		Assert.Null(exchange.CreditAccount);
		Assert.Null(exchange.CreditAmount);
		Assert.Null(exchange.CreditAsset);
		Assert.Null(exchange.DebitAmount);
	}

	[Fact]
	public void Action_SetToRemove()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.CreditAccount = this.account;
		exchange.CreditAmount = 2;
		exchange.CreditAsset = this.appl;

		exchange.Action = TransactionAction.Remove;
		Assert.Same(this.account, exchange.DebitAccount);

		Assert.Null(exchange.CreditAccount);
		Assert.Null(exchange.CreditAmount);
		Assert.Null(exchange.CreditAsset);
		Assert.Null(exchange.DebitAmount);
		Assert.Null(exchange.DebitAsset);
	}

	[Fact]
	public void Action_SetToAdd()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.DebitAccount = this.account;
		exchange.DebitAmount = 2;
		exchange.DebitAsset = this.appl;

		exchange.Action = TransactionAction.Add;
		Assert.Same(this.account, exchange.CreditAccount);

		Assert.Null(exchange.DebitAccount);
		Assert.Null(exchange.DebitAmount);
		Assert.Null(exchange.DebitAsset);
		Assert.Null(exchange.CreditAmount);
		Assert.Null(exchange.CreditAsset);
	}

	[Fact]
	public void Action_SetToDividend()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.DebitAccount = this.account;
		exchange.DebitAmount = 2;
		exchange.DebitAsset = this.appl;

		exchange.Action = TransactionAction.Dividend;
		Assert.Same(this.account, exchange.CreditAccount);

		Assert.Null(exchange.DebitAccount);
		Assert.Null(exchange.DebitAmount);
		Assert.Null(exchange.DebitAsset);
		Assert.Null(exchange.CreditAmount);
		Assert.Null(exchange.CreditAsset);
	}

	[Fact]
	public void Action_SetToInterest()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.DebitAccount = this.account;
		exchange.DebitAmount = 2;
		exchange.DebitAsset = this.appl;

		exchange.Action = TransactionAction.Interest;
		Assert.Same(this.account, exchange.CreditAccount);
		Assert.Same(this.account.CurrencyAsset, exchange.CreditAsset);

		Assert.Null(exchange.DebitAccount);
		Assert.Null(exchange.DebitAmount);
		Assert.Null(exchange.DebitAsset);
		Assert.Null(exchange.CreditAmount);
	}

	[Fact]
	public void Action_SetToDeposit()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.DebitAccount = this.account;
		exchange.DebitAmount = 2;
		exchange.DebitAsset = this.appl;

		exchange.Action = TransactionAction.Deposit;
		Assert.Same(this.account, exchange.CreditAccount);
		Assert.Same(this.account.CurrencyAsset, exchange.CreditAsset);

		Assert.Null(exchange.DebitAccount);
		Assert.Null(exchange.DebitAmount);
		Assert.Null(exchange.DebitAsset);
		Assert.Null(exchange.CreditAmount);
	}

	[Fact]
	public void ExchangeSecurity_WithinAccount()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];
		exchange.Action = TransactionAction.Exchange;

		exchange.DebitAsset = this.msft;
		exchange.DebitAmount = 3;

		exchange.CreditAsset = this.appl;
		exchange.CreditAmount = 2;

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(2, balances[this.appl.Id!.Value]);
		Assert.Equal(-3, balances[this.msft.Id!.Value]);
	}

	[Fact]
	public void BuySecurity()
	{
		InvestingTransactionViewModel buy = this.account.Transactions[^1];
		buy.Action = TransactionAction.Buy;
		buy.CreditAsset = this.msft;
		buy.CreditAmount = 2; // 2 shares
		buy.DebitAmount = 250; // $250 total

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(-250, balances[this.Money.PreferredAssetId]);
		Assert.Equal(2, balances[this.msft.Id!.Value]);

		this.ReloadViewModel();
	}

	[Fact]
	public void SellSecurity()
	{
		InvestingTransactionViewModel sell = this.account.Transactions[^1];
		sell.Action = TransactionAction.Sell;
		sell.DebitAsset = this.msft;
		sell.DebitAmount = 2;
		sell.CreditAmount = 250;

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(250, balances[this.Money.PreferredAssetId]);
		Assert.Equal(-2, balances[this.msft.Id!.Value]);
	}

	[Fact]
	public void Action_UnspecifiedNotAllowed()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];
		exchange.Action = TransactionAction.Unspecified;
		Assert.False(string.IsNullOrEmpty(exchange.Error));
		this.Logger.WriteLine(exchange.Error);
	}

	[Fact]
	public void ApplyTo_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));
	}

	[Fact]
	public void ApplyTo()
	{
		Transaction transaction = new();
		InvestingTransactionViewModel viewModel = new(this.account, null);

		viewModel.When = this.when;
		viewModel.Action = TransactionAction.Sell;
		viewModel.ApplyTo(transaction);

		Assert.Equal(this.when, transaction.When);
		Assert.Equal(TransactionAction.Sell, transaction.Action);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom()
	{
		Transaction transaction = this.viewModel.Model!;
		transaction.When = this.when;
		transaction.Action = TransactionAction.Sell;

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(this.when, this.viewModel.When);
		Assert.Equal(TransactionAction.Sell, this.viewModel.Action);
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.msft = this.DocumentViewModel.AssetsPanel.FindAsset("Microsoft") ?? throw new InvalidOperationException("Unable to find Microsoft asset.");
		this.appl = this.DocumentViewModel.AssetsPanel.FindAsset("Apple") ?? throw new InvalidOperationException("Unable to find Microsoft asset.");
	}
}
