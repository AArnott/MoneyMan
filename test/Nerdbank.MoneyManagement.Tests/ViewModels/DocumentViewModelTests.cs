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
	private DocumentViewModel viewModel = new DocumentViewModel(null);

	public DocumentViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void InitialState()
	{
		Assert.False(this.viewModel.IsFileOpen);
		Assert.Null(this.viewModel.AccountsPanel);
		Assert.Null(this.viewModel.CategoriesPanel);
	}

	[Fact]
	public void LoadFromFile()
	{
		this.Money.InsertAll(new ModelBase[]
		{
			new Account { Name = "Checking" },
			new Category { Name = "Cat1" },
		});
		this.viewModel = new DocumentViewModel(this.Money);
		Assert.Contains(this.viewModel.AccountsPanel?.Accounts, acct => acct.Name == "Checking");
		Assert.Contains(this.viewModel.CategoriesPanel?.Categories, cat => cat.Name == "Cat1");
	}

	[Fact]
	public void NewFileGetsDefaultCategories()
	{
		this.viewModel = DocumentViewModel.CreateNew(MoneyFile.Load(":memory:"));
		Assert.Contains(this.viewModel.CategoriesPanel!.Categories, cat => cat.Name == "Groceries");
	}

	[Fact]
	public void NetWorth()
	{
		Account account = new() { Name = "Checking" };
		this.Money.Insert(account);
		Transaction tx1 = new() { When = DateTime.Now, CreditAccountId = account.Id, Amount = 10 };
		this.Money.Insert(tx1);
		this.viewModel = new DocumentViewModel(this.Money);
		Assert.Equal(10, this.viewModel.NetWorth);

		Transaction tx2 = new() { When = DateTime.Now, DebitAccountId = account.Id, Amount = 3 };
		TestUtilities.AssertPropertyChangedEvent(this.viewModel, () => this.Money.Insert(tx2), nameof(this.viewModel.NetWorth));
		Assert.Equal(7, this.viewModel.NetWorth);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.viewModel.Dispose();
		}

		base.Dispose(disposing);
	}
}
