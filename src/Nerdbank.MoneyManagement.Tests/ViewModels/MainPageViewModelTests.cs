// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class MainPageViewModelTests : TestBase
{
    private MainPageViewModel viewModel = new MainPageViewModel();

    public MainPageViewModelTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void AccountsPanel_NotNull()
    {
        Assert.NotNull(this.viewModel.AccountsPanel);
    }

    [Fact]
    public void AccountsPanel_RetainsAssignment()
    {
        var newValue = new AccountsPanelViewModel();
        this.viewModel.AccountsPanel = newValue;
        Assert.Same(newValue, this.viewModel.AccountsPanel);
    }

    [Fact]
    public void AccountsPanel_PropertyChanged()
    {
        TestUtilities.AssertPropertyChangedEvent(
            this.viewModel,
            () => this.viewModel.AccountsPanel = new AccountsPanelViewModel(),
            nameof(this.viewModel.AccountsPanel));
        TestUtilities.AssertPropertyChangedEvent(
            this.viewModel,
            () => this.viewModel.AccountsPanel = this.viewModel.AccountsPanel);
    }
}
