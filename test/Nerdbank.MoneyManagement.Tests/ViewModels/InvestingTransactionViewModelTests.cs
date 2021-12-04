// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class InvestingTransactionViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel account;
	private InvestingTransactionViewModel viewModel;
	private DateTime when = DateTime.Now - TimeSpan.FromDays(3);

	public InvestingTransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		Account thisAccountModel = this.Money.Insert(new Account { Name = "this", Type = Account.AccountType.Investing });
		this.account = (InvestingAccountViewModel)this.DocumentViewModel.GetAccount(thisAccountModel.Id);
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.viewModel = this.account.Transactions[^1];
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
	public void Action()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Action = InvestmentAction.Withdraw,
			nameof(this.viewModel.Action));
		Assert.Equal(InvestmentAction.Withdraw, this.viewModel.Action);
	}

	[Fact]
	public void ApplyTo_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));
	}

	[Fact]
	public void ApplyTo()
	{
		InvestingTransaction transaction = new();
		InvestingTransactionViewModel viewModel = new(this.account, null);

		viewModel.When = this.when;
		viewModel.Action = InvestmentAction.Sell;
		viewModel.ApplyTo(transaction);

		Assert.Equal(this.when, transaction.When);
		Assert.Equal(InvestmentAction.Sell, transaction.Action);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom()
	{
		InvestingTransaction transaction = this.viewModel.Model!;
		transaction.When = this.when;
		transaction.Action = InvestmentAction.Sell;

		this.viewModel.CopyFrom(transaction);

		Assert.Equal(this.when, this.viewModel.When);
		Assert.Equal(InvestmentAction.Sell, this.viewModel.Action);
	}
}
