// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class InvestingTransactionViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel account;
	private InvestingAccountViewModel otherAccount;
	private InvestingTransactionViewModel viewModel;
	private DateTime when = DateTime.Now - TimeSpan.FromDays(3);
	private AssetViewModel msft;
	private AssetViewModel appl;

	public InvestingTransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		Account thisAccountModel = this.Money.Insert(new Account { Name = "this", Type = Account.AccountType.Investing, CurrencyAssetId = this.Money.PreferredAssetId });
		Account otherAccountModel = this.Money.Insert(new Account { Name = "other", Type = Account.AccountType.Investing, CurrencyAssetId = this.Money.PreferredAssetId });
		this.account = (InvestingAccountViewModel)this.DocumentViewModel.GetAccount(thisAccountModel.Id);
		this.otherAccount = (InvestingAccountViewModel)this.DocumentViewModel.GetAccount(otherAccountModel.Id);
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.viewModel = this.account.Transactions[^1];
		this.msft = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft", "MSFT");
		this.appl = this.DocumentViewModel.AssetsPanel.NewAsset("Apple", "AAPL");
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
		Assert.Same(this.account.CurrencyAsset, exchange.CreditAsset);

		Assert.Null(exchange.DebitAccount);
		Assert.Null(exchange.DebitAmount);
		Assert.Null(exchange.DebitAsset);
		Assert.Null(exchange.CreditAmount);
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

		this.AssertNowAndAfterReload(delegate
		{
			exchange = this.account.FindTransaction(exchange.Id!.Value)!;
			Assert.Same(this.msft, exchange.DebitAsset);
			Assert.Equal(3, exchange.DebitAmount);
			Assert.Same(this.account, exchange.CreditAccount);
			Assert.Same(this.appl, exchange.CreditAsset);
			Assert.Equal(2, exchange.CreditAmount);
			Assert.Same(this.account, exchange.DebitAccount);
			Assert.Equal("3 MSFT -> 2 AAPL", exchange.Description);
		});
	}

	[Fact]
	public void Deposit()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Deposit;
		tx.SimpleAmount = 3;

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.Equal(3, tx.CreditAmount);
			Assert.Same(this.account, tx.CreditAccount);
			Assert.Same(this.account.CurrencyAsset, tx.CreditAsset);
			Assert.Equal("$3.00", tx.Description);
			Assert.False(tx.IsSimpleAssetApplicable);
			Assert.False(tx.IsSimplePriceApplicable);
		});
	}

	[Fact]
	public void Withdraw()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Withdraw;
		tx.SimpleAmount = 3;

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.Equal(3, tx.DebitAmount);
			Assert.Same(this.account, tx.DebitAccount);
			Assert.Same(this.account.CurrencyAsset, tx.DebitAsset);
			Assert.Equal("$3.00", tx.Description);
			Assert.False(tx.IsSimpleAssetApplicable);
			Assert.False(tx.IsSimplePriceApplicable);
		});
	}

	[Fact]
	public void Buy()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Buy;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2; // 2 shares
		tx.SimplePrice = 125; // $250 total

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(-250, balances[this.Money.PreferredAssetId]);
		Assert.Equal(2, balances[this.msft.Id!.Value]);

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.Equal(TransactionAction.Buy, tx.Action);
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Same(this.msft, tx.CreditAsset);
			Assert.Equal(2, tx.CreditAmount);
			Assert.Equal(250, tx.DebitAmount);
			Assert.Equal("2 MSFT @ $125.00", tx.Description);
		});
	}

	[Fact]
	public void Sell()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Sell;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2;
		tx.SimplePrice = 125;

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(250, balances[this.Money.PreferredAssetId]);
		Assert.Equal(-2, balances[this.msft.Id!.Value]);

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Equal(250, tx.CreditAmount);
			Assert.Equal(2, tx.DebitAmount);
			Assert.Same(this.msft, tx.DebitAsset);
			Assert.Equal("2 MSFT @ $125.00", tx.Description);
		});
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
	public void Dividend()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Dividend;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 15; // $15 dividend in cash

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.Equal(TransactionAction.Dividend, tx.Action);
			Assert.Same(this.msft, tx.SimpleAsset);
			Assert.Equal(15, tx.SimpleAmount);
			Assert.False(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Throws<InvalidOperationException>(() => tx.SimplePrice = 10);
			Assert.Equal(15, tx.SimpleCurrencyImpact);
			Assert.Equal("MSFT +$15.00", tx.Description);
		});
	}

	[Fact]
	public void Add()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Add;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2; // 2 shares

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.Equal(TransactionAction.Add, tx.Action);
			Assert.Same(this.msft, tx.SimpleAsset);
			Assert.Equal(2, tx.SimpleAmount);
			Assert.False(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Throws<InvalidOperationException>(() => tx.SimplePrice = 10);
			Assert.Equal(0, tx.SimpleCurrencyImpact);
			Assert.Equal($"2 MSFT", tx.Description); // add " @ $220.00 USD" when we track tax lots
		});
	}

	[Fact]
	public void Remove()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Remove;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2; // 2 shares

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.Equal(TransactionAction.Remove, tx.Action);
			Assert.Same(this.msft, tx.SimpleAsset);
			Assert.Equal(2, tx.SimpleAmount);
			Assert.False(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Throws<InvalidOperationException>(() => tx.SimplePrice = 10);
			Assert.Equal(0, tx.SimpleCurrencyImpact);
			Assert.Equal($"2 MSFT", tx.Description); // add " @ $220.00 USD" when we track tax lots
		});
	}

	[Fact]
	public void Interest()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];
		exchange.Action = TransactionAction.Interest;
		exchange.SimpleAmount = 2; // $2

		this.AssertNowAndAfterReload(delegate
		{
			exchange = this.account.Transactions[0];
			Assert.Equal(TransactionAction.Interest, exchange.Action);
			Assert.Equal(2, exchange.SimpleAmount);
			Assert.False(exchange.IsSimplePriceApplicable);
			Assert.False(exchange.IsSimpleAssetApplicable);
			Assert.Throws<InvalidOperationException>(() => exchange.SimplePrice = 10);
			Assert.Equal(2, exchange.SimpleCurrencyImpact);
			Assert.Equal($"+$2.00", exchange.Description); // add " @ $220.00 USD" when we track tax lots
		});
	}

	[Theory]
	[InlineData(TransactionAction.Interest)]
	[InlineData(TransactionAction.Dividend)]
	[InlineData(TransactionAction.Buy)]
	[InlineData(TransactionAction.Sell)]
	[InlineData(TransactionAction.Add)]
	[InlineData(TransactionAction.Remove)]
	[InlineData(TransactionAction.Deposit)]
	[InlineData(TransactionAction.Withdraw)]
	public void SimpleAmount_CannotBeNegative(TransactionAction action)
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];
		exchange.Action = action;
		exchange.SimpleAmount = -1;
		Assert.Equal(-1, exchange.SimpleAmount);
		Assert.False(string.IsNullOrEmpty(exchange.Error));
		this.Logger.WriteLine(exchange.Error);
		Assert.Null(exchange.Model?.CreditAmount);
		Assert.Null(exchange.Model?.DebitAmount);
	}

	[Fact]
	public void Transfer()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Transfer;
		tx.CreditAmount = 2;
		tx.CreditAsset = this.msft;
		tx.CreditAccount = this.account;
		tx.DebitAmount = 2.1m;
		tx.DebitAsset = this.msft;
		tx.DebitAccount = this.otherAccount;

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.Id!.Value)!;
			Assert.Equal(2, tx.CreditAmount);
			Assert.Same(this.msft, tx.CreditAsset);
			Assert.Same(this.account, tx.CreditAccount);
			Assert.Equal(2.1m, tx.DebitAmount);
			Assert.Same(this.msft, tx.DebitAsset);
			Assert.Same(this.otherAccount, tx.DebitAccount);
			Assert.Equal("+2 MSFT", tx.Description);
		});
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
		viewModel.Memo = "my memo";
		viewModel.Action = TransactionAction.Sell;
		viewModel.RelatedAsset = this.msft;
		viewModel.ApplyTo(transaction);

		Assert.Equal(this.when, transaction.When);
		Assert.Equal(viewModel.Memo, transaction.Memo);
		Assert.Equal(TransactionAction.Sell, transaction.Action);
		Assert.Equal(this.msft.Id, transaction.RelatedAssetId);
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
		transaction.Memo = "my memo";
		transaction.Action = TransactionAction.Sell;
		transaction.RelatedAssetId = this.msft.Id;

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(this.when, this.viewModel.When);
		Assert.Equal(transaction.Memo, this.viewModel.Memo);
		Assert.Equal(TransactionAction.Sell, this.viewModel.Action);
		Assert.Same(this.msft, this.viewModel.RelatedAsset);
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.account = (InvestingAccountViewModel)this.DocumentViewModel.AccountsPanel.FindAccount(this.account.Id!.Value)!;
		this.otherAccount = (InvestingAccountViewModel)this.DocumentViewModel.AccountsPanel.FindAccount(this.otherAccount.Id!.Value)!;
		this.msft = this.DocumentViewModel.AssetsPanel.FindAsset("Microsoft") ?? throw new InvalidOperationException("Unable to find Microsoft asset.");
		this.appl = this.DocumentViewModel.AssetsPanel.FindAsset("Apple") ?? throw new InvalidOperationException("Unable to find Microsoft asset.");
	}
}
