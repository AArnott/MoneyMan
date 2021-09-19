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
	private AccountViewModel viewModel = new AccountViewModel();

	public AccountViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void Name()
	{
		Assert.Null(this.viewModel.Name);
		this.viewModel.Name = "changed";
		Assert.Equal("changed", this.viewModel.Name);
	}

	[Fact]
	public void Name_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Name = "foo",
			nameof(this.viewModel.Name));
	}

	[Fact]
	public void TransferTargetName()
	{
		this.viewModel.Name = "tt-test";
		Assert.Equal($"[{this.viewModel.Name}]", this.viewModel.TransferTargetName);
	}

	[Fact]
	public void TransferTargetName_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(this.viewModel, () => this.viewModel.Name = "other", nameof(this.viewModel.Name), nameof(this.viewModel.TransferTargetName));
	}

	[Fact]
	public void IsClosed()
	{
		Assert.False(this.viewModel.IsClosed);
		this.viewModel.IsClosed = true;
		Assert.True(this.viewModel.IsClosed);
	}

	[Fact]
	public void IsClosed_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.IsClosed = true,
			nameof(this.viewModel.IsClosed));
		TestUtilities.AssertPropertyChangedEvent(
				this.viewModel,
				() => this.viewModel.IsClosed = true);
	}

	[Fact]
	public void ApplyTo_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));
	}

	[Fact]
	public void ApplyTo()
	{
		var account = new Account();

		this.viewModel.Name = "some name";
		this.viewModel.IsClosed = !account.IsClosed;

		this.viewModel.ApplyTo(account);

		Assert.Equal(this.viewModel.Name, account.Name);
		Assert.Equal(this.viewModel.IsClosed, account.IsClosed);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom()
	{
		var account = new Account();
		account.Name = "some name";
		account.IsClosed = !this.viewModel.IsClosed;

		this.viewModel.CopyFrom(account);

		Assert.Equal(account.Name, this.viewModel.Name);
		Assert.Equal(account.IsClosed, this.viewModel.IsClosed);
	}

	[Fact]
	public void Ctor_From_Volatile_Entity()
	{
		var account = new Account
		{
			Id = 5,
			Name = "some person",
		};

		this.viewModel = new AccountViewModel(account, this.Money, this.DocumentViewModel);

		Assert.Equal(account.Id, this.viewModel.Id);
		Assert.Equal(account.Name, this.viewModel.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, account.Name);

		Account fromDb = this.Money.Accounts.First(tx => tx.Id == account.Id);
		Assert.Equal(account.Name, fromDb.Name);
		Assert.Single(this.Money.Accounts);
	}

	[Fact]
	public void Ctor_From_Db_Entity()
	{
		var account = new Account
		{
			Name = "some person",
		};
		this.Money.Insert(account);

		this.viewModel = new AccountViewModel(account, this.Money, this.DocumentViewModel);

		Assert.Equal(account.Id, this.viewModel.Id);
		Assert.Equal(account.Name, this.viewModel.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "some other person";
		Assert.Equal(this.viewModel.Name, account.Name);

		Account fromDb = this.Money.Accounts.First(tx => tx.Id == account.Id);
		Assert.Equal(account.Name, fromDb.Name);
		Assert.Single(this.Money.Accounts);
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
		this.viewModel = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		Assert.Equal(2, this.viewModel.Transactions.Count);
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
		this.viewModel = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		TransactionViewModel txViewModel = Assert.Single(this.viewModel.Transactions);
		this.viewModel.SelectedTransaction = txViewModel;
		txViewModel.IsSelected = true;
		await this.viewModel.DeleteTransactionCommand.ExecuteAsync();
		Assert.Empty(this.Money.Transactions);
	}

	[Fact]
	public void Balance()
	{
		var account = new Account
		{
			Name = "some account",
		};
		this.Money.Insert(account);
		this.viewModel = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		Assert.Equal(0m, this.viewModel.Balance);

		this.Money.InsertAll(new ModelBase[]
		{
			new Transaction { Amount = 10, CreditAccountId = account.Id },
			new Transaction { Amount = 2, DebitAccountId = account.Id },
		});
		this.viewModel = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		Assert.Equal(8m, this.viewModel.Balance);
	}

	[Fact]
	public async Task Balance_Updates()
	{
		var account = new Account
		{
			Name = "some account",
		};
		this.Money.Insert(account);
		var documentViewModel = new DocumentViewModel(this.Money);
		this.viewModel = documentViewModel.AccountsPanel!.Accounts.Single();
		Assert.Equal(0, this.viewModel.Balance);

		TransactionViewModel txViewModel = this.viewModel.NewTransaction();
		this.viewModel.Transactions.Add(txViewModel);
		txViewModel.Amount = 10;
		Assert.Equal(10, this.viewModel.Balance);

		txViewModel.Amount = 8;
		Assert.Equal(8, this.viewModel.Balance);

		Assert.Same(txViewModel, Assert.Single(this.viewModel.Transactions));
		this.viewModel.SelectedTransaction = this.viewModel.Transactions.Last();
		this.viewModel.SelectedTransaction.IsSelected = true;
		await TestUtilities.AssertPropertyChangedEventAsync(this.viewModel, () => this.viewModel.DeleteTransactionCommand.ExecuteAsync(), nameof(this.viewModel.Balance));
		Assert.Equal(0, this.viewModel.Balance);
	}

	[Fact]
	public void SettingBalanceDoesNotPersistAccount()
	{
		var account = new Account { Name = "some account" };
		this.Money.Insert(account);
		this.viewModel = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		bool eventRaised = false;
		this.Money.EntitiesChanged += (s, e) => eventRaised = true;
		this.viewModel.Balance = 10;
		Assert.False(eventRaised);
	}

	[Fact]
	public void Balance_ChangesFromTransactionChangeInOtherAccount()
	{
		(AccountViewModel checkingViewModel, AccountViewModel savingsViewModel) = this.SetupTwoAccounts();

		TransactionViewModel txViewModel = checkingViewModel.NewTransaction();
		this.viewModel.Transactions.Add(txViewModel);
		txViewModel.Amount = -10;
		txViewModel.CategoryOrTransfer = savingsViewModel;
		Assert.Equal(-10, checkingViewModel.Balance);

		Assert.Equal(10, savingsViewModel.Balance);

		txViewModel.Amount = -5;
		Assert.Equal(5, savingsViewModel.Balance);
	}

	[Fact]
	public void TransferFromDbAppearsInBothAccounts()
	{
		(AccountViewModel checkingViewModel, AccountViewModel savingsViewModel) = this.SetupTwoAccounts();

		TransactionViewModel tx1 = checkingViewModel.NewTransaction();
		checkingViewModel.Transactions.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = savingsViewModel;

		// An counterpart transfer view model should have been added to the savings account.
		TransactionViewModel tx2 = Assert.Single(savingsViewModel.Transactions);
		Assert.Same(checkingViewModel, tx2.CategoryOrTransfer);
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	[Fact]
	public void NewTransferShowsUpInBothAccounts()
	{
		// Create two accounts and force them to initialize their transaction collections.
		(AccountViewModel checkingViewModel, AccountViewModel savingsViewModel) = this.SetupTwoAccounts();
		Assert.Empty(checkingViewModel.Transactions);
		Assert.Empty(savingsViewModel.Transactions);

		TransactionViewModel tx1 = checkingViewModel.NewTransaction();
		checkingViewModel.Transactions.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = savingsViewModel;

		// A counterpart transfer view model should have been added to the savings account.
		TransactionViewModel tx2 = Assert.Single(savingsViewModel.Transactions);
		Assert.Same(checkingViewModel, tx2.CategoryOrTransfer);
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	[Fact]
	public void DeletedTransferIsRemovedFromBothAccounts()
	{
		(AccountViewModel checkingViewModel, AccountViewModel savingsViewModel) = this.SetupTwoAccounts();

		TransactionViewModel tx1 = checkingViewModel.NewTransaction();
		checkingViewModel.Transactions.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = savingsViewModel;

		TransactionViewModel tx2 = Assert.Single(savingsViewModel.Transactions);

		checkingViewModel.DeleteTransaction(tx1);
		Assert.Empty(savingsViewModel.Transactions);
	}

	[Fact]
	public void TransferChangedToCategoryIsRemovedFromOtherAccount()
	{
		(AccountViewModel checkingViewModel, AccountViewModel savingsViewModel) = this.SetupTwoAccounts();
		CategoryViewModel cat = this.DocumentViewModel.CategoriesPanel!.NewCategory();
		cat.Name = "Household";

		TransactionViewModel tx1 = checkingViewModel.NewTransaction();
		checkingViewModel.Transactions.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = savingsViewModel;

		Assert.Single(savingsViewModel.Transactions);
		tx1.CategoryOrTransfer = cat;
		Assert.Empty(savingsViewModel.Transactions);
		Assert.Contains(tx1, checkingViewModel.Transactions);
	}

	[Fact]
	public void TransferPropertyChangesAreReflectedInOtherAccount()
	{
		(AccountViewModel checkingViewModel, AccountViewModel savingsViewModel) = this.SetupTwoAccounts();

		TransactionViewModel tx1 = checkingViewModel.NewTransaction();
		checkingViewModel.Transactions.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = savingsViewModel;

		TransactionViewModel tx2 = Assert.Single(savingsViewModel.Transactions);
		tx1.Memo = "memo 1";
		Assert.Equal(tx1.Memo, tx2.Memo);
		tx1.Amount = 5;
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	private (AccountViewModel Checking, AccountViewModel Savings) SetupTwoAccounts()
	{
		AccountViewModel checking = this.DocumentViewModel.NewAccount();
		checking.Name = "Checking";
		AccountViewModel savings = this.DocumentViewModel.NewAccount();
		savings.Name = "Savings";
		return (checking, savings);
	}
}
