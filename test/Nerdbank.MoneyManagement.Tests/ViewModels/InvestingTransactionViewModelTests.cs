// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class InvestingTransactionViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel account;
	private InvestingAccountViewModel otherAccount;
	private BankingAccountViewModel checking;
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
		this.checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
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
		Assert.Same(this.account, buy.DepositAccount);
		Assert.Same(this.account, buy.WithdrawAccount);
		Assert.Same(this.DocumentViewModel.DefaultCurrency, buy.WithdrawAsset);

		Assert.Null(buy.DepositAmount);
		Assert.Null(buy.WithdrawAmount);
		Assert.Null(buy.DepositAsset);
	}

	[Fact]
	public void Action_SetToSell()
	{
		InvestingTransactionViewModel sell = this.account.Transactions[^1];
		sell.Action = TransactionAction.Sell;
		Assert.Same(this.account, sell.DepositAccount);
		Assert.Same(this.account, sell.WithdrawAccount);
		Assert.Same(this.account.CurrencyAsset, sell.DepositAsset);

		Assert.Null(sell.DepositAmount);
		Assert.Null(sell.WithdrawAmount);
		Assert.Null(sell.WithdrawAsset);
	}

	[Fact]
	public void Action_SetToExchange()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];
		exchange.Action = TransactionAction.Exchange;
		Assert.Same(this.account, exchange.DepositAccount);
		Assert.Same(this.account, exchange.WithdrawAccount);

		Assert.Null(exchange.DepositAmount);
		Assert.Null(exchange.WithdrawAmount);
		Assert.Null(exchange.WithdrawAsset);
		Assert.Null(exchange.DepositAsset);
	}

	[Fact]
	public void Action_SetToWithdraw()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.DepositAccount = this.account;
		exchange.DepositAmount = 2;
		exchange.DepositAsset = this.appl;

		exchange.Action = TransactionAction.Withdraw;
		Assert.Same(this.account, exchange.WithdrawAccount);
		Assert.Same(this.DocumentViewModel.DefaultCurrency, exchange.WithdrawAsset);

		Assert.Null(exchange.DepositAccount);
		Assert.Null(exchange.DepositAmount);
		Assert.Null(exchange.DepositAsset);
		Assert.Null(exchange.WithdrawAmount);
	}

	[Fact]
	public void Action_SetToRemove()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.DepositAccount = this.account;
		exchange.DepositAmount = 2;
		exchange.DepositAsset = this.appl;

		exchange.Action = TransactionAction.Remove;
		Assert.Same(this.account, exchange.WithdrawAccount);

		Assert.Null(exchange.DepositAccount);
		Assert.Null(exchange.DepositAmount);
		Assert.Null(exchange.DepositAsset);
		Assert.Null(exchange.WithdrawAmount);
		Assert.Null(exchange.WithdrawAsset);
	}

	[Fact]
	public void Action_SetToAdd()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.WithdrawAccount = this.account;
		exchange.WithdrawAmount = 2;
		exchange.WithdrawAsset = this.appl;

		exchange.Action = TransactionAction.Add;
		Assert.Same(this.account, exchange.DepositAccount);

		Assert.Null(exchange.WithdrawAccount);
		Assert.Null(exchange.WithdrawAmount);
		Assert.Null(exchange.WithdrawAsset);
		Assert.Null(exchange.DepositAmount);
		Assert.Null(exchange.DepositAsset);
	}

	[Fact]
	public void Action_SetToDividend()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.WithdrawAccount = this.account;
		exchange.WithdrawAmount = 2;
		exchange.WithdrawAsset = this.appl;

		exchange.Action = TransactionAction.Dividend;
		Assert.Same(this.account, exchange.DepositAccount);
		Assert.Same(this.account.CurrencyAsset, exchange.DepositAsset);

		Assert.Null(exchange.WithdrawAccount);
		Assert.Null(exchange.WithdrawAmount);
		Assert.Null(exchange.WithdrawAsset);
		Assert.Null(exchange.DepositAmount);
	}

	[Fact]
	public void Action_SetToInterest()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.WithdrawAccount = this.account;
		exchange.WithdrawAmount = 2;
		exchange.WithdrawAsset = this.appl;

		exchange.Action = TransactionAction.Interest;
		Assert.Same(this.account, exchange.DepositAccount);
		Assert.Same(this.account.CurrencyAsset, exchange.DepositAsset);

		Assert.Null(exchange.WithdrawAccount);
		Assert.Null(exchange.WithdrawAmount);
		Assert.Null(exchange.WithdrawAsset);
		Assert.Null(exchange.DepositAmount);
	}

	[Fact]
	public void Action_SetToDeposit()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];

		// Add some noise that we expect to be cleaned up by setting Action.
		exchange.WithdrawAccount = this.account;
		exchange.WithdrawAmount = 2;
		exchange.WithdrawAsset = this.appl;

		exchange.Action = TransactionAction.Deposit;
		Assert.Same(this.account, exchange.DepositAccount);
		Assert.Same(this.account.CurrencyAsset, exchange.DepositAsset);

		Assert.Null(exchange.WithdrawAccount);
		Assert.Null(exchange.WithdrawAmount);
		Assert.Null(exchange.WithdrawAsset);
		Assert.Null(exchange.DepositAmount);
	}

	[Fact]
	public void ExchangeSecurity_WithinAccount()
	{
		InvestingTransactionViewModel exchange = this.account.Transactions[^1];
		exchange.Action = TransactionAction.Exchange;

		exchange.WithdrawAsset = this.msft;
		exchange.WithdrawAmount = 3;

		exchange.DepositAsset = this.appl;
		exchange.DepositAmount = 2;

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(2, balances[this.appl.Id]);
		Assert.Equal(-3, balances[this.msft.Id]);

		this.AssertNowAndAfterReload(delegate
		{
			exchange = this.account.FindTransaction(exchange.TransactionId)!;
			Assert.Same(this.msft, exchange.WithdrawAsset);
			Assert.Equal(3, exchange.WithdrawAmount);
			Assert.Same(this.account, exchange.DepositAccount);
			Assert.Same(this.appl, exchange.DepositAsset);
			Assert.Equal(2, exchange.DepositAmount);
			Assert.Same(this.account, exchange.WithdrawAccount);
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
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(3, tx.DepositAmount);
			Assert.Same(this.account, tx.DepositAccount);
			Assert.Same(this.account.CurrencyAsset, tx.DepositAsset);
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
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(3, tx.WithdrawAmount);
			Assert.Same(this.account, tx.WithdrawAccount);
			Assert.Same(this.account.CurrencyAsset, tx.WithdrawAsset);
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
		Assert.Equal(2, balances[this.msft.Id]);

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(TransactionAction.Buy, tx.Action);
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Same(this.msft, tx.DepositAsset);
			Assert.Equal(2, tx.DepositAmount);
			Assert.Equal(250, tx.WithdrawAmount);
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
		Assert.Equal(-2, balances[this.msft.Id]);

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Equal(250, tx.DepositAmount);
			Assert.Equal(2, tx.WithdrawAmount);
			Assert.Same(this.msft, tx.WithdrawAsset);
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
			tx = this.account.FindTransaction(tx.TransactionId)!;
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
			tx = this.account.FindTransaction(tx.TransactionId)!;
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
			tx = this.account.FindTransaction(tx.TransactionId)!;
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
		Assert.Empty(exchange.Entries.Where(te => te.Amount != 0));
	}

	[Theory]
	[InlineData(TransactionAction.Buy)]
	[InlineData(TransactionAction.Sell)]
	public void SimplePrice_CannotBeNegative(TransactionAction action)
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = action;
		tx.SimpleAmount = 1;
		tx.SimplePrice = -1;
		Assert.Equal(-1, tx.SimplePrice);
		Assert.False(string.IsNullOrEmpty(tx.Error));
		this.Logger.WriteLine(tx.Error);
	}

	[Fact]
	public void Transfer_BringAssetsIn()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Transfer;
		tx.SimpleAccount = this.otherAccount;
		tx.SimpleAmount = 2;
		tx.SimpleAsset = this.msft;

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(string.Empty, tx.Error);

			Assert.Equal(2, tx.SimpleAmount);
			Assert.Same(this.msft, tx.SimpleAsset);
			Assert.Same(this.otherAccount, tx.SimpleAccount);

			Assert.Equal(2, tx.DepositAmount);
			Assert.Same(this.msft, tx.DepositAsset);
			Assert.Same(this.account, tx.DepositAccount);
			Assert.Equal(2, tx.WithdrawAmount);
			Assert.Same(this.msft, tx.WithdrawAsset);
			Assert.Same(this.otherAccount, tx.WithdrawAccount);
			Assert.Equal($"{this.otherAccount.Name} -> 2 MSFT", tx.Description);
		});
	}

	[Fact]
	public void Transfer_SendAssetsAway()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Transfer;
		tx.SimpleAccount = this.otherAccount;
		tx.SimpleAmount = -2;
		tx.SimpleAsset = this.msft;

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(string.Empty, tx.Error);

			Assert.Equal(-2, tx.SimpleAmount);
			Assert.Same(this.msft, tx.SimpleAsset);
			Assert.Same(this.otherAccount, tx.SimpleAccount);

			Assert.Equal(2, tx.DepositAmount);
			Assert.Same(this.msft, tx.DepositAsset);
			Assert.Same(this.otherAccount, tx.DepositAccount);
			Assert.Equal(2, tx.WithdrawAmount);
			Assert.Same(this.msft, tx.WithdrawAsset);
			Assert.Same(this.account, tx.WithdrawAccount);
			Assert.Equal($"{this.otherAccount.Name} <- 2 MSFT", tx.Description);
		});
	}

	[Fact]
	public void Transfer_CurrencyFrom()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Transfer;
		tx.SimpleAccount = this.checking;
		tx.SimpleAmount = 2;
		tx.SimpleAsset = this.account.CurrencyAsset;

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(string.Empty, tx.Error);

			Assert.Equal(2, tx.SimpleAmount);
			Assert.Same(this.account.CurrencyAsset, tx.SimpleAsset);
			Assert.Same(this.checking, tx.SimpleAccount);

			Assert.Equal(2, tx.DepositAmount);
			Assert.Same(this.account.CurrencyAsset, tx.DepositAsset);
			Assert.Same(this.account, tx.DepositAccount);
			Assert.Equal(2, tx.WithdrawAmount);
			Assert.Same(this.account.CurrencyAsset, tx.WithdrawAsset);
			Assert.Same(this.checking, tx.WithdrawAccount);
			Assert.Equal($"{this.checking.Name} -> $2.00 USD", tx.Description);
		});
	}

	[Fact]
	public void ApplyTo()
	{
		InvestingTransactionViewModel viewModel = new(this.account);

		viewModel.When = this.when;
		viewModel.Memo = "my memo";
		viewModel.Action = TransactionAction.Sell;
		viewModel.RelatedAsset = this.msft;
		viewModel.ApplyToModel();

		Assert.Equal(this.when, viewModel.Transaction.When);
		Assert.Equal(viewModel.Memo, viewModel.Transaction.Memo);
		Assert.Equal(TransactionAction.Sell, viewModel.Transaction.Action);
		Assert.Equal(this.msft.Id, viewModel.Transaction.RelatedAssetId);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom()
	{
		TransactionAndEntry transactionAndEntry = new()
		{
			TransactionId = this.viewModel.TransactionId,
			When = this.when,
			TransactionMemo = "my memo",
			Action = TransactionAction.Sell,
			RelatedAssetId = this.msft.Id,
		};

		this.viewModel.CopyFrom(new[] { transactionAndEntry });

		Assert.Equal(this.when, this.viewModel.When);
		Assert.Equal(transactionAndEntry.TransactionMemo, this.viewModel.Memo);
		Assert.Equal(TransactionAction.Sell, this.viewModel.Action);
		Assert.Same(this.msft, this.viewModel.RelatedAsset);
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.account = (InvestingAccountViewModel)this.DocumentViewModel.AccountsPanel.FindAccount(this.account.Id)!;
		this.otherAccount = (InvestingAccountViewModel)this.DocumentViewModel.AccountsPanel.FindAccount(this.otherAccount.Id)!;
		this.checking = (BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.FindAccount(this.checking.Id)!;
		this.msft = this.DocumentViewModel.AssetsPanel.FindAsset("Microsoft") ?? throw new InvalidOperationException("Unable to find Microsoft asset.");
		this.appl = this.DocumentViewModel.AssetsPanel.FindAsset("Apple") ?? throw new InvalidOperationException("Unable to find Microsoft asset.");
	}
}
