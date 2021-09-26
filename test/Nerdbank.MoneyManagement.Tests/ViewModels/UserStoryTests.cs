// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Tests that span multiple view models to weave together a real user story.
/// </summary>
public class UserStoryTests : MoneyTestBase
{
	private readonly AccountViewModel checkingAccount;
	private readonly AccountViewModel savingsAccount;

	public UserStoryTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.checkingAccount = this.DocumentViewModel.AccountsPanel.NewAccount("Checking");
		this.savingsAccount = this.DocumentViewModel.AccountsPanel.NewAccount("Savings");
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checkingAccount;
	}

	[Fact]
	public void AddCreditingTransaction()
	{
		TransactionViewModel tx = this.checkingAccount.NewTransaction();
		Assert.True(DateTime.Now - tx.When < TimeSpan.FromMinutes(5));
		tx.Payee = "My boss";
		tx.Amount = 15;

		Transaction? txModel = this.Money.Transactions.FirstOrDefault(t => t.Id == tx.Id);
		Assert.NotNull(txModel);
		Assert.Equal(this.checkingAccount.Id, txModel.CreditAccountId);
		Assert.Null(txModel.DebitAccountId);
	}

	[Fact]
	public void AddTransferTransaction()
	{
		TransactionViewModel tx = this.checkingAccount.NewTransaction();
		Assert.True(DateTime.Now - tx.When < TimeSpan.FromMinutes(5));
		tx.Amount = 15;
		tx.CategoryOrTransfer = this.savingsAccount;

		Transaction? txModel = this.Money.Transactions.FirstOrDefault(t => t.Id == tx.Id);
		Assert.NotNull(txModel);
		Assert.Equal(this.checkingAccount.Id, txModel.CreditAccountId);
		Assert.Equal(this.savingsAccount.Id, txModel.DebitAccountId);

		// Reverse direction of money flow.
		tx.Amount *= -1;

		txModel = this.Money.Transactions.FirstOrDefault(t => t.Id == tx.Id);
		Assert.Equal(this.savingsAccount.Id, txModel.CreditAccountId);
		Assert.Equal(this.checkingAccount.Id, txModel.DebitAccountId);
	}
}
