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

public class PayeeViewModelTests : TestBase
{
    private PayeeViewModel viewModel = new PayeeViewModel();

    public PayeeViewModelTests(ITestOutputHelper logger)
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
    public void ApplyTo()
    {
        Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null));

        var payee = new Payee();

        this.viewModel.Name = "some name";

        this.viewModel.ApplyTo(payee);

        Assert.Equal(this.viewModel.Name, payee.Name);
    }

    [Fact]
    public void CopyFrom()
    {
        Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null));

        var payee = new Payee();
        payee.Name = "some name";

        this.viewModel.CopyFrom(payee);

        Assert.Equal(payee.Name, this.viewModel.Name);
    }
}
