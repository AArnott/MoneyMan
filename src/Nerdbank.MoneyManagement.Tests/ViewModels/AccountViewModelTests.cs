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

public class AccountViewModelTests : TestBase
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
        Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null));

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
        Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null));

        var account = new Account();
        account.Name = "some name";
        account.IsClosed = !this.viewModel.IsClosed;

        this.viewModel.CopyFrom(account);

        Assert.Equal(account.Name, this.viewModel.Name);
        Assert.Equal(account.IsClosed, this.viewModel.IsClosed);
    }
}
