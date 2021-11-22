// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class TransactionViewModelTests : MoneyTestBase
{
	private AccountViewModel account;
	private AccountViewModel otherAccount;

	private TransactionViewModel viewModel;

	private string payee = "some person";

	private decimal amount = 5.5m;

	private string memo = "Some memo";

	private DateTime when = DateTime.Now - TimeSpan.FromDays(3);

	private int? checkNumber = 15;

	private ClearedState cleared = ClearedState.None;

	public TransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		Account thisAccountModel = this.Money.Insert(new Account { Name = "this" });
		Account otherAccountModel = this.Money.Insert(new Account { Name = "other" });

		this.account = this.DocumentViewModel.GetAccount(thisAccountModel.Id);
		this.otherAccount = this.DocumentViewModel.GetAccount(otherAccountModel.Id);
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.viewModel = this.account.Transactions[^1];
	}

	[Fact]
	public void When()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.When = this.when,
			nameof(this.viewModel.When));
		Assert.Equal(this.when, this.viewModel.When);

		TransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.Throws<InvalidOperationException>(() => foreignSplitTransaction.When = DateTime.Now);

		// When linked across split transfers
		Assert.Equal(this.viewModel.When, foreignSplitTransaction.When);
		this.viewModel.When = DateTime.Now;
		Assert.Equal(this.viewModel.When, foreignSplitTransaction.When);
	}

	[Fact]
	public void WhenIsReadOnly()
	{
		Assert.False(this.viewModel.WhenIsReadOnly);
		TransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.True(foreignSplitTransaction.WhenIsReadOnly);
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
	public void AmountIsReadOnly()
	{
		this.viewModel.Save();
		Assert.False(this.viewModel.AmountIsReadOnly);
		TransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.True(foreignSplitTransaction.AmountIsReadOnly);
		Assert.True(foreignSplitTransaction.GetSplitParent()!.AmountIsReadOnly);
	}

	[Fact]
	public void Amount_OnSplitTransactions_ViewModelOnly()
	{
		this.viewModel.Amount = -50;
		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		Assert.Equal(-50, this.viewModel.Amount);
		Assert.Equal(-50, split1.Amount);
		Assert.Equal(0, this.viewModel.Model!.Amount);

		split1.Amount = -40;
		Assert.Equal(-40, this.viewModel.Amount);
		Assert.Equal(-40, split1.Amount);
		Assert.Equal(0, this.viewModel.Model!.Amount);

		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		Assert.Equal(-40, this.viewModel.Amount);
		Assert.Equal(-40, split1.Amount);
		split2.Amount = -30;
		Assert.Equal(-70, this.viewModel.Amount);
		Assert.Equal(0, this.viewModel.Model!.Amount);

		this.ReloadViewModel();
		Assert.Equal(-70, this.viewModel.Amount);
	}

	[Fact]
	public void Balance_OnSplitTransactions()
	{
		this.viewModel.Save();
		this.viewModel.Amount = -50;
		Assert.Equal(-50, this.viewModel.Balance);
		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		Assert.Equal(-50, this.viewModel.Balance);

		split1.Amount = -40;
		Assert.Equal(-40, this.viewModel.Balance);

		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = -30;
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
	public void Payee()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Payee = "somebody",
			nameof(this.viewModel.Payee));
		Assert.Same("somebody", this.viewModel.Payee);

		TransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.Throws<InvalidOperationException>(() => foreignSplitTransaction.Payee = "me");

		// Payee linked across split transfers
		Assert.Equal(this.viewModel.Payee, foreignSplitTransaction.Payee);
		this.viewModel.Payee = "somebody else";
		Assert.Equal(this.viewModel.Payee, foreignSplitTransaction.Payee);
	}

	[Fact]
	public void PayeeIsReadOnly()
	{
		Assert.False(this.viewModel.PayeeIsReadOnly);
		TransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.True(foreignSplitTransaction.PayeeIsReadOnly);
	}

	[Fact]
	public void Splits_Empty()
	{
		Assert.Empty(this.viewModel.Splits);
	}

	[Fact]
	public void NewSplit_EmptyTransaction()
	{
		SplitTransactionViewModel split = this.viewModel.NewSplit();
		Assert.Same(this.viewModel, split.ParentTransaction);
		Assert.Same(split, this.viewModel.Splits[0]);
		Assert.Equal(2, this.viewModel.Splits.Count);
		Assert.True(this.viewModel.Splits[0].IsPersisted);
		Assert.False(this.viewModel.Splits[1].IsPersisted);
	}

	[Fact]
	public void NewSplit_MovesCategory()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");
		this.viewModel.CategoryOrTransfer = categoryViewModel;
		SplitTransactionViewModel split = this.viewModel.NewSplit();

		this.AssertNowAndAfterReload(delegate
		{
			Assert.Same(SplitCategoryPlaceholder.Singleton, this.viewModel.CategoryOrTransfer);
			split = this.viewModel.Splits[0];
			Assert.Equal(categoryViewModel.Id, split.CategoryOrTransfer?.Id);
		});
	}

	[Fact]
	public void MultipleNewSplits()
	{
		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		Assert.Null(split1.CategoryOrTransfer);
		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		Assert.Null(split2.CategoryOrTransfer);
	}

	[Fact]
	public void DeleteSplit()
	{
		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		Assert.Equal(3, this.viewModel.Splits.Count);
		this.viewModel.DeleteSplit(split1);
		Assert.Equal(2, this.viewModel.Splits.Count);
		this.viewModel.DeleteSplit(split2);
		Assert.Equal(0, this.viewModel.Splits.Count); // jump to zero since the only remaining split was provisionary
	}

	[Fact]
	public void DeleteSplitCommand()
	{
		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		Assert.Equal(3, this.viewModel.Splits.Count);

		this.viewModel.SelectedSplit = split2;
		Assert.Equal(3, this.viewModel.Splits.Count);

		Assert.True(this.viewModel.DeleteSplitCommand.CanExecute(null));
		this.viewModel.DeleteSplitCommand.Execute(null);
		Assert.Equal(2, this.viewModel.Splits.Count);
		Assert.Same(this.viewModel.Splits[1], this.viewModel.SelectedSplit);
	}

	[Fact]
	public void DeleteSplit_LastSplitMergesIntoParent()
	{
		CategoryViewModel cat1 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat1");
		CategoryViewModel cat2 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat2");

		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 10;
		split1.CategoryOrTransfer = cat1;
		split1.Memo = "memo1";
		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 5;
		split2.CategoryOrTransfer = cat2;
		split2.Memo = "memo2";

		this.viewModel.DeleteSplit(split1);
		Assert.Equal(5, this.viewModel.Amount);
		Assert.Same(SplitCategoryPlaceholder.Singleton, this.viewModel.CategoryOrTransfer);
		Assert.Null(this.viewModel.Memo);

		this.viewModel.DeleteSplit(split2);
		Assert.Equal(5, this.viewModel.Amount);
		Assert.Same(cat2, this.viewModel.CategoryOrTransfer);
		Assert.Equal("memo2", this.viewModel.Memo);
	}

	[Fact]
	public async Task SplitCommand_OneSplit_ThenDelete()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");

		Assert.True(this.viewModel.SplitCommand.CanExecute(null));
		await TestUtilities.AssertPropertyChangedEventAsync(this.viewModel, () => this.viewModel.SplitCommand.ExecuteAsync(), nameof(this.viewModel.ContainsSplits));
		Assert.True(this.viewModel.ContainsSplits);

		SplitTransactionViewModel split = this.viewModel.Splits[0];
		split.Amount = 10;
		split.CategoryOrTransfer = categoryViewModel;

		Assert.True(this.viewModel.SplitCommand.CanExecute(null));
		await TestUtilities.AssertPropertyChangedEventAsync(this.viewModel, () => this.viewModel.SplitCommand.ExecuteAsync(), nameof(this.viewModel.ContainsSplits));

		// We expect no prompts because one split can collapse to a simple transaction with little or no data loss.
		Assert.Equal(0, this.UserNotification.ConfirmCounter);

		this.AssertNowAndAfterReload(delegate
		{
			Assert.False(this.viewModel.ContainsSplits);
			Assert.Equal(10, this.viewModel.Amount);
			Assert.Equal(categoryViewModel.Name, this.viewModel.CategoryOrTransfer?.Name);
		});

		// Confirm the split was deleted from the database.
		Assert.Empty(this.Money.Transactions.Where(tx => tx.Id == split.Id));
	}

	[Theory, PairwiseData]
	public async Task SplitCommand_DeleteTwoSplits_IncludesPrompt(bool confirmed)
	{
		this.UserNotification.ChosenAction = confirmed ? IUserNotification.UserAction.Yes : IUserNotification.UserAction.No;

		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 10;
		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 5;

		Assert.True(this.viewModel.SplitCommand.CanExecute(null));
		await this.viewModel.SplitCommand.ExecuteAsync();
		Assert.Equal(1, this.UserNotification.ConfirmCounter);

		this.AssertNowAndAfterReload(delegate
		{
			if (confirmed)
			{
				Assert.False(this.viewModel.ContainsSplits);
				Assert.Equal(15, this.viewModel.Amount);
				Assert.Null(this.viewModel.CategoryOrTransfer);
			}
			else
			{
				Assert.Equal(3, this.viewModel.Splits.Count);
			}
		});
	}

	[Fact]
	public void ChangingVolatileTransactionProducesNewOne()
	{
		SplitTransactionViewModel tx1 = this.viewModel.NewSplit();
		SplitTransactionViewModel volatileTx = this.viewModel.Splits[1];
		Assert.True(tx1.IsPersisted);
		Assert.False(volatileTx.IsPersisted);
		volatileTx.Amount = 50;
		Assert.True(volatileTx.IsPersisted);
		Assert.Equal(3, this.viewModel.Splits.Count);
		SplitTransactionViewModel volatileTx2 = this.viewModel.Splits[2];
		Assert.False(volatileTx2.IsPersisted);
	}

	[Fact]
	public void CategoryOrTransfer_ThrowsWhenSplit()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");
		SplitTransactionViewModel split = this.viewModel.NewSplit();

		// Setting a category should throw when a transaction is split.
		Assert.Throws<InvalidOperationException>(() => this.viewModel.CategoryOrTransfer = categoryViewModel);
		Assert.Throws<InvalidOperationException>(() => this.viewModel.CategoryOrTransfer = null);

		// Setting to the singleton split value should be allowed.
		this.viewModel.CategoryOrTransfer = SplitCategoryPlaceholder.Singleton;

		this.viewModel.DeleteSplit(split);
		this.viewModel.CategoryOrTransfer = categoryViewModel;
		Assert.Same(categoryViewModel, this.viewModel.CategoryOrTransfer);
	}

	[Fact]
	public void CategoryOrTransferIsReadOnly()
	{
		Assert.False(this.viewModel.CategoryOrTransferIsReadOnly);
		TransactionViewModel foreignSplitTransaction = this.SplitAndFetchForeignTransactionViewModel();
		Assert.True(foreignSplitTransaction.CategoryOrTransferIsReadOnly);
	}

	[Fact]
	public void AvailableTransactionTargets()
	{
		Assert.DoesNotContain(this.viewModel.AvailableTransactionTargets, tt => tt == SplitCategoryPlaceholder.Singleton);
		Assert.DoesNotContain(this.viewModel.AvailableTransactionTargets, tt => tt == this.viewModel.ThisAccount);
		Assert.NotEmpty(this.viewModel.AvailableTransactionTargets);
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
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");
		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		split1.CategoryOrTransfer = categoryViewModel;
		split1.Amount = this.amount;
		split1.Memo = this.memo;

		this.ReloadViewModel();

		categoryViewModel = this.DocumentViewModel.CategoriesPanel.Categories.Single();
		split1 = this.viewModel.Splits[0];
		Assert.Equal(this.amount, split1.Amount);
		Assert.Equal(this.memo, split1.Memo);
		Assert.Same(categoryViewModel, split1.CategoryOrTransfer);
	}

	[Fact]
	public void Balance_JustOneTransaction()
	{
		// Verify that one lone transaction's balance is based on its own amount.
		TransactionViewModel tx = this.account.NewTransaction();
		tx.When = new DateTime(2021, 1, 2);
		Assert.Equal(0m, tx.Balance);
		tx.Amount = 5m;
		Assert.Equal(tx.Amount, tx.Balance);
	}

	[Fact]
	public void Balance_SecondTransactionBuildsOnFirst()
	{
		TransactionViewModel tx1 = this.account.NewTransaction();
		tx1.When = new DateTime(2021, 1, 2);
		tx1.Amount = 5m;

		TransactionViewModel tx2 = this.account.NewTransaction();
		tx2.When = new DateTime(2021, 1, 3);
		Assert.Equal(tx1.Balance + tx2.Amount, tx2.Balance);
		tx2.Amount = 8m;
		Assert.Equal(tx1.Balance + tx2.Amount, tx2.Balance);
	}

	[Fact]
	public void Balance_UpdatesInResponseToTransactionInsertedAbove()
	{
		TransactionViewModel tx2 = this.account.NewTransaction();
		tx2.When = new DateTime(2021, 1, 2);
		tx2.Amount = 5m;

		TransactionViewModel tx1 = this.account.NewTransaction();
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
		TransactionViewModel tx1 = this.account.NewTransaction();
		tx1.When = new DateTime(2021, 1, 2);
		tx1.Amount = 5m;

		TransactionViewModel tx2 = this.account.NewTransaction();
		tx2.When = new DateTime(2021, 1, 3);
		tx2.Amount = 3m;
		Assert.Equal(tx1.Balance + tx2.Amount, tx2.Balance);

		this.account.DeleteTransaction(tx1);
		Assert.Equal(tx2.Amount, tx2.Balance);
	}

	[Fact]
	public void ApplyTo_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));
	}

	[Fact]
	public void ApplyTo()
	{
		Transaction transaction = new Transaction();
		TransactionViewModel viewModel = new(this.account, null);

		viewModel.Payee = this.payee;
		viewModel.Amount = this.amount;
		viewModel.When = this.when;
		viewModel.Memo = this.memo;
		viewModel.CheckNumber = this.checkNumber;
		viewModel.Cleared = this.cleared;
		viewModel.ApplyTo(transaction);

		Assert.Equal(this.account.Id, transaction.CreditAccountId);
		Assert.Null(transaction.DebitAccountId);
		Assert.Equal(this.payee, transaction.Payee);
		Assert.Equal(this.amount, transaction.Amount);
		Assert.Equal(this.when, transaction.When);
		Assert.Equal(this.memo, transaction.Memo);
		Assert.Equal(this.checkNumber, transaction.CheckNumber);
		Assert.Equal(this.cleared, transaction.Cleared);

		// Test auto-save behavior.
		viewModel.Memo = "bonus";
		Assert.Equal(viewModel.Memo, transaction.Memo);

		// Test negative amount.
		viewModel.Amount *= -1;
		Assert.Equal(transaction.Amount, this.amount);
		Assert.Equal(this.account.Id, transaction.DebitAccountId);
		Assert.Null(transaction.CreditAccountId);

		// Test a money transfer.
		viewModel.CategoryOrTransfer = this.otherAccount;
		Assert.Equal(this.otherAccount.Id, transaction.CreditAccountId);
	}

	[Fact]
	public void ApplyTo_WithSplits()
	{
		this.viewModel.Amount = 6;
		SplitTransactionViewModel split1 = this.viewModel.NewSplit();
		split1.Amount = 2;
		SplitTransactionViewModel split2 = this.viewModel.NewSplit();
		split2.Amount = 4;

		Assert.Equal(6, this.viewModel.Amount);
		Assert.Equal(0, this.viewModel.Model!.Amount);
		Assert.Equal(Category.Split, this.viewModel.Model.CategoryId);

		Transaction splitModel1 = this.Money.Transactions.First(s => s.Id == split1.Id);
		Assert.Equal(split1.Amount, splitModel1.Amount);
		Assert.Equal(this.viewModel.Id, splitModel1.ParentTransactionId);
		Assert.Equal(this.viewModel.ThisAccount.Id, splitModel1.CreditAccountId);
		Assert.Null(splitModel1.DebitAccountId);

		Transaction splitModel2 = this.Money.Transactions.First(s => s.Id == split2.Id);
		Assert.Equal(split2.Amount, splitModel2.Amount);
	}

	[Fact]
	public void ApplyToThrowsOnEntityMismatch()
	{
		this.viewModel.CopyFrom(this.viewModel.Model!);
		Assert.Throws<ArgumentException>(() => this.viewModel.ApplyTo(new Transaction { Id = this.viewModel.Model!.Id + 1 }));
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom_Category()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");

		Transaction transaction = this.viewModel.Model!;
		transaction.Payee = this.payee;
		transaction.Amount = this.amount;
		transaction.When = this.when;
		transaction.Memo = this.memo;
		transaction.CheckNumber = this.checkNumber;
		transaction.Cleared = this.cleared;
		transaction.CategoryId = categoryViewModel.Id;
		transaction.DebitAccountId = this.account.Id;

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(transaction.Payee, this.viewModel.Payee);
		Assert.Equal(-transaction.Amount, this.viewModel.Amount);
		Assert.Equal(transaction.When, this.viewModel.When);
		Assert.Equal(transaction.Memo, this.viewModel.Memo);
		Assert.Equal(transaction.CheckNumber, this.viewModel.CheckNumber);
		Assert.Equal(transaction.Cleared, this.viewModel.Cleared);
		Assert.Equal(categoryViewModel.Id, Assert.IsType<CategoryViewModel>(this.viewModel.CategoryOrTransfer).Id);

		// Test auto-save behavior.
		this.viewModel.Memo = "another memo";
		Assert.Equal(this.viewModel.Memo, transaction.Memo);

		transaction.CategoryId = null;
		this.viewModel.CopyFrom(transaction);
		Assert.Null(this.viewModel.CategoryOrTransfer);
	}

	[Fact]
	public void CopyFrom_TransferToAccount()
	{
		Transaction transaction = this.viewModel.Model!;
		transaction.Amount = this.amount;
		transaction.CreditAccountId = this.account.Id;
		transaction.DebitAccountId = this.otherAccount.Id;
		this.Money.Insert(transaction);

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(transaction.Amount, this.viewModel.Amount);
		Assert.Equal(this.otherAccount.Id, Assert.IsType<AccountViewModel>(this.viewModel.CategoryOrTransfer).Id);
	}

	[Fact]
	public void CopyFrom_TransferFromAccount()
	{
		Transaction transaction = new Transaction
		{
			Amount = this.amount,
			CreditAccountId = this.otherAccount.Id,
			DebitAccountId = this.account.Id,
		};

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(-transaction.Amount, this.viewModel.Amount);
		Assert.Equal(this.otherAccount.Id, Assert.IsType<AccountViewModel>(this.viewModel.CategoryOrTransfer).Id);
	}

	[Fact]
	public void CopyFrom_SplitTransaction()
	{
		Transaction transaction = new()
		{
			CategoryId = Category.Split,
			CreditAccountId = this.account.Id,
		};
		this.Money.Insert(transaction);

		Transaction split1 = new() { Amount = 3, CreditAccountId = this.account.Id, ParentTransactionId = transaction.Id };
		this.Money.Insert(split1);
		Transaction split2 = new() { Amount = 7, CreditAccountId = this.account.Id, ParentTransactionId = transaction.Id };
		this.Money.Insert(split2);

		this.ReloadViewModel();

		this.account = Assert.Single(this.DocumentViewModel.BankingPanel.Accounts, a => a.Id == this.account.Id);
		this.viewModel = Assert.Single(this.account.Transactions, t => t.Id == transaction.Id);
		Assert.Equal(10, this.viewModel.Amount);
		Assert.Same(SplitCategoryPlaceholder.Singleton, this.viewModel.CategoryOrTransfer);
		Assert.Equal(3, this.viewModel.Splits.Count);
		Assert.Single(this.viewModel.Splits, s => s.Amount == split1.Amount);
		Assert.Single(this.viewModel.Splits, s => s.Amount == split2.Amount);
	}

	[Fact]
	public void Ctor_From_Volatile_Entity()
	{
		var transaction = new Transaction
		{
			Payee = "some person",
		};

		this.viewModel = new TransactionViewModel(this.account, transaction);

		Assert.Equal(transaction.Id, this.viewModel.Id);
		Assert.Equal(transaction.Payee, this.viewModel.Payee);

		// Test auto-save behavior.
		Assert.Equal(0, this.viewModel.Id);
		this.viewModel.Payee = "another name";
		Assert.Equal(this.viewModel.Payee, transaction.Payee);
		Assert.Equal(transaction.Id, this.viewModel.Id);
		Assert.NotEqual(0, this.viewModel.Id);

		Transaction fromDb = this.Money.Transactions.First(tx => tx.Id == transaction.Id);
		Assert.Equal(transaction.Payee, fromDb.Payee);
		Assert.Single(this.Money.Transactions);
	}

	[Fact]
	public void Ctor_From_Db_Entity()
	{
		var transaction = new Transaction
		{
			Payee = "some person",
		};
		this.Money.Insert(transaction);

		this.viewModel = new TransactionViewModel(this.account, transaction);

		Assert.Equal(transaction.Id, this.viewModel.Id);
		Assert.Equal(transaction.Payee, this.viewModel.Payee);
		Assert.Equal(transaction.Memo, this.viewModel.Memo);

		// Test auto-save behavior.
		this.viewModel.Payee = "some other person";
		Assert.Equal(this.viewModel.Payee, transaction.Payee);

		Transaction fromDb = this.Money.Transactions.First(tx => tx.Id == transaction.Id);
		Assert.Equal(transaction.Payee, fromDb.Payee);
		Assert.Single(this.Money.Transactions);
	}

	[Fact]
	public void ChangesAfterCloseDoNotThrowException()
	{
		var transaction = new Transaction
		{
			Payee = "some person",
		};
		this.Money.Insert(transaction);

		this.viewModel = new TransactionViewModel(this.account, transaction);
		this.Money.Dispose();
		this.viewModel.Amount = 12;
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();

		this.account = this.DocumentViewModel.GetAccount(this.account.Id!.Value);
		this.otherAccount = this.DocumentViewModel.GetAccount(this.otherAccount.Id!.Value);

		if (this.viewModel.Id.HasValue)
		{
			this.viewModel = this.account.Transactions.Single(t => t.Id == this.viewModel.Id.Value);
		}
	}

	private TransactionViewModel SplitAndFetchForeignTransactionViewModel()
	{
		SplitTransactionViewModel split = this.viewModel.NewSplit();
		Assert.True(this.viewModel.CategoryOrTransferIsReadOnly);
		split.CategoryOrTransfer = this.otherAccount;
		TransactionViewModel foreignSplitTransaction = this.otherAccount.Transactions.Single(t => t.Id == split.Id);
		return foreignSplitTransaction;
	}
}
