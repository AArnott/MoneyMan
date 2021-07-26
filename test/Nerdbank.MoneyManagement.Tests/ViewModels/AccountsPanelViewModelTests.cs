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

public class AccountsPanelViewModelTests : TestBase
{
	private AccountsPanelViewModel viewModel = new AccountsPanelViewModel();

	public AccountsPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void Accounts_NotNull()
	{
		Assert.NotNull(this.viewModel.Accounts);
	}

	[Fact]
	public void AccountsPanel_RetainsAssignment()
	{
		var newValue = new ObservableCollection<AccountViewModel>();
		this.viewModel.Accounts = newValue;
		Assert.Same(newValue, this.viewModel.Accounts);
	}

	[Fact]
	public void Accounts_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Accounts = new ObservableCollection<AccountViewModel>(),
			nameof(this.viewModel.Accounts));
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Accounts = this.viewModel.Accounts);
	}
}
