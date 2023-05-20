// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using SQLite;

public class InvestingTransactionViewModelTests : MoneyTestBase
{
	private readonly CategoryAccountViewModel[] categories;
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
		this.categories = Enumerable.Range(1, 3).Select(i => this.DocumentViewModel.CategoriesPanel.NewCategory($"Category {i}")).ToArray();
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.viewModel = this.account.Transactions[^1];
		this.msft = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft", "MSFT");
		this.appl = this.DocumentViewModel.AssetsPanel.NewAsset("Apple", "AAPL");
		this.EnableSqlLogging();
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
		Assert.Equal(0, exchange.DepositAmount);
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
		Assert.Equal(0, exchange.DepositAmount);
	}

	[Theory, PairwiseData]
	public void AutoDetectedAction_Deposit([CombinatorialRange(0, 2)] int assignedCategories)
	{
		TransactionViewModel tx = this.CreateDepositTransaction(this.account, assignedCategories);
		Assert.Equal(TransactionAction.Deposit, tx.AutoDetectedAction);
	}

	[Theory, PairwiseData]
	public void AutoDetectedAction_Withdraw([CombinatorialRange(0, 2)] int assignedCategories)
	{
		TransactionViewModel tx = this.CreateWithdrawTransaction(this.account, assignedCategories);
		Assert.Equal(TransactionAction.Withdraw, tx.AutoDetectedAction);
	}

	[Theory, PairwiseData]
	public void AutoDetectedAction_Transfer(bool security)
	{
		Asset asset = security ? this.msft : this.checking.CurrencyAsset!;
		Transaction tx = new()
		{
			Action = TransactionAction.Transfer,
			When = DateTime.Now,
		};
		this.Money.Insert(tx);
		this.Money.InsertAll(
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = asset.Id,
				Amount = -50,
			},
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.otherAccount.Id,
				AssetId = asset.Id,
				Amount = 50,
			});

		TransactionViewModel? txVm = this.account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		Assert.Equal(TransactionAction.Transfer, txVm.AutoDetectedAction);
	}

	[Fact]
	public void AutoDetectedAction_Buy()
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Buy,
			When = DateTime.Now,
		};
		this.Money.Insert(tx);
		this.Money.InsertAll(
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.account.CurrencyAsset!.Id,
				Amount = -50,
			},
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.msft.Id,
				Amount = 2,
			});

		TransactionViewModel? txVm = this.account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		Assert.Equal(TransactionAction.Buy, txVm.AutoDetectedAction);
	}

	[Fact]
	public void AutoDetectedAction_Sell()
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Sell,
			When = DateTime.Now,
		};
		this.Money.Insert(tx);
		this.Money.InsertAll(
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.account.CurrencyAsset!.Id,
				Amount = 50,
			},
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.msft.Id,
				Amount = -2,
			});

		TransactionViewModel? txVm = this.account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		Assert.Equal(TransactionAction.Sell, txVm.AutoDetectedAction);
	}

	[Fact]
	public void AutoDetectedAction_Exchange()
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Exchange,
			When = DateTime.Now,
		};
		this.Money.Insert(tx);
		this.Money.InsertAll(
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.appl.Id,
				Amount = 5,
			},
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.msft.Id,
				Amount = -2,
			});

		TransactionViewModel? txVm = this.account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		Assert.Equal(TransactionAction.Exchange, txVm.AutoDetectedAction);
	}

	[Fact]
	public void AutoDetectedAction_Dividend()
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Dividend,
			When = DateTime.Now,
			RelatedAssetId = this.msft.Id,
		};
		this.Money.Insert(tx);
		this.Money.InsertAll(
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.account.CurrencyAsset!.Id,
				Amount = 5,
			});

		TransactionViewModel? txVm = this.account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		Assert.Equal(TransactionAction.Dividend, txVm.AutoDetectedAction);
	}

	[Fact]
	public void AutoDetectedAction_Add()
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Add,
			When = DateTime.Now,
			RelatedAssetId = this.msft.Id,
		};
		this.Money.Insert(tx);
		this.Money.InsertAll(
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.msft.Id,
				Amount = 5,
			});

		TransactionViewModel? txVm = this.account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		Assert.Equal(TransactionAction.Add, txVm.AutoDetectedAction);
	}

	[Fact]
	public void AutoDetectedAction_Remove()
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Remove,
			When = DateTime.Now,
			RelatedAssetId = this.msft.Id,
		};
		this.Money.Insert(tx);
		this.Money.InsertAll(
			new TransactionEntry()
			{
				TransactionId = tx.Id,
				AccountId = this.account.Id,
				AssetId = this.msft.Id,
				Amount = -5,
			});

		TransactionViewModel? txVm = this.account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		Assert.Equal(TransactionAction.Remove, txVm.AutoDetectedAction);
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
			Assert.Equal(2, tx.SimpleAmount);
			Assert.Equal(125, tx.SimplePrice);
			Assert.Equal(250, tx.WithdrawAmount);
			Assert.Equal("2 MSFT @ $125.00", tx.Description);
		});
	}

	[Fact]
	public void Buy_WithCommission()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Buy;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2; // 2 shares
		tx.SimplePrice = 125; // $250 total
		tx.Commission = 5;

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(-255, balances[this.Money.PreferredAssetId]);
		Assert.Equal(2, balances[this.msft.Id]);

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(TransactionAction.Buy, tx.Action);
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Same(this.msft, tx.DepositAsset);
			Assert.Equal(2, tx.DepositAmount);
			Assert.Equal(2, tx.SimpleAmount);
			Assert.Equal(125, tx.SimplePrice);
			Assert.Equal(250, tx.WithdrawAmount);
			Assert.Equal(5, tx.Commission);
			Assert.Equal("2 MSFT @ $125.00 (-$5.00)", tx.Description);
		});
	}

	[Fact]
	public void Sell_WithCommission()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Sell;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2; // 2 shares
		tx.SimplePrice = 125; // $250 total
		tx.Commission = 5;

		IReadOnlyDictionary<int, decimal> balances = this.Money.GetBalances(this.account.Model!);
		Assert.Equal(245, balances[this.Money.PreferredAssetId]);
		Assert.Equal(-2, balances[this.msft.Id]);

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(TransactionAction.Sell, tx.Action);
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Same(this.msft, tx.WithdrawAsset);
			Assert.Equal(250, tx.DepositAmount);
			Assert.Equal(2, tx.SimpleAmount);
			Assert.Equal(125, tx.SimplePrice);
			Assert.Equal(2, tx.WithdrawAmount);
			Assert.Equal(5, tx.Commission);
			Assert.Equal("2 MSFT @ $125.00 (-$5.00)", tx.Description);
		});
	}

	[Fact]
	public void Commission_MustBePositive()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Buy;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2; // 2 shares
		tx.SimplePrice = 125; // $250 total

		Assert.True(tx.IsReadyToSave);
		tx.Commission = -5;
		Assert.False(tx.IsReadyToSave);
		this.Logger.WriteLine(tx.Error);
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
	public void Dividend_Cash()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Dividend;
		tx.RelatedAsset = this.msft;
		tx.SimpleAsset = this.account.CurrencyAsset;
		tx.SimpleAmount = 15; // $15 dividend in cash

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(TransactionAction.Dividend, tx.Action);
			Assert.Same(this.account.CurrencyAsset, tx.SimpleAsset);
			Assert.Same(this.msft, tx.RelatedAsset);
			Assert.Equal(15, tx.SimpleAmount);
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Equal("MSFT +$15.00", tx.Description);
		});
	}

	[Fact]
	public void Dividend_Reinvested()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Dividend;
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 0.8m; // shares reinvested from cash value
		tx.CashValue = 50;

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(TransactionAction.Dividend, tx.Action);
			Assert.Same(this.msft, tx.SimpleAsset);
			Assert.Equal(0.8m, tx.SimpleAmount);
			Assert.Equal(50, tx.SimpleCurrencyImpact);
			Assert.Equal(50, tx.CashValue);
			Assert.Equal(tx.CashValue / tx.SimpleAmount, tx.SimplePrice);
			Assert.Equal("MSFT +0.8 ($50.00)", tx.Description);
		});
	}

	[Fact]
	public void Add()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Add;
		tx.When = new DateTime(2022, 1, 2);
		tx.SimpleAsset = this.msft;
		tx.SimpleAmount = 2; // 2 shares
		tx.SimplePrice = 220;

		Assert.Equal(tx.When, tx.AcquisitionDate);
		tx.AcquisitionDate = new DateTime(2021, 9, 3);

		this.AssertNowAndAfterReload(delegate
		{
			tx = this.account.FindTransaction(tx.TransactionId)!;
			Assert.Equal(TransactionAction.Add, tx.Action);
			Assert.Same(this.msft, tx.SimpleAsset);
			Assert.Equal(2, tx.SimpleAmount);
			Assert.Equal(220, tx.SimplePrice);
			Assert.True(tx.IsSimplePriceApplicable);
			Assert.True(tx.IsSimpleAssetApplicable);
			Assert.Equal(0, tx.SimpleCurrencyImpact);
			Assert.Equal($"2 MSFT @ $220.00", tx.Description);

			Assert.Equal(new DateTime(2021, 9, 3), tx.AcquisitionDate);
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
	public void TaxLotSelection()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		Assert.Null(tx.TaxLotSelection);
		TestUtilities.AssertPropertyChangedEvent(
			tx,
			() => tx.Action = TransactionAction.Sell,
			nameof(tx.TaxLotSelection));
		Assert.NotNull(tx.TaxLotSelection);
	}

	[Fact]
	public void Transfer_BringAssetsIn()
	{
		InvestingTransactionViewModel tx = this.account.Transactions[^1];
		tx.Action = TransactionAction.Transfer;
		tx.SimpleAccount = this.otherAccount;
		tx.SimpleAmount = 2;
		tx.SimpleAsset = this.msft;
		Assert.Equal(0, tx.Entries.Sum(e => e.Model.Amount));

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
		Assert.Equal(0, tx.Entries.Sum(e => e.Model.Amount));

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
	public void Transfer_ConsumesAndRecreatesTaxLots()
	{
		InvestingTransactionViewModel tx1 = this.account.Transactions[^1];
		tx1.Action = TransactionAction.Buy;
		tx1.When = new DateTime(2022, 1, 2);
		tx1.SimpleAmount = 3;
		tx1.SimpleAsset = this.msft;
		tx1.SimplePrice = 100;
		TransactionEntryViewModel tx1AddEntry = Assert.Single(tx1.Entries, e => e.Asset == this.msft);
		Assert.Single(tx1AddEntry.CreatedTaxLots!);

		InvestingTransactionViewModel tx2 = this.account.Transactions[^1];
		tx2.Action = TransactionAction.Buy;
		tx2.When = new DateTime(2022, 1, 3);
		tx2.SimpleAmount = 5;
		tx2.SimpleAsset = this.msft;
		tx2.SimplePrice = 110;
		TransactionEntryViewModel tx2AddEntry = Assert.Single(tx2.Entries, e => e.Asset == this.msft);
		Assert.Single(tx2AddEntry.CreatedTaxLots!);

		InvestingTransactionViewModel txMove = this.account.Transactions[^1];
		txMove.Action = TransactionAction.Transfer;
		txMove.When = new DateTime(2022, 1, 4);
		txMove.SimpleAmount = -6; // Enough that all of one and some of another must be consumed.
		txMove.SimpleAsset = this.msft;
		txMove.SimpleAccount = this.otherAccount;

		// One entry is required to consume all the required tax lots.
		TransactionEntryViewModel txMoveFromEntry = Assert.Single(txMove.Entries, e => e.Account == this.account);
		TableQuery<TaxLotAssignment> tla = this.Money.GetTaxLotAssignments(txMoveFromEntry.Id);
		Assert.Equal(2, tla.Count());

		// And one entry is required to create all the new tax lots.
		TransactionEntryViewModel txMoveToEntry = Assert.Single(txMove.Entries, e => e.Account == this.otherAccount);
		Assert.Empty(this.Money.GetTaxLotAssignments(txMoveToEntry.Id));
		Assert.Equal(2, txMoveToEntry.CreatedTaxLots?.Count);

		// Verify that each created tax lot matches the data from the originals.
		TaxLotViewModel taxLotRecreated1 = Assert.Single(txMoveToEntry.CreatedTaxLots!, lot => lot.AcquiredDate == tx1.When);
		Assert.Equal(tx1.SimpleAmount * tx1.SimplePrice, taxLotRecreated1.CostBasisAmount);
		Assert.Same(this.account.CurrencyAsset, taxLotRecreated1.CostBasisAsset);

		TaxLotViewModel taxLotRecreated2 = Assert.Single(txMoveToEntry.CreatedTaxLots!, lot => lot.AcquiredDate == tx2.When);
		Assert.Equal(3 * tx2.SimplePrice, taxLotRecreated2.CostBasisAmount);
		Assert.Same(this.account.CurrencyAsset, taxLotRecreated2.CostBasisAsset);
	}

	[Fact]
	public void Transfers_CreateOrDeleteTransactionViewsInOtherAccounts()
	{
		// Force population of the other accounts we'll be testing so we can test dynamically adding to it when a transfer is created that's related to it.
		_ = this.checking.Transactions;
		_ = this.otherAccount.Transactions;

		InvestingTransactionViewModel tx1 = this.account.Transactions[0];
		tx1.Action = TransactionAction.Transfer;
		tx1.SimpleAmount = 10;
		tx1.SimpleAccount = this.checking;
		Assert.True(tx1.IsReadyToSave);
		Assert.False(tx1.IsDirty);

		Assert.Single(this.checking.Transactions.Where(t => t.IsPersisted));
		Assert.Equal(2, this.checking.Transactions.Count);
		Assert.Equal(-10, this.checking.Transactions[0].Balance);

		tx1.SimpleAmount = -10;
		Assert.Equal(10, this.checking.Transactions[0].Balance);

		tx1.SimpleAccount = this.otherAccount;
		Assert.Empty(this.checking.Transactions.Where(t => t.IsPersisted));
		Assert.Equal(2, this.otherAccount.Transactions.Count);
		Assert.Contains(tx1, this.account.Transactions);
	}

	[Fact]
	public void Transfers_InitiateFromBankingAccount()
	{
		// Force population of the other accounts we'll be testing so we can test dynamically adding to it when a transfer is created that's related to it.
		_ = this.checking.Transactions;
		_ = this.otherAccount.Transactions;

		BankingTransactionViewModel bankingTx = this.checking.Transactions[0];
		bankingTx.Amount = -10;
		bankingTx.OtherAccount = this.account;

		Assert.Single(this.account!.Transactions.Where(t => t.IsPersisted));
		Assert.Equal(2, this.account.Transactions.Count);

		InvestingTransactionViewModel investingTx = this.account.Transactions[0];
		Assert.Equal(TransactionAction.Transfer, investingTx.Action);
		Assert.Equal(10, investingTx.SimpleAmount);
		Assert.Same(this.checking, investingTx.SimpleAccount);
		investingTx.SimpleAccount = this.otherAccount;

		Assert.Empty(this.checking.Transactions.Where(t => t.IsPersisted));
		Assert.Single(this.checking.Transactions);
		Assert.Equal(2, this.otherAccount.Transactions.Count);
	}

	[Fact]
	public async Task DeleteTransaction_RemovesRowFromViewModel()
	{
		// Particularly the Transfer type may need two deletes to work.
		InvestingTransactionViewModel tx1 = this.account.Transactions[0];
		tx1.Action = TransactionAction.Transfer;
		tx1.SimpleAmount = 10;
		tx1.SimpleAccount = this.checking;

		this.DocumentViewModel.SelectedTransaction = tx1;
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Assert.False(Assert.Single(this.account.Transactions).IsPersisted);
	}

	[Fact]
	public void ApplyTo_Dividend()
	{
		InvestingTransactionViewModel viewModel = new(this.account);

		viewModel.When = this.when;
		viewModel.Memo = "my memo";
		viewModel.Action = TransactionAction.Dividend;
		viewModel.RelatedAsset = this.msft;
		viewModel.Cleared = ClearedState.Cleared;
		viewModel.SimpleAmount = 3;
		viewModel.ApplyToModel();

		Assert.Equal(this.when, viewModel.Transaction.When);
		Assert.Equal(viewModel.Memo, viewModel.Transaction.Memo);
		Assert.Equal(TransactionAction.Dividend, viewModel.Transaction.Action);
		Assert.Equal(this.msft.Id, viewModel.Transaction.RelatedAssetId);
		Assert.NotEmpty(viewModel.Entries);
		Assert.All(viewModel.Entries, e => Assert.Equal(ClearedState.Cleared, e.Model.Cleared));
	}

	[Fact]
	public void ApplyTo_Sell()
	{
		InvestingTransactionViewModel viewModel = new(this.account);

		viewModel.When = this.when;
		viewModel.Memo = "my memo";
		viewModel.Action = TransactionAction.Sell;
		viewModel.Cleared = ClearedState.Cleared;
		viewModel.SimpleAmount = 3;
		viewModel.SimpleAsset = this.msft;
		viewModel.SimplePrice = 100m;
		viewModel.ApplyToModel();

		Assert.Equal(this.when, viewModel.Transaction.When);
		Assert.Equal(viewModel.Memo, viewModel.Transaction.Memo);
		Assert.Equal(TransactionAction.Sell, viewModel.Transaction.Action);
		Assert.Null(viewModel.Transaction.RelatedAssetId);

		TransactionEntryViewModel cashEntry = Assert.Single(viewModel.Entries, e => e.Asset == this.account.CurrencyAsset);
		TransactionEntryViewModel stockEntry = Assert.Single(viewModel.Entries, e => e.Asset == this.msft);

		Assert.All(viewModel.Entries, e => Assert.Equal(ClearedState.Cleared, e.Model.Cleared));
		Assert.Equal(-3, stockEntry.Amount);
		Assert.Equal(300, cashEntry.Amount);
	}

	[Fact]
	public void ApplyTo_Transfer()
	{
		InvestingTransactionViewModel viewModel = new(this.account);

		viewModel.When = this.when;
		viewModel.Memo = "my memo";
		viewModel.Action = TransactionAction.Transfer;
		viewModel.Cleared = ClearedState.Cleared;
		viewModel.SimpleAmount = 3;
		viewModel.SimpleAsset = this.msft;
		viewModel.SimpleAccount = this.otherAccount;
		viewModel.ApplyToModel();

		Assert.Equal(this.when, viewModel.Transaction.When);
		Assert.Equal(viewModel.Memo, viewModel.Transaction.Memo);
		Assert.Equal(TransactionAction.Transfer, viewModel.Transaction.Action);
		Assert.Null(viewModel.Transaction.RelatedAssetId);

		TransactionEntryViewModel localEntry = Assert.Single(viewModel.Entries, e => e.Account == this.account);
		TransactionEntryViewModel otherEntry = Assert.Single(viewModel.Entries, e => e.Account == this.otherAccount);

		Assert.Equal(ClearedState.Cleared, localEntry.Cleared);
		Assert.Equal(ClearedState.None, otherEntry.Cleared);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom_Sell()
	{
		TransactionAndEntry[] transactionAndEntries = new TransactionAndEntry[]
		{
			new TransactionAndEntry
			{
				TransactionId = this.viewModel.TransactionId,
				When = this.when,
				TransactionMemo = "my memo",
				Action = TransactionAction.Sell,
				AssetId = this.msft.Id,
				Amount = -2,
				AccountId = this.account.Id,
				Cleared = ClearedState.Cleared,
			},
			new TransactionAndEntry
			{
				TransactionId = this.viewModel.TransactionId,
				When = this.when,
				TransactionMemo = "my memo",
				Action = TransactionAction.Sell,
				AssetId = this.account.CurrencyAsset!.Id,
				Amount = 250,
				AccountId = this.account.Id,
				Cleared = ClearedState.Cleared,
			},
		};

		this.viewModel.CopyFrom(transactionAndEntries);

		Assert.Equal(this.when, this.viewModel.When);
		Assert.Equal(transactionAndEntries[0].TransactionMemo, this.viewModel.Memo);
		Assert.Equal(TransactionAction.Sell, this.viewModel.Action);
		Assert.Equal(ClearedState.Cleared, this.viewModel.Cleared);
		Assert.Same(this.msft, this.viewModel.SimpleAsset);
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

	private TransactionViewModel CreateDepositTransaction(AccountViewModel account, int assignedCategories)
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Deposit,
			When = DateTime.Now,
		};
		this.Money.Insert(tx);
		TransactionEntry te1 = new()
		{
			TransactionId = tx.Id,
			AccountId = account.Id,
			AssetId = account.CurrencyAsset!.Id,
			Amount = 2 + assignedCategories,
		};
		this.Money.Insert(te1);

		for (int i = 0; i < assignedCategories; i++)
		{
			TransactionEntry teCategory = new()
			{
				TransactionId = tx.Id,
				AccountId = this.categories[i].Id,
				AssetId = account.CurrencyAsset!.Id,
				Amount = -1,
			};
			this.Money.Insert(teCategory);
		}

		TransactionViewModel? txVm = account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		return txVm;
	}

	private TransactionViewModel CreateWithdrawTransaction(AccountViewModel account, int assignedCategories)
	{
		Transaction tx = new()
		{
			Action = TransactionAction.Withdraw,
			When = DateTime.Now,
		};
		this.Money.Insert(tx);
		TransactionEntry te1 = new()
		{
			TransactionId = tx.Id,
			AccountId = account.Id,
			AssetId = account.CurrencyAsset!.Id,
			Amount = -2 - assignedCategories,
		};
		this.Money.Insert(te1);

		for (int i = 0; i < assignedCategories; i++)
		{
			TransactionEntry teCategory = new()
			{
				TransactionId = tx.Id,
				AccountId = this.categories[i].Id,
				AssetId = account.CurrencyAsset!.Id,
				Amount = 1,
			};
			this.Money.Insert(teCategory);
		}

		TransactionViewModel? txVm = account.FindTransaction(tx.Id);
		Assert.NotNull(txVm);
		return txVm;
	}
}
