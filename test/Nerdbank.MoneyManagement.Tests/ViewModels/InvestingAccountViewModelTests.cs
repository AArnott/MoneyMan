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
		this.Money.Action.Add(this.brokerage.Model, new Amount(1, this.brokerage.CurrencyAsset!.Id));
		this.Money.Action.Remove(this.brokerage.Model, new Amount(1, this.brokerage.CurrencyAsset!.Id));
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
		checkingTx.OtherAccount = this.brokerage;
		Assert.Equal(2, this.brokerage.Transactions.Count);
		InvestingTransactionViewModel investingTx = this.brokerage.Transactions[0];
		Assert.Equal(TransactionAction.Transfer, investingTx.Action);
		Assert.Equal(this.checking, investingTx.WithdrawAccount);
		Assert.Equal(this.brokerage, investingTx.DepositAccount);
	}

	[Fact]
	public void DepositMadeFromInvestingContext()
	{
		InvestingTransactionViewModel investingTx = this.brokerage.Transactions[^1];
		investingTx.Action = TransactionAction.Transfer;
		investingTx.WithdrawAccount = this.checking;
		investingTx.WithdrawAmount = 10;
		investingTx.WithdrawAsset = this.checking.CurrencyAsset;
		investingTx.DepositAmount = 10;
		investingTx.DepositAccount = this.brokerage;
		investingTx.DepositAsset = this.checking.CurrencyAsset;

		Assert.Equal(2, this.checking.Transactions.Count);
		BankingTransactionViewModel checkingTx = this.checking.Transactions[0];
		checkingTx.When = this.when;
		checkingTx.Amount = -10;
		checkingTx.OtherAccount = this.brokerage;
	}

	[Fact]
	public void Value()
	{
		Assert.Equal(0m, this.brokerage.Value);

		this.Money.Action.Deposit(this.brokerage.Model, 10);
		this.Money.Action.Withdraw(this.brokerage.Model, 2);
		this.AssertNowAndAfterReload(delegate
		{
			Assert.Equal(8m, this.brokerage.Value);
		});
	}

	[Fact]
	public void Value_UpdatesWithAssetPriceUpdates()
	{
		AssetViewModel msft = this.DocumentViewModel.AssetsPanel.NewAsset("MSFT");

		DateTime when = DateTime.Today.AddDays(-1);
		this.Money.Action.Deposit(this.brokerage.Model, new Amount(2, msft.Id), when);
		this.Money.Action.AssetPrice(msft.Model, when, 10);
		Assert.Equal(20, this.brokerage.Value);
		this.Money.Action.AssetPrice(msft.Model, when.AddDays(1), 12);
		Assert.Equal(24, this.brokerage.Value);
	}

	[Fact]
	public async Task DeleteTransaction()
	{
		this.Money.Action.Deposit(this.brokerage.Model, 5);
		InvestingTransactionViewModel txViewModel = this.brokerage.Transactions[0];
		this.DocumentViewModel.SelectedTransaction = txViewModel;
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Assert.Empty(this.Money.Transactions);
	}

	[Fact]
	public void DeleteVolatileTransaction()
	{
		InvestingTransactionViewModel tx = this.brokerage.Transactions[^1];
		tx.DepositAmount = 5;
		this.brokerage.DeleteTransaction(tx);
		Assert.NotEmpty(this.brokerage.Transactions);
		tx = this.brokerage.Transactions[^1];
		Assert.Null(tx.DepositAmount);
	}

	[Fact]
	public async Task DeleteTransactions()
	{
		this.Money.Action.Deposit(this.brokerage.Model, 5);
		this.Money.Action.Deposit(this.brokerage.Model, 12);
		this.Money.Action.Deposit(this.brokerage.Model, 15);
		Assert.False(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
		this.DocumentViewModel.SelectedTransactions = this.brokerage.Transactions.Where(t => t.DepositAmount != 12).ToArray();
		Assert.True(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Transaction remainingTransaction = Assert.Single(this.Money.Transactions);
		TransactionEntry remainingTransactionEntry = Assert.Single(this.Money.TransactionEntries);
		Assert.Equal(remainingTransaction.Id, remainingTransactionEntry.TransactionId);
		Assert.Equal(12, remainingTransactionEntry.Amount);
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
