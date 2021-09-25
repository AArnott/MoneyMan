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
		this.checking = this.DocumentViewModel.NewAccount("Checking");
		this.savings = this.DocumentViewModel.NewAccount("Savings");
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
		AccountViewModel newAccount = this.DocumentViewModel.NewAccount();
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

		var alternate = new AccountViewModel(account, this.Money, this.DocumentViewModel);

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
		this.checking = new AccountViewModel(account, this.Money, this.DocumentViewModel);
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
		this.checking = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		TransactionViewModel txViewModel = Assert.Single(this.checking.Transactions);
		this.DocumentViewModel.SelectedTransaction = txViewModel;
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Assert.Empty(this.Money.Transactions);
	}

	[Fact]
	public async Task DeleteTransactions()
	{
		var account = new Account { Name = "some account" };
		this.Money.Insert(account);
		this.Money.Insert(new Transaction { CreditAccountId = account.Id, Amount = 5 });
		this.Money.Insert(new Transaction { CreditAccountId = account.Id, Amount = 12 });
		this.Money.Insert(new Transaction { CreditAccountId = account.Id, Amount = 15 });
		this.checking = new AccountViewModel(account, this.Money, this.DocumentViewModel);
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
		this.checking = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		Assert.Equal(0m, this.checking.Balance);

		this.Money.InsertAll(new ModelBase[]
		{
			new Transaction { Amount = 10, CreditAccountId = account.Id },
			new Transaction { Amount = 2, DebitAccountId = account.Id },
		});
		this.checking = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		Assert.Equal(8m, this.checking.Balance);
	}

	[Fact]
	public async Task Balance_Updates()
	{
		Assert.Equal(0, this.checking.Balance);

		TransactionViewModel txViewModel = this.DocumentViewModel.NewTransaction();
		this.checking.Transactions.Add(txViewModel);
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
	public void SettingBalanceDoesNotPersistAccount()
	{
		var account = new Account { Name = "some account" };
		this.Money.Insert(account);
		this.checking = new AccountViewModel(account, this.Money, this.DocumentViewModel);
		bool eventRaised = false;
		this.Money.EntitiesChanged += (s, e) => eventRaised = true;
		this.checking.Balance = 10;
		Assert.False(eventRaised);
	}

	[Fact]
	public void Balance_ChangesFromTransactionChangeInOtherAccount()
	{
		TransactionViewModel txViewModel = this.DocumentViewModel.NewTransaction();
		this.checking.Transactions.Add(txViewModel);
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
		TransactionViewModel tx1 = this.DocumentViewModel.NewTransaction();
		this.checking.Transactions.Add(tx1);
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
		TransactionViewModel tx1 = this.DocumentViewModel.NewTransaction();
		this.checking.Transactions.Add(tx1);
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
		TransactionViewModel tx1 = this.DocumentViewModel.NewTransaction();
		this.checking.Transactions.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = this.savings;

		TransactionViewModel tx2 = Assert.Single(this.savings.Transactions);

		this.DocumentViewModel.DeleteTransaction(tx1);
		Assert.Empty(this.savings.Transactions);
	}

	[Fact]
	public void TransferChangedToCategoryIsRemovedFromOtherAccount()
	{
		CategoryViewModel cat = this.DocumentViewModel.NewCategory("Household");

		TransactionViewModel tx1 = this.DocumentViewModel.NewTransaction();
		this.checking.Transactions.Add(tx1);
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
		TransactionViewModel tx1 = this.DocumentViewModel.NewTransaction();
		this.checking.Transactions.Add(tx1);
		tx1.Amount = -10;
		tx1.CategoryOrTransfer = this.savings;

		TransactionViewModel tx2 = Assert.Single(this.savings.Transactions);
		tx1.Memo = "memo 1";
		Assert.Equal(tx1.Memo, tx2.Memo);
		tx1.Amount = 5;
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}
}
