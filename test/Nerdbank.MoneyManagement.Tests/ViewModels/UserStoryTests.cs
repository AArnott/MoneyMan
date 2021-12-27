// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

/// <summary>
/// Tests that span multiple view models to weave together a real user story.
/// </summary>
public class UserStoryTests : MoneyTestBase
{
	private readonly BankingAccountViewModel checkingAccount;
	private readonly BankingAccountViewModel savingsAccount;

	public UserStoryTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.checkingAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.savingsAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Savings");
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checkingAccount;
	}

	[Fact]
	public void AddCreditingTransaction()
	{
		BankingTransactionViewModel tx = this.checkingAccount.NewTransaction();
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
		BankingTransactionViewModel tx = this.checkingAccount.NewTransaction();
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
