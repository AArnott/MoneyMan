// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class BankingTransactionViewModelTests : MoneyTestBase
{
	private BankingAccountViewModel account;
	private BankingAccountViewModel otherAccount;
	private CategoryAccountViewModel category1;
	private CategoryAccountViewModel category2;

	private BankingTransactionViewModel viewModel;

	private string payee = "some person";

	private decimal amount = 5.5m;

	private string memo = "Some memo";

	private DateTime when = DateTime.Now - TimeSpan.FromDays(3);

	private int? checkNumber = 15;

	private ClearedState cleared = ClearedState.None;

	public BankingTransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		Account thisAccountModel = this.Money.Insert(new Account { Name = "this", CurrencyAssetId = this.Money.PreferredAssetId });
		Account otherAccountModel = this.Money.Insert(new Account { Name = "other", CurrencyAssetId = this.Money.PreferredAssetId });
		Account category1AccountModel = this.Money.Insert(new Account { Name = "Spending", Type = Account.AccountType.Category, });
		Account category2AccountModel = this.Money.Insert(new Account { Name = "Entertainment", Type = Account.AccountType.Category, });

		this.account = (BankingAccountViewModel)this.DocumentViewModel.GetAccount(thisAccountModel.Id);
		this.otherAccount = (BankingAccountViewModel)this.DocumentViewModel.GetAccount(otherAccountModel.Id);
		this.category1 = this.DocumentViewModel.GetCategory(category1AccountModel.Id);
		this.category2 = this.DocumentViewModel.GetCategory(category2AccountModel.Id);
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.viewModel = this.account.Transactions[^1];
		this.EnableSqlLogging();
	}

	[Fact]
	public void When_LinkedAcrossSplits()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.When = this.when,
			nameof(this.viewModel.When));
		Assert.Equal(this.when, this.viewModel.When);

		BankingTransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.Throws<InvalidOperationException>(() => foreignSplitTransaction.When = DateTime.Now);

		// When linked across split transfers
		Assert.Equal(this.viewModel.When, foreignSplitTransaction.When);
		this.viewModel.When = DateTime.Now;
		Assert.Equal(this.viewModel.When, foreignSplitTransaction.When);
	}

	[Fact]
	public void CheckNumber()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.CheckNumber = this.checkNumber,
			nameof(this.viewModel.CheckNumber));
		Assert.Equal(this.checkNumber, this.viewModel.CheckNumber);
	}

	[Fact]
	public void Amount()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Amount = this.amount,
			nameof(this.viewModel.Amount));
		Assert.Equal(this.amount, this.viewModel.Amount);
	}

	[Fact]
	public void Amount_OnSplitTransactions_ViewModelOnly()
	{
		this.viewModel.Amount = -50;
		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		Assert.Equal(-50, this.viewModel.Amount);
		Assert.Equal(-50, split1.Amount);

		split1.Account = this.category1;
		split1.Amount = -40;
		Assert.Equal(-40, this.viewModel.Amount);
		Assert.Equal(-40, split1.Amount);

		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		Assert.Equal(-40, this.viewModel.Amount);
		Assert.Equal(-40, split1.Amount);
		split2.Account = this.category2;
		split2.Amount = -30;
		Assert.Equal(-70, this.viewModel.Amount);

		this.ReloadViewModel();
		Assert.Equal(-70, this.viewModel.Amount);

		split1 = this.viewModel.Splits[0];
		split2 = this.viewModel.Splits[1];

		split2.Amount += 10;
		Assert.Equal(split1.Amount + split2.Amount, this.viewModel.Amount);
	}

	[Fact]
	public async Task Balance_OnSplitTransactions()
	{
		this.viewModel.Amount = -50;
		this.viewModel.OtherAccount = this.category1;
		Assert.Equal(-50, this.viewModel.Balance);
		await this.viewModel.SplitCommand.ExecuteAsync();
		TransactionEntryViewModel split1 = this.viewModel.Splits[0];
		Assert.Equal(-50, this.viewModel.Balance);

		split1.Amount = -40;
		Assert.Equal(-40, this.viewModel.Balance);

		TransactionEntryViewModel split2 = this.viewModel.Splits[^1];
		split2.Amount = -30;
		split2.Account = this.category2;
		Assert.Equal(-70, this.viewModel.Balance);

		this.ReloadViewModel();
		Assert.Equal(-70, this.viewModel.Balance);
	}

	[Fact]
	public void Memo()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Memo = this.memo,
			nameof(this.viewModel.Memo));
		Assert.Equal(this.memo, this.viewModel.Memo);
	}

	[Fact]
	public void Cleared()
	{
		this.viewModel.Cleared = ClearedState.Cleared;
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Cleared = this.cleared,
			nameof(this.viewModel.Cleared));
		Assert.Equal(this.cleared, this.viewModel.Cleared);
	}

	[Fact]
	public void Payee_LinkedAcrossSplits()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Payee = "somebody",
			nameof(this.viewModel.Payee));
		Assert.Same("somebody", this.viewModel.Payee);

		BankingTransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.Throws<InvalidOperationException>(() => foreignSplitTransaction.Payee = "me");

		// Payee linked across split transfers
		Assert.Equal(this.viewModel.Payee, foreignSplitTransaction.Payee);
		this.viewModel.Payee = "somebody else";
		Assert.Equal(this.viewModel.Payee, foreignSplitTransaction.Payee);
	}

	[Fact]
	public void Splits_Empty()
	{
		Assert.Empty(this.viewModel.Splits);
	}

	[Fact]
	public void NewSplit_EmptyTransaction()
	{
		TransactionEntryViewModel split = this.viewModel.NewSplit();
		Assert.Same(this.viewModel, split.Transaction);
		Assert.Same(split, this.viewModel.Splits[0]);
		Assert.Equal(2, this.viewModel.Splits.Count);
		Assert.Equal(string.Empty, split.Error);
		split.Amount = 5;
		Assert.True(this.viewModel.Splits[0].IsPersisted);
		Assert.False(this.viewModel.Splits[1].IsPersisted);
	}

	[Fact]
	public void NewSplit_MovesCategory()
	{
		CategoryAccountViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");
		this.viewModel.OtherAccount = categoryViewModel;
		this.viewModel.Amount = -10;
		TransactionEntryViewModel split = this.viewModel.NewSplit();
		split.Amount = -6;
		split = this.viewModel.NewSplit();
		split.Amount = -4;

		this.AssertNowAndAfterReload(delegate
		{
			Assert.Same(this.DocumentViewModel.SplitCategory, this.viewModel.OtherAccount);
			split = this.viewModel.Splits[0];
			Assert.Equal(categoryViewModel.Id, split.Account?.Id);
		});
	}

	[Fact]
	public void NewSplit_CollapsesOnReloadWhenOnlyOne()
	{
		CategoryAccountViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");
		this.viewModel.OtherAccount = categoryViewModel;
		this.viewModel.Amount = 5;

		TransactionEntryViewModel split = this.viewModel.NewSplit();
		Assert.True(this.viewModel.ContainsSplits);
		Assert.Same(this.DocumentViewModel.SplitCategory, this.viewModel.OtherAccount);
		this.ReloadViewModel();
		Assert.Equal(categoryViewModel.Id, this.viewModel.OtherAccount?.Id);
	}

	[Fact]
	public void MultipleNewSplits()
	{
		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		Assert.Same(this.viewModel.ThisAccount, split1.Account);
		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		Assert.Same(this.viewModel.ThisAccount, split2.Account);
	}

	[Fact]
	public void DeleteSplit()
	{
		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 10;
		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 20;
		Assert.Equal(3, this.viewModel.Splits.Count);
		this.viewModel.DeleteSplit(split1);
		Assert.Equal(2, this.viewModel.Splits.Count);
		this.viewModel.DeleteSplit(split2);
		Assert.Empty(this.viewModel.Splits); // jump to zero since the only remaining split was provisionary
	}

	[Fact]
	public void DeleteSplitCommand()
	{
		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 5;
		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 8;
		Assert.Equal(3, this.viewModel.Splits.Count);

		this.viewModel.SelectedSplit = split2;
		Assert.Equal(3, this.viewModel.Splits.Count);

		this.viewModel.DeleteSplitCommand.Execute(null);
		Assert.Equal(2, this.viewModel.Splits.Count);
		Assert.Same(this.viewModel.Splits[1], this.viewModel.SelectedSplit);
	}

	[Fact]
	public void DeleteSplit_LastSplitMergesIntoParent()
	{
		CategoryAccountViewModel cat1 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat1");
		CategoryAccountViewModel cat2 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat2");

		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 10;
		split1.Account = cat1;
		split1.Memo = "memo1";
		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 5;
		split2.Account = cat2;
		split2.Memo = "memo2";

		this.viewModel.DeleteSplit(split1);
		Assert.Equal(5, this.viewModel.Amount);
		Assert.Same(this.DocumentViewModel.SplitCategory, this.viewModel.OtherAccount);
		Assert.Null(this.viewModel.Memo);

		this.viewModel.DeleteSplit(split2);
		Assert.Equal(5, this.viewModel.Amount);
		Assert.Same(cat2, this.viewModel.OtherAccount);
		Assert.Equal("memo2", this.viewModel.Memo);
	}

	[Fact]
	public async Task SplitCommand_OneSplit_ThenDelete()
	{
		await TestUtilities.AssertPropertyChangedEventAsync(this.viewModel, () => this.viewModel.SplitCommand.ExecuteAsync(), nameof(this.viewModel.ContainsSplits));
		Assert.True(this.viewModel.ContainsSplits);

		TransactionEntryViewModel split = this.viewModel.Splits[0];
		split.Amount = 10;
		split.Account = this.category1;

		await TestUtilities.AssertPropertyChangedEventAsync(this.viewModel, () => this.viewModel.SplitCommand.ExecuteAsync(), nameof(this.viewModel.ContainsSplits));

		// We expect no prompts because one split can collapse to a simple transaction with little or no data loss.
		Assert.Equal(0, this.UserNotification.ConfirmCounter);

		this.AssertNowAndAfterReload(delegate
		{
			Assert.False(this.viewModel.ContainsSplits);
			Assert.Equal(10, this.viewModel.Amount);
			Assert.Equal(this.category1.Name, this.viewModel.OtherAccount?.Name);
		});

		// Confirm the split was deleted from the database.
		Assert.Empty(this.Money.TransactionEntries.Where(entry => entry.Id == split.Id));
	}

	[Theory, PairwiseData]
	public async Task SplitCommand_DeleteTwoSplits_IncludesPrompt(bool confirmed)
	{
		this.UserNotification.ChosenAction = confirmed ? IUserNotification.UserAction.Yes : IUserNotification.UserAction.No;

		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 10;
		split1.Account = this.category1;
		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 5;
		split2.Account = this.category2;

		await this.viewModel.SplitCommand.ExecuteAsync();
		Assert.Equal(1, this.UserNotification.ConfirmCounter);

		this.AssertNowAndAfterReload(delegate
		{
			if (confirmed)
			{
				Assert.False(this.viewModel.ContainsSplits);
				Assert.Equal(15, this.viewModel.Amount);
				Assert.Null(this.viewModel.OtherAccount);
			}
			else
			{
				Assert.Equal(3, this.viewModel.Splits.Count);
			}
		});
	}

	[Fact]
	public async Task SplitCommand_TransfersCategoryAndAmountToFirstSplit()
	{
		this.viewModel.OtherAccount = this.category1;
		this.viewModel.Amount = 10;
		this.viewModel.Memo = "memo";
		await this.viewModel.SplitCommand.ExecuteAsync();
		Assert.Equal("memo", this.viewModel.Memo);
		Assert.Equal(10, this.viewModel.Amount);
		Assert.Same(this.DocumentViewModel.SplitCategory, this.viewModel.OtherAccount);

		Assert.Equal(2, this.viewModel.Splits.Count);
		Assert.Null(this.viewModel.Splits[1].Account);

		TransactionEntryViewModel split = this.viewModel.Splits[0];
		Assert.Null(split.Memo);
		Assert.Equal(10, split.Amount);
		Assert.Same(this.category1, split.Account);
	}

	[Fact]
	public void ChangingVolatileTransactionProducesNewOne()
	{
		TransactionEntryViewModel tx1 = this.viewModel.NewSplit();
		tx1.Account = this.category1;
		tx1.Amount = 3;
		TransactionEntryViewModel volatileTx = this.viewModel.Splits[1];
		Assert.True(tx1.IsPersisted);
		Assert.False(volatileTx.IsPersisted);
		volatileTx.Amount = 50;
		volatileTx.Account = this.category2;
		Assert.True(volatileTx.IsPersisted);
		Assert.Equal(3, this.viewModel.Splits.Count);
		TransactionEntryViewModel volatileTx2 = this.viewModel.Splits[2];
		Assert.False(volatileTx2.IsPersisted);
	}

	[Fact]
	public void OtherAccount_ThrowsWhenSplit()
	{
		CategoryAccountViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");
		TransactionEntryViewModel split = this.viewModel.NewSplit();

		// Setting a category should throw when a transaction is split.
		Assert.Throws<InvalidOperationException>(() => this.viewModel.OtherAccount = categoryViewModel);
		Assert.Throws<InvalidOperationException>(() => this.viewModel.OtherAccount = null);

		// Setting to the singleton split value should be allowed.
		this.viewModel.OtherAccount = this.DocumentViewModel.SplitCategory;

		this.viewModel.DeleteSplit(split);
		this.viewModel.OtherAccount = categoryViewModel;
		Assert.Same(categoryViewModel, this.viewModel.OtherAccount);
	}

	[Fact]
	public void AvailableTransactionTargets()
	{
		Assert.DoesNotContain(this.viewModel.AvailableTransactionTargets, tt => tt == this.DocumentViewModel.SplitCategory);
		Assert.DoesNotContain(this.viewModel.AvailableTransactionTargets, tt => tt == this.viewModel.ThisAccount);
		Assert.NotEmpty(this.viewModel.AvailableTransactionTargets);

		// There should be a way to clear the selected category.
		Assert.Contains(null, this.viewModel.AvailableTransactionTargets);
	}

	[Fact]
	public void ContainsSplits()
	{
		Assert.False(this.viewModel.ContainsSplits);

		// Transition to split state.
		TestUtilities.AssertPropertyChangedEvent(this.viewModel, () => this.viewModel.NewSplit(), nameof(this.viewModel.ContainsSplits));
		Assert.True(this.viewModel.ContainsSplits);

		// Transition back to non-split.
		TestUtilities.AssertPropertyChangedEvent(this.viewModel, () => this.viewModel.DeleteSplit(this.viewModel.Splits[0]), nameof(this.viewModel.ContainsSplits));
		Assert.False(this.viewModel.ContainsSplits);
	}

	[Fact]
	public void Splits_Reload()
	{
		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		split1.Account = this.category1;
		split1.Amount = -1;
		split1.Memo = "memo 1";
		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		split2.Account = this.category2;
		split2.Amount = -2;
		split2.Memo = "memo 2";

		this.ReloadViewModel();

		split1 = this.viewModel.Splits[0];
		split2 = this.viewModel.Splits[1];
		Assert.Equal(-1, split1.Amount);
		Assert.Equal(-2, split2.Amount);
		Assert.Equal("memo 1", split1.Memo);
		Assert.Equal("memo 2", split2.Memo);
		Assert.Same(this.category1, split1.Account);
		Assert.Same(this.category2, split2.Account);
	}

	[Fact]
	public void Balance_JustOneTransaction()
	{
		// Verify that one lone transaction's balance is based on its own amount.
		BankingTransactionViewModel tx = this.account.NewTransaction();
		tx.When = new DateTime(2021, 1, 2);
		Assert.Equal(0m, tx.Balance);
		tx.Amount = 5m;
		Assert.Equal(tx.Amount, tx.Balance);
	}

	[Fact]
	public void Balance_SecondTransactionBuildsOnFirst()
	{
		BankingTransactionViewModel tx1 = this.account.NewTransaction();
		tx1.When = new DateTime(2021, 1, 2);
		tx1.Amount = 5m;

		BankingTransactionViewModel tx2 = this.account.NewTransaction();
		tx2.When = new DateTime(2021, 1, 3);
		Assert.Equal(tx1.Balance + tx2.Amount, tx2.Balance);
		tx2.Amount = 8m;
		Assert.Equal(tx1.Balance + tx2.Amount, tx2.Balance);
	}

	[Fact]
	public void Balance_UpdatesInResponseToTransactionInsertedAbove()
	{
		BankingTransactionViewModel tx2 = this.account.NewTransaction();
		tx2.When = new DateTime(2021, 1, 2);
		tx2.Amount = 5m;

		BankingTransactionViewModel tx1 = this.account.NewTransaction();
		tx1.When = new DateTime(2021, 1, 1);
		TestUtilities.AssertPropertyChangedEvent(tx2, () => tx1.Amount = 4m, nameof(tx2.Balance));

		Assert.Equal(tx1.Balance, tx1.Balance);
		Assert.Equal(tx1.Balance + tx2.Amount, tx2.Balance);

		// Reorder the transactions and observe their balance shifting.
		tx1.When = tx2.When + TimeSpan.FromDays(1);

		Assert.Equal(tx2.Amount, tx2.Balance);
		Assert.Equal(tx2.Balance + tx1.Amount, tx1.Balance);
	}

	[Fact]
	public void Balance_UpdatesWhenEarlierTransactionIsRemoved()
	{
		BankingTransactionViewModel tx1 = this.account.NewTransaction();
		tx1.When = new DateTime(2021, 1, 2);
		tx1.Amount = 5m;

		BankingTransactionViewModel tx2 = this.account.NewTransaction();
		tx2.When = new DateTime(2021, 1, 3);
		tx2.Amount = 3m;
		Assert.Equal(tx1.Balance + tx2.Amount, tx2.Balance);

		this.account.DeleteTransaction(tx1);
		Assert.Equal(tx2.Amount, tx2.Balance);
	}

	[Fact]
	public void ApplyTo()
	{
		Transaction transaction = this.viewModel.Transaction;

		this.viewModel.Payee = this.payee;
		this.viewModel.Amount = this.amount;
		this.viewModel.When = this.when;
		this.viewModel.Memo = this.memo;
		this.viewModel.CheckNumber = this.checkNumber;
		this.viewModel.Cleared = this.cleared;
		this.viewModel.ApplyToModel();

		Assert.Equal(this.payee, transaction.Payee);
		Assert.Equal(this.when, transaction.When);
		Assert.Equal(this.memo, transaction.Memo);
		Assert.Equal(this.checkNumber, transaction.CheckNumber);

		TransactionEntry entry = Assert.Single(this.viewModel.Entries).Model;
		Assert.Equal(this.account.Id, entry.AccountId);
		Assert.Equal(this.amount, entry.Amount);
		Assert.Equal(this.cleared, entry.Cleared);

		// Test auto-save behavior for transaction model.
		this.viewModel.Memo = "bonus";
		Assert.Equal(this.viewModel.Memo, transaction.Memo);

		// Test auto-save behavior for transaction entry model.
		this.viewModel.Amount = this.amount + 1;
		Assert.Equal(this.amount + 1, entry.Amount);

		// Test negative amount.
		this.viewModel.Amount = -this.amount;
		entry = Assert.Single(this.viewModel.Entries).Model;
		Assert.Equal(-this.amount, entry.Amount);
		Assert.Equal(this.account.Id, entry.AccountId);

		// Test a money transfer.
		this.viewModel.OtherAccount = this.otherAccount;
		TransactionEntry otherEntry = this.viewModel.Entries.Single(e => e.Account == this.otherAccount).Model;
		Assert.Equal(this.otherAccount.Id, otherEntry.AccountId);
	}

	[Fact]
	public void ApplyTo_WithSplits()
	{
		this.viewModel.Amount = 6;
		TransactionEntryViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 2;
		TransactionEntryViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 4;

		Assert.Equal(6, this.viewModel.Amount);
		Assert.Same(this.DocumentViewModel.SplitCategory, this.viewModel.OtherAccount);

		TransactionEntry splitModel1 = this.Money.TransactionEntries.First(s => s.Id == split1.Id);
		Assert.Equal(split1.Amount, splitModel1.Amount);
		Assert.Equal(this.viewModel.TransactionId, splitModel1.TransactionId);
		Assert.Equal(this.viewModel.ThisAccount.Id, splitModel1.AccountId);

		TransactionEntry splitModel2 = this.Money.TransactionEntries.First(s => s.Id == split2.Id);
		Assert.Equal(split2.Amount, splitModel2.Amount);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom_Category()
	{
		CategoryAccountViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");
		this.viewModel.Transaction.Id = 1;
		List<TransactionAndEntry> transactionAndEntries = new()
		{
			new TransactionAndEntry()
			{
				TransactionId = this.viewModel.TransactionId,
				Payee = this.payee,
				When = this.when,
				TransactionMemo = this.memo,
				CheckNumber = this.checkNumber,
				TransactionEntryId = 1,
				AccountId = categoryViewModel.Id,
				Amount = -this.amount,
				AssetId = this.account.CurrencyAsset!.Id,
			},
			new TransactionAndEntry()
			{
				TransactionId = this.viewModel.TransactionId,
				Payee = this.payee,
				When = this.when,
				TransactionMemo = this.memo,
				CheckNumber = this.checkNumber,
				TransactionEntryId = 2,
				Cleared = this.cleared,
				AccountId = this.account.Id,
				Amount = this.amount,
				AssetId = this.account.CurrencyAsset!.Id,
			},
		};

		this.viewModel.CopyFrom(transactionAndEntries);

		Assert.Equal(this.payee, this.viewModel.Payee);
		Assert.Equal(this.amount, this.viewModel.Amount);
		Assert.Equal(this.when, this.viewModel.When);
		Assert.Equal(this.memo, this.viewModel.Memo);
		Assert.Equal(this.checkNumber, this.viewModel.CheckNumber);
		Assert.Equal(this.cleared, this.viewModel.Cleared);
		Assert.Equal(categoryViewModel.Id, Assert.IsType<CategoryAccountViewModel>(this.viewModel.OtherAccount).Id);

		// Test auto-save behavior.
		this.viewModel.Memo = "another memo";
		Assert.Equal(this.viewModel.Memo, this.viewModel.Transaction.Memo);

		// Remove the category assignment.
		transactionAndEntries.RemoveAt(0);
		this.viewModel.CopyFrom(transactionAndEntries);
		Assert.Null(this.viewModel.OtherAccount);
	}

	[Fact]
	public void CopyFrom_TransferToAccount()
	{
		List<TransactionAndEntry> transactionAndEntries = new()
		{
			new TransactionAndEntry()
			{
				TransactionId = this.viewModel.TransactionId,
				TransactionEntryId = 1,
				Amount = this.amount,
				AccountId = this.account.Id,
			},
			new TransactionAndEntry()
			{
				TransactionId = this.viewModel.TransactionId,
				TransactionEntryId = 2,
				Amount = -this.amount,
				AccountId = this.otherAccount.Id,
			},
		};

		this.viewModel.CopyFrom(transactionAndEntries);

		Assert.Equal(this.amount, this.viewModel.Amount);
		Assert.False(this.viewModel.ContainsSplits);
		Assert.Same(this.otherAccount, this.viewModel.OtherAccount);
	}

	[Fact]
	public void CopyFrom_TransferFromAccount()
	{
		List<TransactionAndEntry> transactionAndEntries = new()
		{
			new TransactionAndEntry()
			{
				TransactionId = this.viewModel.TransactionId,
				TransactionEntryId = 1,
				Amount = this.amount - 1,
				AccountId = this.otherAccount.Id,
			},
			new TransactionAndEntry()
			{
				TransactionId = this.viewModel.TransactionId,
				TransactionEntryId = 2,
				Amount = -this.amount,
				AccountId = this.account.Id,
			},
		};

		this.viewModel.CopyFrom(transactionAndEntries);

		Assert.Equal(-this.amount, this.viewModel.Amount);
		Assert.Same(this.otherAccount, this.viewModel.OtherAccount);
	}

	[Fact]
	public void CopyFrom_SplitTransaction()
	{
		Transaction transaction = new()
		{
		};
		this.Money.Insert(transaction);

		TransactionEntry homeEntry = new() { Amount = -4, AccountId = this.account.Id, TransactionId = transaction.Id, AssetId = this.account.CurrencyAsset!.Id };
		TransactionEntry split1 = new() { Amount = 3, AccountId = this.otherAccount.Id, TransactionId = transaction.Id, AssetId = this.account.CurrencyAsset!.Id };
		TransactionEntry split2 = new() { Amount = 1, AccountId = this.category1.Id, TransactionId = transaction.Id, AssetId = this.account.CurrencyAsset!.Id };
		this.Money.InsertAll(homeEntry, split1, split2);

		this.ReloadViewModel();

		this.viewModel = Assert.Single(this.account.Transactions, t => t.TransactionId == transaction.Id);
		Assert.Equal(-4, this.viewModel.Amount);
		Assert.Same(this.DocumentViewModel.SplitCategory, this.viewModel.OtherAccount);
		Assert.Equal(3, this.viewModel.Splits.Count);
		Assert.Single(this.viewModel.Splits, s => s.Amount == -split1.Amount);
		Assert.Single(this.viewModel.Splits, s => s.Amount == -split2.Amount);
	}

	[Fact]
	public void ChangesAfterCloseDoNotThrowException()
	{
		this.viewModel.Payee = "some person";
		this.viewModel.Amount = 50;
		this.Money.Dispose();
		this.viewModel.Amount = 12;
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();

		this.account = (BankingAccountViewModel)this.DocumentViewModel.GetAccount(this.account.Id);
		this.otherAccount = (BankingAccountViewModel)this.DocumentViewModel.GetAccount(this.otherAccount.Id);
		this.category1 = this.DocumentViewModel.GetCategory(this.category1.Id);
		this.category2 = this.DocumentViewModel.GetCategory(this.category2.Id);

		if (this.viewModel.IsPersisted)
		{
			this.viewModel = this.account.Transactions.Single(t => t.TransactionId == this.viewModel.TransactionId);
		}
		else
		{
			this.viewModel = this.account.Transactions[^1];
		}
	}

	private BankingTransactionViewModel SplitAndFetchForeignTransactionViewModel()
	{
		TransactionEntryViewModel split = this.viewModel.NewSplit();
		split.Account = this.otherAccount;
		BankingTransactionViewModel foreignSplitTransaction = this.otherAccount.Transactions.Single(t => t.TransactionId == this.viewModel.TransactionId);
		return foreignSplitTransaction;
	}
}
