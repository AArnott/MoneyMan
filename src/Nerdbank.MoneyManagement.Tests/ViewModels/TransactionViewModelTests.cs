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

public class TransactionViewModelTests : TestBase
{
    private TransactionViewModel viewModel = new TransactionViewModel();

    private decimal amount = 5.5m;

    private DateTime when = DateTime.Now - TimeSpan.FromDays(3);

    public TransactionViewModelTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void When()
    {
        TestUtilities.AssertPropertyChangedEvent(
            this.viewModel,
            () => this.viewModel.When = this.when,
            nameof(this.viewModel.When));
        Assert.Equal(this.when, this.viewModel.When);
    }

    [Fact]
    public void Amount()
    {
        TestUtilities.AssertPropertyChangedEvent(
            this.viewModel,
            () => this.viewModel.Amount = this.amount,
            nameof(this.viewModel.Amount));
        Assert.Equal(this.amount, this.viewModel.Amount);
    }

    [Fact]
    public void Payee()
    {
        var payeeViewModel = new PayeeViewModel();
        TestUtilities.AssertPropertyChangedEvent(
            this.viewModel,
            () => this.viewModel.Payee = payeeViewModel,
            nameof(this.viewModel.Payee));
        Assert.Same(payeeViewModel, this.viewModel.Payee);
    }

    [Fact]
    public void ApplyTo()
    {
        Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null));

        Transaction transaction = new Transaction();

        this.viewModel.Amount = this.amount;
        this.viewModel.When = this.when;
        this.viewModel.ApplyTo(transaction);

        Assert.Equal(this.amount, transaction.Amount);
        Assert.Equal(this.when, transaction.When);
    }

    [Fact]
    public void CopyFrom()
    {
        Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null));

        Transaction transaction = new Transaction
        {
            Amount = this.amount,
            When = this.when,
        };

        this.viewModel.CopyFrom(transaction);

        Assert.Equal(transaction.Amount, this.viewModel.Amount);
        Assert.Equal(transaction.When, this.viewModel.When);
    }
}
