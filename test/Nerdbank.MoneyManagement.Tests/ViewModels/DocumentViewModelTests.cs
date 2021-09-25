// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class DocumentViewModelTests : MoneyTestBase
{
	public DocumentViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void InitialState()
	{
		DocumentViewModel documentViewModel = new DocumentViewModel();
		Assert.False(documentViewModel.IsFileOpen);
	}

	[Fact]
	public void LoadFromFile()
	{
		this.Money.InsertAll(new ModelBase[]
		{
			new Account { Name = "Checking" },
			new Category { Name = "Cat1" },
		});
		DocumentViewModel documentViewModel = new(this.Money);
		Assert.Contains(documentViewModel.BankingPanel?.Accounts, acct => acct.Name == "Checking");
		Assert.Contains(documentViewModel.CategoriesPanel?.Categories, cat => cat.Name == "Cat1");
	}

	[Fact]
	public void NewFileGetsDefaultCategories()
	{
		DocumentViewModel documentViewModel = DocumentViewModel.CreateNew(MoneyFile.Load(":memory:"));
		Assert.Contains(documentViewModel.CategoriesPanel!.Categories, cat => cat.Name == "Groceries");
	}

	[Fact]
	public void NetWorth()
	{
		Account account = new() { Name = "Checking" };
		this.Money.Insert(account);
		Transaction tx1 = new() { When = DateTime.Now, CreditAccountId = account.Id, Amount = 10 };
		this.Money.Insert(tx1);
		Assert.Equal(10, this.DocumentViewModel.NetWorth);

		Transaction tx2 = new() { When = DateTime.Now, DebitAccountId = account.Id, Amount = 3 };
		TestUtilities.AssertPropertyChangedEvent(this.DocumentViewModel, () => this.Money.Insert(tx2), nameof(this.DocumentViewModel.NetWorth));
		Assert.Equal(7, this.DocumentViewModel.NetWorth);
	}

	[Fact]
	public void NewAccount()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.NewAccount();
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);
	}

	[Fact]
	public void AddedAccountAddsToTransactionTargets()
	{
		Assert.Empty(this.DocumentViewModel.TransactionTargets);
		AccountViewModel accountViewModel = this.DocumentViewModel.NewAccount();
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);

		ITransactionTarget accountTarget = Assert.Single(this.DocumentViewModel.TransactionTargets);
		Assert.Same(accountViewModel, accountTarget);
	}

	[Fact]
	public void DeletedAccountRemovesFromTransactionTargets()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.NewAccount();
		accountViewModel.Name = "some new account";

		Assert.Single(this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.DeleteAccount(accountViewModel);
		Assert.Empty(this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void AddedCategoryAddsToTransactionTargets()
	{
		Assert.Empty(this.DocumentViewModel.TransactionTargets);
		CategoryViewModel categoryViewModel = this.DocumentViewModel.NewCategory("some new category");
		Category category = Assert.Single(this.Money.Categories);
		Assert.Equal(categoryViewModel.Name, category.Name);

		ITransactionTarget categoryTarget = Assert.Single(this.DocumentViewModel.TransactionTargets);
		Assert.Equal(categoryViewModel, categoryTarget);
	}

	[Fact]
	public void DeletedCategoryRemovesFromTransactionTargets()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.NewCategory("some new category");

		Assert.Single(this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.DeleteCategory(categoryViewModel);
		Assert.Empty(this.DocumentViewModel.TransactionTargets);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.DocumentViewModel.Dispose();
		}

		base.Dispose(disposing);
	}
}
