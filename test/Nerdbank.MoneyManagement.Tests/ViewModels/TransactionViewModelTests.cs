// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class TransactionViewModelTests : MoneyTestBase
{
	private readonly CategoryAccountViewModel[] categories;
	private InvestingAccountViewModel account;
	private InvestingAccountViewModel otherAccount;
	private BankingAccountViewModel checking;
	private InvestingTransactionViewModel viewModel;
	private DateTime when = DateTime.Now - TimeSpan.FromDays(3);
	private AssetViewModel msft;
	private AssetViewModel appl;

	public TransactionViewModelTests(ITestOutputHelper logger)
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
