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
	public void ApplyTo()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));

		var account = new Account();

		this.viewModel.Name = "some name";
		this.viewModel.IsClosed = !account.IsClosed;

		this.viewModel.ApplyTo(account);

		Assert.Equal(this.viewModel.Name, account.Name);
		Assert.Equal(this.viewModel.IsClosed, account.IsClosed);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));

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

		this.viewModel = new AccountViewModel(account, this.Money);

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

		this.viewModel = new AccountViewModel(account, this.Money);

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
		this.Money.InsertAll(new object[]
		{
			new Transaction { CreditAccountId = account.Id },
			new Transaction { DebitAccountId = account.Id },
		});
		this.viewModel = new AccountViewModel(account, this.Money);
		Assert.Equal(2, this.viewModel.Transactions.Count);
	}

	[Fact]
	public void Balance()
	{
		var account = new Account
		{
			Name = "some account",
		};
		this.Money.Insert(account);
		this.viewModel = new AccountViewModel(account, this.Money);
		Assert.Equal(0m, this.viewModel.Balance);

		this.Money.InsertAll(new object[]
		{
			new Transaction { Amount = 10, CreditAccountId = account.Id },
			new Transaction { Amount = 2, DebitAccountId = account.Id },
		});
		this.viewModel = new AccountViewModel(account, this.Money);
		Assert.Equal(8m, this.viewModel.Balance);
	}
}
