// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class InvestingAccountViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel brokerage;

	public InvestingAccountViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.brokerage = this.DocumentViewModel.AccountsPanel.NewInvestingAccount("Brokerage");
	}

	[Fact]
	public void Name()
	{
		Assert.Equal("Brokerage", this.brokerage.Name);
	}

	[Fact]
	public void NoTransactions()
	{
		InvestingTransactionViewModel volatileTransaction = Assert.Single(this.brokerage.Transactions);
	}
}
