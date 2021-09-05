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
	private TransactionViewModel viewModel = new TransactionViewModel();

	private string payee = "some person";

	private decimal amount = 5.5m;

	private string memo = "Some memo";

	private DateTime when = DateTime.Now - TimeSpan.FromDays(3);

	private int? checkNumber = 15;

	private TransactionViewModel.ClearedStateViewModel cleared = TransactionViewModel.SharedClearedStates[0];

	public TransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
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
	public void ApplyTo()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));

		Transaction transaction = new Transaction();

		this.viewModel.Payee = this.payee;
		this.viewModel.Amount = this.amount;
		this.viewModel.When = this.when;
		this.viewModel.Memo = this.memo;
		this.viewModel.CheckNumber = this.checkNumber;
		this.viewModel.Cleared = this.cleared;
		this.viewModel.ApplyTo(transaction);

		Assert.Equal(this.payee, transaction.Payee);
		Assert.Equal(this.amount, transaction.Amount);
		Assert.Equal(this.when, transaction.When);
		Assert.Equal(this.memo, transaction.Memo);
		Assert.Equal(this.checkNumber, transaction.CheckNumber);
		Assert.Equal(this.cleared.Value, transaction.Cleared);

		// Test auto-save behavior.
		this.viewModel.Memo = "bonus";
		Assert.Equal(this.viewModel.Memo, transaction.Memo);
	}

	[Fact]
	public void ApplyToThrowsOnEntityMismatch()
	{
		this.viewModel.CopyFrom(new Transaction { Id = 2, Memo = "Groceries" });
		Assert.Throws<ArgumentException>(() => this.viewModel.ApplyTo(new Transaction { Id = 4 }));
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));

		Transaction transaction = new Transaction
		{
			Payee = this.payee,
			Amount = this.amount,
			When = this.when,
			Memo = this.memo,
			CheckNumber = this.checkNumber,
			Cleared = this.cleared.Value,
		};

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(transaction.Payee, this.viewModel.Payee);
		Assert.Equal(transaction.Amount, this.viewModel.Amount);
		Assert.Equal(transaction.When, this.viewModel.When);
		Assert.Equal(transaction.Memo, this.viewModel.Memo);
		Assert.Equal(transaction.CheckNumber, this.viewModel.CheckNumber);
		Assert.Equal(transaction.Cleared, this.viewModel.Cleared.Value);

		// Test auto-save behavior.
		this.viewModel.Memo = "another memo";
		Assert.Equal(this.viewModel.Memo, transaction.Memo);
	}

	[Fact]
	public void Ctor_From_Volatile_Entity()
	{
		var transaction = new Transaction
		{
			Id = 5,
			Payee = "some person",
		};

		this.viewModel = new TransactionViewModel(transaction, this.Money);

		Assert.Equal(transaction.Id, this.viewModel.Id);
		Assert.Equal(transaction.Payee, this.viewModel.Payee);

		// Test auto-save behavior.
		this.viewModel.Payee = "another name";
		Assert.Equal(this.viewModel.Payee, transaction.Payee);

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

		this.viewModel = new TransactionViewModel(transaction, this.Money);

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
}
