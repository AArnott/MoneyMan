// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class AccountViewModelTests : MoneyTestBase
{
	private AccountViewModel checking;
	private AccountViewModel savings;

	public AccountViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.checking = this.DocumentViewModel.AccountsPanel.NewAccount("Checking");
		this.savings = this.DocumentViewModel.AccountsPanel.NewAccount("Savings");
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
	}

	[Fact]
	public void Name()
	{
		Assert.Equal("Checking", this.checking.Name);
		TestUtilities.AssertPropertyChangedEvent(
			this.checking,
			() => this.checking.Name = "changed",
			nameof(this.checking.Name));
		Assert.Equal("changed", this.checking.Name);
	}

	[Fact]
	public void Name_Validation()
	{
		this.checking.Name = string.Empty;

		this.Logger.WriteLine(this.checking.Error);
		Assert.NotEqual(string.Empty, this.checking[nameof(this.checking.Name)]);
		Assert.Equal(this.checking[nameof(this.checking.Name)], this.checking.Error);

		this.checking.Name = "a";
		Assert.Equal(string.Empty, this.checking[nameof(this.checking.Name)]);
	}

	[Fact]
	public void TransferTargetName()
	{
		this.checking.Name = "tt-test";
		Assert.Equal($"[{this.checking.Name}]", this.checking.TransferTargetName);
	}

	[Fact]
	public void TransferTargetName_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(this.checking, () => this.checking.Name = "other", nameof(this.checking.Name), nameof(this.checking.TransferTargetName));
	}

	[Fact]
	public void IsClosed()
	{
		Assert.False(this.checking.IsClosed);
		this.checking.IsClosed = true;
		Assert.True(this.checking.IsClosed);
	}

	[Fact]
	public void IsClosed_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.checking,
			() => this.checking.IsClosed = true,
			nameof(this.checking.IsClosed));
		TestUtilities.AssertPropertyChangedEvent(
				this.checking,
				() => this.checking.IsClosed = true);
	}

	[Fact]
	public void ApplyTo_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.checking.ApplyTo(null!));
	}

	[Fact]
	public void ApplyTo()
	{
		this.checking.Name = "some name";
		this.checking.IsClosed = !this.checking.IsClosed;

		this.checking.ApplyTo(this.checking.Model!);

		Assert.Equal(this.checking.Name, this.checking.Model!.Name);
		Assert.Equal(this.checking.IsClosed, this.checking.Model.IsClosed);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.checking.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom()
	{
		this.checking.Model!.Name = "another name";
		this.checking.Model.IsClosed = true;
		this.checking.CopyFrom(this.checking.Model);

		Assert.Equal(this.checking.Model.Name, this.checking.Name);
		Assert.Equal(this.checking.Model.IsClosed, this.checking.IsClosed);
	}

	[Fact]
	public void Ctor_From_Volatile_Entity()
	{
		AccountViewModel newAccount = this.DocumentViewModel.AccountsPanel.NewAccount();
		Assert.Equal(0, newAccount.Model!.Id);

		Assert.Equal(newAccount.Name, newAccount.Model.Name);

		// Test auto-save behavior.
		newAccount.Name = "another name";
		Assert.Equal(newAccount.Name, newAccount.Model.Name);

		Account fromDb = this.Money.Accounts.First(tx => tx.Id == newAccount.Model.Id);
		Assert.Equal(newAccount.Model.Name, fromDb.Name);
		Assert.Single(this.Money.Accounts, a => a.Name == newAccount.Name);
	}

	[Fact]
	public void Ctor_From_Db_Entity()
	{
		var account = new Account
		{
			Name = "some person",
		};
		this.Money.Insert(account);

		var alternate = new AccountViewModel(account, this.DocumentViewModel);

		Assert.Equal(account.Id, alternate.Id);
		Assert.Equal(account.Name, alternate.Name);

		// Test auto-save behavior.
		alternate.Name = "some other person";
		Assert.Equal(alternate.Name, account.Name);

		Account fromDb = this.Money.Accounts.First(tx => tx.Id == account.Id);
		Assert.Equal(account.Name, fromDb.Name);
		Assert.Single(this.Money.Accounts, a => a.Name == alternate.Name);
	}

	[Fact]
	public void PopulatesWithTransactionsFromDb()
	{
		var account = new Account
		{
			Name = "some account",
		};
		this.Money.Insert(account);
		this.Money.InsertAll(new ModelBase[]
		{
			new Transaction { CreditAccountId = account.Id },
			new Transaction { DebitAccountId = account.Id },
		});
		this.checking = new AccountViewModel(account, this.DocumentViewModel);
		Assert.Equal(2, this.checking.Transactions.Count);
	}

	[Fact]
	public async Task DeleteTransaction()
	{
		var account = new Account
		{
			Name = "some account",
		};
		this.Money.Insert(account);
		this.Money.Insert(new Transaction { CreditAccountId = account.Id, Amount = 5 });
		this.checking = new AccountViewModel(account, this.DocumentViewModel);
		TransactionViewModel txViewModel = Assert.Single(this.checking.Transactions);
		this.DocumentViewModel.SelectedTransaction = txViewModel;
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Assert.Empty(this.Money.Transactions);
	}

	[Fact]
	public void DeleteVolatileTransaction()
	{
		TransactionViewModel tx = this.checking.NewTransaction(volatileOnly: true);
		this.checking.Add(tx);
		this.checking.DeleteTransaction(tx);
		Assert.Empty(this.checking.Transactions);
	}

	[Fact]
	public async Task DeleteTransactions()
	{
		var account = new Account { Name = "some account" };
		this.Money.Insert(account);
		this.Money.Insert(new Transaction { CreditAccountId = account.Id, Amount = 5 });
		this.Money.Insert(new Transaction { CreditAccountId = account.Id, Amount = 12 });
		this.Money.Insert(new Transaction { CreditAccountId = account.Id, Amount = 15 });
		this.checking = new AccountViewModel(account, this.DocumentViewModel);
		Assert.False(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
		this.DocumentViewModel.SelectedTransactions = this.checking.Transactions.Where(t => t.Amount != 12).ToArray();
		Assert.True(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Assert.Equal(12, Assert.Single(this.Money.Transactions).Amount);
	}

	[Fact]
	public void Balance()
	{
		var account = new Account
		{
			Name = "some account",
		};
		this.Money.Insert(account);
		this.checking = new AccountViewModel(account, this.DocumentViewModel);
		Assert.Equal(0m, this.checking.Balance);

		this.Money.InsertAll(new ModelBase[]
		{
			new Transaction { Amount = 10, CreditAccountId = account.Id },
			new Transaction { Amount = 2, DebitAccountId = account.Id },
		});
		this.checking = new AccountViewModel(account, this.DocumentViewModel);
		Assert.Equal(8m, this.checking.Balance);
	}

	[Fact]
	public void NewTransactionAddedToCollection()
	{
		TransactionViewModel tx = this.checking.NewTransaction();
		tx.Memo = "some memo";
		Assert.Same(tx, Assert.Single(this.checking.Transactions));
	}

	[Fact]
	public void TransactionSorting_2Transactions()
	{
		TransactionViewModel tx1 = this.checking.NewTransaction(volatileOnly: false);
		tx1.When = new DateTime(2021, 1, 1);

		TransactionViewModel tx2 = this.checking.NewTransaction(volatileOnly: false);
		tx2.When = new DateTime(2021, 1, 2);

		Assert.Equal(new[] { tx1.Id, tx2.Id }, this.checking.Transactions.Select(tx => tx.Id));

		tx1.When = tx2.When + TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.Id, tx1.Id }, this.checking.Transactions.Select(tx => tx.Id));
	}

	[Fact]
	public void TransactionSorting_3Transactions()
	{
		TransactionViewModel tx1 = this.checking.NewTransaction(volatileOnly: false);
		tx1.When = new DateTime(2021, 1, 1);

		TransactionViewModel tx2 = this.checking.NewTransaction(volatileOnly: false);
		tx2.When = new DateTime(2021, 1, 3);

		TransactionViewModel tx3 = this.checking.NewTransaction(volatileOnly: false);
		tx3.When = new DateTime(2021, 1, 5);

		Assert.Equal(new[] { tx1.Id, tx2.Id, tx3.Id }, this.checking.Transactions.Select(tx => tx.Id));

		tx1.When = tx2.When + TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.Id, tx1.Id, tx3.Id }, this.checking.Transactions.Select(tx => tx.Id));

		tx1.When = tx3.When + TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.Id, tx3.Id, tx1.Id }, this.checking.Transactions.Select(tx => tx.Id));

		tx1.When += TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.Id, tx3.Id, tx1.Id }, this.checking.Transactions.Select(tx => tx.Id));
	}

	[Fact]
	public void TransactionsSortedOnLoad()
	{
		TransactionViewModel tx1 = this.checking.NewTransaction(volatileOnly: false);
		tx1.When = new DateTime(2021, 1, 3);

		TransactionViewModel tx2 = this.checking.NewTransaction(volatileOnly: false);
		tx2.When = new DateTime(2021, 1, 2);

		// Confirm that a reload does not mess up transaction order.
		Assert.Equal(new[] { tx2.Id, tx1.Id }, this.checking.Transactions.Select(tx => tx.Id));
		this.ReloadViewModel();
		AccountViewModel newChecking = this.DocumentViewModel.AccountsPanel.Accounts.Single(a => a.Id == this.checking.Id);
		Assert.Equal(new[] { tx2.Id, tx1.Id }, newChecking.Transactions.Select(tx => tx.Id));
	}

	[Fact]
	public async Task Balance_Updates()
	{
		Assert.Equal(0, this.checking.Balance);

		TransactionViewModel txViewModel = this.checking.NewTransaction();
		txViewModel.Amount = 10;
		Assert.Equal(10, this.checking.Balance);

		txViewModel.Amount = 8;
		Assert.Equal(8, this.checking.Balance);

		Assert.Same(txViewModel, Assert.Single(this.checking.Transactions));
		this.DocumentViewModel.SelectedTransaction = this.checking.Transactions.Last();
		await TestUtilities.AssertPropertyChangedEventAsync(this.checking, () => this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync(), nameof(this.checking.Balance));
		Assert.Equal(0, this.checking.Balance);
	}

	[Fact]
	public void TransactionBalancesInitializedOnLoad()
	{
		TransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.When = new DateTime(2021, 1, 2);
		tx1.Amount = 5m;

		TransactionViewModel tx2 = this.checking.NewTransaction();
		tx2.When = new DateTime(2021, 1, 3);
		tx2.Amount = 8m;

		this.ReloadViewModel();
		Assert.Equal(5m, this.checking.Transactions[0].Balance);
		Assert.Equal(13m, this.checking.Transactions[1].Balance);
	}

	[Fact]
	public void SettingBalanceDoesNotPersistAccount()
	{
		var account = new Account { Name = "some account" };
		this.Money.Insert(account);
		this.checking = new AccountViewModel(account, this.DocumentViewModel);
		bool eventRaised = false;
		this.Money.EntitiesChanged += (s, e) => eventRaised = true;
		this.checking.Balance = 10;
		Assert.False(eventRaised);
	}

	[Fact]
	public void Balance_ChangesFromTransactionChangeInOtherAccount()
	{
		TransactionViewModel txViewModel = this.checking.NewTransaction();
		this.checking.Add(txViewModel);
		txViewModel.Amount = -10;
		txViewModel.CategoryOrTransfer = this.savings;
		Assert.Equal(-10, this.checking.Balance);

		Assert.Equal(10, this.savings.Balance);

		txViewModel.Amount = -5;
		Assert.Equal(5, this.savings.Balance);
	}

	[Fact]
	public void TransferFromDbAppearsInBothAccounts()
	{
		TransactionViewModel tx1 = this.checking.NewTransaction();
		this.checking.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = this.savings;

		// An counterpart transfer view model should have been added to the savings account.
		TransactionViewModel tx2 = Assert.Single(this.savings.Transactions);
		Assert.Same(this.checking, tx2.CategoryOrTransfer);
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	[Fact]
	public void NewTransferShowsUpInBothAccounts()
	{
		Assert.Empty(this.checking.Transactions);
		Assert.Empty(this.savings.Transactions);

		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
		TransactionViewModel tx1 = this.checking.NewTransaction();
		this.checking.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = this.savings;

		// A counterpart transfer view model should have been added to the savings account.
		TransactionViewModel tx2 = Assert.Single(this.savings.Transactions);
		Assert.Same(this.checking, tx2.CategoryOrTransfer);
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	[Fact]
	public void DeletedTransferIsRemovedFromBothAccounts()
	{
		TransactionViewModel tx1 = this.checking.NewTransaction();
		this.checking.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = this.savings;

		TransactionViewModel tx2 = Assert.Single(this.savings.Transactions);

		this.checking.DeleteTransaction(tx1);
		Assert.Empty(this.savings.Transactions);
	}

	[Fact]
	public void TransferChangedToCategoryIsRemovedFromOtherAccount()
	{
		CategoryViewModel cat = this.DocumentViewModel.CategoriesPanel.NewCategory("Household");

		TransactionViewModel tx1 = this.checking.NewTransaction();
		this.checking.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = this.savings;

		Assert.Single(this.savings.Transactions);
		tx1.CategoryOrTransfer = cat;
		Assert.Empty(this.savings.Transactions);
		Assert.Contains(tx1, this.checking.Transactions);
	}

	[Fact]
	public void TransferPropertyChangesAreReflectedInOtherAccount()
	{
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
		TransactionViewModel tx1 = this.checking.NewTransaction();
		this.checking.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = this.savings;

		TransactionViewModel tx2 = Assert.Single(this.savings.Transactions);
		tx1.Memo = "memo 1";
		Assert.Equal(tx1.Memo, tx2.Memo);
		tx1.Amount = 5;
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	/// <summary>
	/// Verifies that a split transaction where a split line item represents a transfer to another account also appears
	/// in the other account, and that whatever the split item amount is appears and impacts the account balance.
	/// </summary>
	[Fact]
	public void SplitTransferTransactionAppearsInOtherAccount()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		this.AssertNowAndAfterReload(delegate
		{
			TransactionViewModel txSavings = Assert.Single(this.savings.Transactions);
			Assert.Equal(-50, txSavings.Amount);
			Assert.Equal(-50, this.savings.Balance);
			Assert.True(txSavings.IsSynthesizedFromSplit);
		});
	}

	[Fact]
	public void SplitTransferTransactionCannotBeFurtherSplit()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		TransactionViewModel txSavings = Assert.Single(this.savings.Transactions);
		Assert.True(txSavings.IsSynthesizedFromSplit);
		Assert.False(txSavings.ContainsSplits);
		Assert.Throws<InvalidOperationException>(() => txSavings.NewSplit());
	}

	/// <summary>
	/// Verifies that a split transaction where <em>multiple</em> split line items represent a transfer to another account also appears
	/// in the other account (as one transaction), and that whatever the split item amount is appears and impacts the account balance.
	/// </summary>
	[Fact]
	public void MultipleSplitTransfersTransactionAppearsInOtherAccount()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		SplitTransactionViewModel split3 = tx.NewSplit();
		split3.Amount = -5;
		split3.CategoryOrTransfer = this.savings;
		tx.Amount += split3.Amount;
		Assert.Equal(tx.Amount, this.checking.Balance);

		this.AssertNowAndAfterReload(delegate
		{
			TransactionViewModel txSavings = Assert.Single(this.savings.Transactions);
			Assert.Equal(-45, txSavings.Amount);
			Assert.Equal(-45, this.savings.Balance);
			Assert.True(txSavings.IsSynthesizedFromSplit);
		});
	}

	/// <summary>
	/// Verifies that an attempt to delete a transaction that is actually just a split that transfers with another account
	/// cannot be deleted from the view of the other account.
	/// This is important because it would quietly upset the sum of the splits that (presumably) match the total of the overall transaction.
	/// </summary>
	[Fact]
	public void SplitTransferTransactionCannotBeDeletedFromOtherAccount()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.Equal(tx.Amount, this.checking.Balance);

		TransactionViewModel txSavings = Assert.Single(this.savings.Transactions);
		Assert.Throws<InvalidOperationException>(() => this.savings.DeleteTransaction(txSavings));
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.savings;
		this.DocumentViewModel.SelectedTransaction = txSavings;
		Assert.False(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
	}

	[Fact]
	public void SplitTransferTransactionAllowLimitedChangesFromOtherAccount()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		TransactionViewModel txSavings = Assert.Single(this.savings.Transactions);

		// Disallow changes to amount, since that can upset the balance on the overall transaction.
		Assert.Throws<InvalidOperationException>(() => txSavings.Amount += 1);

		// Disallow changes to category, since that is set in the original transaction to this foreign account.
		Assert.Throws<InvalidOperationException>(() => txSavings.CategoryOrTransfer = this.DocumentViewModel.CategoriesPanel.Categories.First());

		// Allow updating the memo field.
		txSavings.Memo = "some memo";
		SplitTransactionViewModel splitTransfer = Assert.Single(tx.Splits, s => s.CategoryOrTransfer == this.savings);
		Assert.Equal(txSavings.Memo, splitTransfer.Memo);

		// Allow updating the cleared flag, independently of the parent transaction.
		txSavings.Cleared = TransactionViewModel.Matched;
		Assert.Equal(TransactionViewModel.NotCleared, tx.Cleared);
	}

	[Fact]
	public void SplitParent_NonSplitChild()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.Null(tx.SplitParent);
	}

	[Fact]
	public void SplitParent_SplitChild()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		this.AssertNowAndAfterReload(delegate
		{
			TransactionViewModel txSavings = Assert.Single(this.savings.Transactions);
			Assert.Same(this.checking.Transactions.Single(t => t.Id == tx.Id), txSavings.SplitParent);
		});
	}

	[Fact]
	public void JumpToSplitParent_OnNonSynthesizedTransaction()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.Throws<InvalidOperationException>(() => tx.JumpToSplitParent());
	}

	[Fact]
	public void JumpToSplitParent_FromTransactionSynthesizedFromSplit()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		TransactionViewModel txSavings = Assert.Single(this.savings.Transactions);
		txSavings.JumpToSplitParent();
		Assert.Same(this.checking, this.DocumentViewModel.BankingPanel.SelectedAccount);
		Assert.Same(tx, this.DocumentViewModel.SelectedTransaction);
	}

	[Fact]
	public void DeletingSplitTransferAlsoRemovesTransactionsFromOtherAccounts()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.NotEmpty(this.savings.Transactions);
		tx.DeleteSplit(tx.Splits.Single(s => s.CategoryOrTransfer is AccountViewModel));

		this.AssertNowAndAfterReload(delegate
		{
			Assert.Empty(this.savings.Transactions);
		});
	}

	[Fact]
	public void DeletingSplitTransactionWithTransfersAlsoRemovesTransactionsFromOtherAccounts()
	{
		TransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.NotEmpty(this.savings.Transactions);
		this.checking.DeleteTransaction(tx);

		this.AssertNowAndAfterReload(delegate
		{
			Assert.Empty(this.savings.Transactions);
		});
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.checking = this.DocumentViewModel.AccountsPanel.Accounts.Single(a => a.Name == "Checking");
		this.savings = this.DocumentViewModel.AccountsPanel.Accounts.Single(a => a.Name == "Savings");
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
	}

	private TransactionViewModel CreateSplitWithCategoryAndTransfer()
	{
		CategoryViewModel cat1 = this.DocumentViewModel.CategoriesPanel.NewCategory("Salary");

		TransactionViewModel tx = this.checking.NewTransaction();
		SplitTransactionViewModel split1 = tx.NewSplit();
		split1.Amount = 100;
		split1.CategoryOrTransfer = cat1;
		SplitTransactionViewModel split2 = tx.NewSplit();
		split2.Amount = 50;
		split2.CategoryOrTransfer = this.savings;
		tx.Amount = split1.Amount + split2.Amount;

		return tx;
	}

	private void AssertNowAndAfterReload(Action assertions)
	{
		assertions();
		this.ReloadViewModel();
		assertions();
	}
}
