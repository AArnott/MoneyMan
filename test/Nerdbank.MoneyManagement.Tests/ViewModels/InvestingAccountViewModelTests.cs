// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class InvestingAccountViewModelTests : MoneyTestBase
{
	private readonly DateTime when = DateTime.Now.AddDays(-1);
	private BankingAccountViewModel checking;
	private InvestingAccountViewModel brokerage;

	public InvestingAccountViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
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

	[Fact]
	public void FillingOutPlaceholderTransactionGeneratesAnother()
	{
		InvestingTransactionViewModel tx = this.brokerage.Transactions[^1];
		tx.When = DateTime.Now;
		tx.Action = TransactionAction.Buy;
		Assert.Equal(2, this.brokerage.Transactions.Count);
	}

	[Fact]
	public void TransactionsPopulatedFromDb()
	{
		this.Money.InsertAll(new ModelBase[]
		{
			new Transaction { Action = TransactionAction.Add, CreditAccountId = this.brokerage.Id },
			new Transaction { Action = TransactionAction.Remove, DebitAccountId = this.brokerage.Id },
		});
		this.brokerage = new InvestingAccountViewModel(this.brokerage.Model, this.DocumentViewModel);
		Assert.Equal(2, this.brokerage.Transactions.Count(t => t.IsPersisted));
	}

	[Fact]
	public void TransactionsWrittenToDb()
	{
		InvestingTransactionViewModel tx = this.brokerage.Transactions[^1];
		tx.When = DateTime.Now;
		tx.Action = TransactionAction.Interest;
		this.ReloadViewModel();
		Assert.Equal(2, this.brokerage.Transactions.Count);
	}

	[Fact]
	public void DepositMadeFromBankingContext()
	{
		BankingTransactionViewModel checkingTx = this.checking.Transactions[^1];
		checkingTx.When = this.when;
		checkingTx.Amount = -10;
		checkingTx.CategoryOrTransfer = this.brokerage;
		Assert.Equal(2, this.brokerage.Transactions.Count);
		InvestingTransactionViewModel investingTx = this.brokerage.Transactions[0];
		Assert.Equal(TransactionAction.Transfer, investingTx.Action);
		Assert.Equal(this.checking, investingTx.DebitAccount);
		Assert.Equal(this.brokerage, investingTx.CreditAccount);
	}

	[Fact]
	public void DepositMadeFromInvestingContext()
	{
		InvestingTransactionViewModel investingTx = this.brokerage.Transactions[^1];
		investingTx.Action = TransactionAction.Transfer;
		investingTx.DebitAccount = this.checking;
		investingTx.DebitAmount = 10;
		investingTx.DebitAsset = this.checking.CurrencyAsset;
		investingTx.CreditAmount = 10;
		investingTx.CreditAccount = this.brokerage;
		investingTx.CreditAsset = this.checking.CurrencyAsset;

		Assert.Equal(2, this.checking.Transactions.Count);
		BankingTransactionViewModel checkingTx = this.checking.Transactions[0];
		checkingTx.When = this.when;
		checkingTx.Amount = -10;
		checkingTx.CategoryOrTransfer = this.brokerage;
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.RefetchViewModels();
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.brokerage;
	}

	private void RefetchViewModels()
	{
		this.brokerage = (InvestingAccountViewModel)this.DocumentViewModel.BankingPanel.Accounts.Single(a => a.Name == this.brokerage.Name);
	}
}
