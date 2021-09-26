// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	public void AccountsPanel_RetainsAssignment()
	{
		var newValue = new ObservableCollection<AccountViewModel>();
		this.ViewModel.Accounts = newValue;
		Assert.Same(newValue, this.ViewModel.Accounts);
	}

	[Fact]
	public void Accounts_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.ViewModel,
			() => this.ViewModel.Accounts = new ObservableCollection<AccountViewModel>(),
			nameof(this.ViewModel.Accounts));
		TestUtilities.AssertPropertyChangedEvent(
			this.ViewModel,
			() => this.ViewModel.Accounts = this.ViewModel.Accounts);
	}
}
