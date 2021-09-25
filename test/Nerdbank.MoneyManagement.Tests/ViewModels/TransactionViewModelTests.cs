// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

	private TransactionViewModel.ClearedStateViewModel cleared = TransactionViewModel.SharedClearedStates[0];

	public TransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		Account thisAccountModel = this.Money.Insert(new Account { Name = "this" });
		Account otherAccountModel = this.Money.Insert(new Account { Name = "other" });

		this.account = this.DocumentViewModel.GetAccount(thisAccountModel.Id);
		this.otherAccount = this.DocumentViewModel.GetAccount(otherAccountModel.Id);
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.viewModel = this.DocumentViewModel.NewTransaction();
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
		this.viewModel.Cleared = TransactionViewModel.SharedClearedStates[1];
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
		TransactionViewModel viewModel = new(this.account, null, this.Money);

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
		Assert.Equal(this.cleared.Value, transaction.Cleared);

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
		transaction.Cleared = this.cleared.Value;
		transaction.CategoryId = categoryViewModel.Id;
		transaction.DebitAccountId = this.account.Id;

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(transaction.Payee, this.viewModel.Payee);
		Assert.Equal(-transaction.Amount, this.viewModel.Amount);
		Assert.Equal(transaction.When, this.viewModel.When);
		Assert.Equal(transaction.Memo, this.viewModel.Memo);
		Assert.Equal(transaction.CheckNumber, this.viewModel.CheckNumber);
		Assert.Equal(transaction.Cleared, this.viewModel.Cleared.Value);
		Assert.Equal(categoryViewModel.Id, Assert.IsType<CategoryViewModel>(this.viewModel.CategoryOrTransfer).Id);

		// Test auto-save behavior.
		this.viewModel.Memo = "another memo";
		Assert.Equal(this.viewModel.Memo, transaction.Memo);
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
	public void Ctor_From_Volatile_Entity()
	{
		var transaction = new Transaction
		{
			Payee = "some person",
		};

		this.viewModel = new TransactionViewModel(this.account, transaction, this.Money);

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

		this.viewModel = new TransactionViewModel(this.account, transaction, this.Money);

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

		this.viewModel = new TransactionViewModel(this.account, transaction, this.Money);
		this.Money.Dispose();
		this.viewModel.Amount = 12;
	}
}
