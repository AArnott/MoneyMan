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

		this.DocumentViewModel.AccountsPanel.Accounts.Single().IsClosed = true;
		Assert.Equal(0, this.DocumentViewModel.NetWorth);
	}

	[Fact]
	public void NewAccount()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount();
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);
	}

	[Fact]
	public void AddedAccountAddsToTransactionTargets()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount();
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void DeletedAccountRemovesFromTransactionTargets()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount();
		accountViewModel.Name = "some new account";
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.AccountsPanel.DeleteAccount(accountViewModel);
		Assert.DoesNotContain(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void AddedCategoryAddsToTransactionTargets()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("some new category");
		Category category = Assert.Single(this.Money.Categories);
		Assert.Equal(categoryViewModel.Name, category.Name);
		Assert.Contains(categoryViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void DeletedCategoryRemovesFromTransactionTargets()
	{
		CategoryViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("some new category");
		Assert.Contains(categoryViewModel, this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.CategoriesPanel.DeleteCategory(categoryViewModel);
		Assert.DoesNotContain(categoryViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void TransactionTargets_DoesNotIncludeVolatileAccounts()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount();
		Assert.DoesNotContain(accountViewModel, this.DocumentViewModel.TransactionTargets);
		accountViewModel.Name = "Checking";
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	/// <summary>
	/// Verifies that transaction targets includes closed accounts.
	/// This is important because editing old transactions must be able to show that it possibly transferred to/from an account that is now closed.
	/// </summary>
	[Fact]
	public void TransactionTargetsIncludesClosedAccounts()
	{
		AccountViewModel closed = this.DocumentViewModel.AccountsPanel.NewAccount("ToBeClosed");
		Assert.Contains(closed, this.DocumentViewModel.TransactionTargets);
		closed.IsClosed = true;
		Assert.Contains(closed, this.DocumentViewModel.TransactionTargets);
		this.ReloadViewModel();
		Assert.Contains(this.DocumentViewModel.TransactionTargets, tt => tt.Name == closed.Name);
	}

	[Fact]
	public void TransactionTargets_IncludesSplitSingleton()
	{
		Assert.Contains(SplitCategoryPlaceholder.Singleton, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void TransactionTargets_IsSorted()
	{
		AccountViewModel accountG = this.DocumentViewModel.AccountsPanel.NewAccount("g");
		AccountViewModel accountA = this.DocumentViewModel.AccountsPanel.NewAccount("a");
		CategoryViewModel categoryA = this.DocumentViewModel.CategoriesPanel.NewCategory("a");
		CategoryViewModel categoryG = this.DocumentViewModel.CategoriesPanel.NewCategory("g");
		Assert.Equal<ITransactionTarget>(
			new ITransactionTarget[] { categoryA, categoryG, SplitCategoryPlaceholder.Singleton, accountA, accountG },
			this.DocumentViewModel.TransactionTargets);
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
