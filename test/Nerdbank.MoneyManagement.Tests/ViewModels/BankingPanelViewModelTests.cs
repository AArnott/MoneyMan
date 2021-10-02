// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class BankingPanelViewModelTests : MoneyTestBase
{
	public BankingPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	private BankingPanelViewModel ViewModel => this.DocumentViewModel.BankingPanel;

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.ViewModel.Accounts);
		Assert.Null(this.ViewModel.SelectedAccount);
	}

	[Fact]
	public void Accounts_RemovesAccountWhenClosed()
	{
		AccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewAccount("Checking");
		Assert.Single(this.ViewModel.Accounts);
		checking.IsClosed = true;
		Assert.Empty(this.ViewModel.Accounts);
		checking.IsClosed = false;
		Assert.Single(this.ViewModel.Accounts);
	}

	[Fact]
	public void Accounts_ExcludesClosedAccounts()
	{
		this.Money.Insert(new Account { Name = "Checking", IsClosed = true });
		this.ReloadViewModel();
		Assert.Empty(this.ViewModel.Accounts);
		this.DocumentViewModel.AccountsPanel.Accounts.Single().IsClosed = false;
		Assert.Single(this.ViewModel.Accounts);
	}

	[Fact]
	public void Accounts_NewAccountAlreadyClosed()
	{
		AccountViewModel newAccount = this.DocumentViewModel.AccountsPanel.NewAccount();
		newAccount.IsClosed = true; // Set this before the name, since setting the name formally adds it to the db and the Banking panel.
		newAccount.Name = "Checking";
		Assert.Empty(this.ViewModel.Accounts);
	}
}
