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

		TransactionEntry? txModel = this.Money.TransactionEntries.SingleOrDefault(t => t.TransactionId == tx.TransactionId);
		Assert.NotNull(txModel);
		Assert.Equal(this.checkingAccount.Id, txModel!.AccountId);
		Assert.Equal(15, txModel.Amount);
	}

	[Fact]
	public void AddTransferTransaction()
	{
		BankingTransactionViewModel tx = this.checkingAccount.NewTransaction();
		Assert.True(DateTime.Now - tx.When < TimeSpan.FromMinutes(5));
		tx.Amount = 15;
		tx.OtherAccount = this.savingsAccount;

		var txModel = this.Money.TransactionEntries.Where(t => t.TransactionId == tx.TransactionId);
		Assert.Single(txModel, te => te.AccountId == this.checkingAccount.Id && te.Amount == tx.Amount);
		Assert.Single(txModel, te => te.AccountId == this.savingsAccount.Id && te.Amount == -tx.Amount);

		// Reverse direction of money flow.
		tx.Amount *= -1;

		Assert.Single(txModel, te => te.AccountId == this.checkingAccount.Id && te.Amount == -tx.Amount);
		Assert.Single(txModel, te => te.AccountId == this.savingsAccount.Id && te.Amount == tx.Amount);
	}
}
