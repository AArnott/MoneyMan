// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class TransactionEntryViewModelTests : MoneyTestBase
{
	private BankingAccountViewModel checkingAccount;
	private InvestingAccountViewModel brokerageAccount;
	private CategoryAccountViewModel spendingCategory;
	private BankingTransactionViewModel bankingTransaction;
	private TransactionEntryViewModel bankingViewModel;
	private AssetViewModel msft;

	private decimal amount = 5.5m;
	private string ofxFitId = "someFitId";
	private string memo = "Some memo";

	public TransactionEntryViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.spendingCategory = this.DocumentViewModel.CategoriesPanel.NewCategory("Spending");
		this.msft = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft", "MSFT");

		this.checkingAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.bankingTransaction = this.checkingAccount.NewTransaction();
		this.bankingViewModel = this.bankingTransaction.NewSplit();

		this.brokerageAccount = this.DocumentViewModel.AccountsPanel.NewInvestingAccount("Brokerage");

		this.EnableSqlLogging();
	}

	[Fact]
	public void OfxFitId()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.bankingViewModel,
			() => this.bankingViewModel.OfxFitId = this.ofxFitId,
			nameof(this.bankingViewModel.OfxFitId));
		Assert.Equal(this.ofxFitId, this.bankingViewModel.OfxFitId);
	}

	[Fact]
	public void Amount()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.bankingViewModel,
			() => this.bankingViewModel.Amount = this.amount,
			nameof(this.bankingViewModel.Amount));
		Assert.Equal(this.amount, this.bankingViewModel.Amount);
	}

	[Fact]
	public void Category()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.bankingViewModel,
			() => this.bankingViewModel.Account = this.spendingCategory,
			nameof(this.bankingViewModel.Account));
		Assert.Equal(this.spendingCategory, this.bankingViewModel.Account);
	}

	[Fact]
	public void AvailableTransactionTargets()
	{
		Assert.DoesNotContain(this.bankingViewModel.AvailableTransactionTargets, tt => tt == this.DocumentViewModel.SplitCategory);
		Assert.DoesNotContain(this.bankingViewModel.AvailableTransactionTargets, tt => tt == this.bankingViewModel.ThisAccount);
		Assert.NotEmpty(this.bankingViewModel.AvailableTransactionTargets);
	}

	[Fact]
	public void Memo()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.bankingViewModel,
			() => this.bankingViewModel.Memo = this.memo,
			nameof(this.bankingViewModel.Memo));
		Assert.Equal(this.memo, this.bankingViewModel.Memo);
	}

	[Fact]
	public void CreatedTaxLot_BankingAccount()
	{
		Assert.Null(this.bankingViewModel.CreatedTaxLot);
	}

	[Fact]
	public void CreatedTaxLot_InvestingAccount_Remove()
	{
		InvestingTransactionViewModel tx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 1,
		};

		Assert.Null(tx.Entries[0].CreatedTaxLot);
	}

	[Fact]
	public void CreatedTaxLot_InvestingAccount_Add()
	{
		InvestingTransactionViewModel tx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		TransactionEntryViewModel entry = tx.Entries[0];
		Assert.NotNull(entry.CreatedTaxLot);
		Assert.Same(entry, entry.CreatedTaxLot.CreatingTransactionEntry);

		Assert.Single(this.Money.TaxLots.Where(tl => tl.Id == entry.CreatedTaxLot.Id));
	}

	[Fact]
	public void CreatedTaxLot_InvestingAccount_ErasedWithChange()
	{
		InvestingTransactionViewModel tx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		TransactionEntryViewModel entry = tx.Entries[0];
		Assert.NotNull(entry.CreatedTaxLot);
		int taxLotId = entry.CreatedTaxLot.Id;

		// Verify that after an entry with a tax lot changes to one that shouldn't have a tax lot, the tax lot should be deleted.
		tx.Action = TransactionAction.Remove;
		Assert.Null(entry.CreatedTaxLot);
		Assert.Empty(this.Money.TaxLots.Where(tl => tl.Id == taxLotId));
	}

	[Fact]
	public void ApplyTo()
	{
		this.bankingViewModel.Account = this.spendingCategory;
		this.bankingViewModel.Amount = this.amount;
		this.bankingViewModel.Asset = this.DocumentViewModel.DefaultCurrency;
		this.bankingViewModel.Memo = this.memo;
		this.bankingViewModel.Cleared = ClearedState.Cleared;
		this.bankingViewModel.OfxFitId = this.ofxFitId;
		this.bankingViewModel.ApplyToModel();

		Assert.Equal(-this.amount, this.bankingViewModel.Model.Amount);
		Assert.Equal(this.memo, this.bankingViewModel.Model.Memo);
		Assert.Equal(ClearedState.Cleared, this.bankingViewModel.Model.Cleared);
		Assert.Equal(this.ofxFitId, this.bankingViewModel.Model.OfxFitId);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>("model", () => this.bankingViewModel.CopyFrom(null!));

		TransactionEntry splitTransaction = new()
		{
			Amount = this.amount,
			AssetId = this.checkingAccount.CurrencyAsset!.Id,
			Memo = this.memo,
			AccountId = this.spendingCategory.Id,
			OfxFitId = this.ofxFitId,
			Cleared = ClearedState.Cleared,
		};

		this.bankingViewModel.CopyFrom(splitTransaction);

		Assert.Equal(-splitTransaction.Amount, this.bankingViewModel.Amount);
		Assert.Equal(splitTransaction.Memo, this.bankingViewModel.Memo);
		Assert.Equal(this.spendingCategory.Id, this.bankingViewModel.Account?.Id);
		Assert.Equal(this.ofxFitId, this.bankingViewModel.OfxFitId);
		Assert.Equal(ClearedState.Cleared, this.bankingViewModel.Cleared);

		splitTransaction.AccountId = 0;
		this.bankingViewModel.CopyFrom(splitTransaction);
		Assert.Null(this.bankingViewModel.Account);
	}
}
