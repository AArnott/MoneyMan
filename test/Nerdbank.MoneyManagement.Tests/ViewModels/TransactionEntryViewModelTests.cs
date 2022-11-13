// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using SQLite;

public class TransactionEntryViewModelTests : MoneyTestBase
{
	private BankingAccountViewModel checkingAccount;
	private InvestingAccountViewModel brokerageAccount;
	private CategoryAccountViewModel spendingCategory;
	private BankingTransactionViewModel bankingTransaction;
	private TransactionEntryViewModel bankingViewModel;
	private AssetViewModel msft;
	private AssetViewModel aapl;

	private decimal amount = 5.5m;
	private string ofxFitId = "someFitId";
	private string memo = "Some memo";

	public TransactionEntryViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.spendingCategory = this.DocumentViewModel.CategoriesPanel.NewCategory("Spending");
		this.msft = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft", "MSFT");
		this.aapl = this.DocumentViewModel.AssetsPanel.NewAsset("Apple", "AAPL");

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
	public void CreatedTaxLot_BankingAccount_IsNull()
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
		Assert.Equal(tx.When, entry.CreatedTaxLot.AcquiredDate);
		Assert.Null(entry.CreatedTaxLot.CostBasisAmount);
		Assert.Null(entry.CreatedTaxLot.CostBasisAsset);

		Assert.Single(this.Money.TaxLots.Where(tl => tl.Id == entry.CreatedTaxLot.Id));
	}

	[Fact]
	public void CreatedTaxLot_InvestingAccount_Buy()
	{
		InvestingTransactionViewModel tx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Buy,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAmount = 50,
			WithdrawAsset = this.brokerageAccount.CurrencyAsset,
		};

		TransactionEntryViewModel addEntry = tx.Entries.Single(e => e.Amount > 0);
		Assert.NotNull(addEntry.CreatedTaxLot);
		Assert.Same(addEntry, addEntry.CreatedTaxLot.CreatingTransactionEntry);
		Assert.Equal(tx.When, addEntry.CreatedTaxLot.AcquiredDate);
		Assert.Equal(tx.WithdrawAmount, addEntry.CreatedTaxLot.CostBasisAmount);
		Assert.Same(tx.WithdrawAsset, addEntry.CreatedTaxLot.CostBasisAsset);

		Assert.Single(this.Money.TaxLots.Where(tl => tl.Id == addEntry.CreatedTaxLot.Id));
	}

	[Fact]
	public void CreatedTaxLot_InvestingAccount_Buy_ChangeAmount()
	{
		InvestingTransactionViewModel tx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Buy,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAmount = 50,
			WithdrawAsset = this.brokerageAccount.CurrencyAsset,
		};

		tx.WithdrawAmount = 70;

		TransactionEntryViewModel addEntry = tx.Entries.Single(e => e.Amount > 0);
		Assert.NotNull(addEntry.CreatedTaxLot);
		Assert.Same(addEntry, addEntry.CreatedTaxLot.CreatingTransactionEntry);
		Assert.Equal(tx.WithdrawAmount, addEntry.CreatedTaxLot.CostBasisAmount);

		Assert.Single(this.Money.TaxLots.Where(tl => tl.Id == addEntry.CreatedTaxLot.Id));
	}

	[Fact]
	public void CreatedTaxLot_InvestingAccount_Buy_ChangeAssetAndAmount()
	{
		InvestingTransactionViewModel tx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Buy,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAmount = 50,
			WithdrawAsset = this.brokerageAccount.CurrencyAsset,
		};

		tx.WithdrawAsset = this.aapl;
		tx.WithdrawAmount = 70;

		TransactionEntryViewModel addEntry = tx.Entries.Single(e => e.Amount > 0);
		Assert.NotNull(addEntry.CreatedTaxLot);
		Assert.Same(addEntry, addEntry.CreatedTaxLot.CreatingTransactionEntry);
		Assert.Equal(tx.WithdrawAmount, addEntry.CreatedTaxLot.CostBasisAmount);
		Assert.Same(tx.WithdrawAsset, addEntry.CreatedTaxLot.CostBasisAsset);

		Assert.Single(this.Money.TaxLots.Where(tl => tl.Id == addEntry.CreatedTaxLot.Id));
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
	public void Remove_ConsumedTaxLot_PartialEachTime()
	{
		InvestingTransactionViewModel addTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		TransactionEntryViewModel addEntry = addTx.Entries[0];
		Assert.NotNull(addEntry.CreatedTaxLot);
		int taxLotId = addEntry.CreatedTaxLot.Id;

		InvestingTransactionViewModel removeTx1 = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 0.5m,
		};

		TransactionEntryViewModel removeEntry1 = removeTx1.Entries[0];
		TaxLotAssignment consumingAssignment1 = Assert.Single(this.GetTaxLotAssignments(removeEntry1, addEntry.CreatedTaxLot));
		Assert.False(consumingAssignment1.Pinned);

		// Remove more
		InvestingTransactionViewModel removeTx2 = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 0.5m,
		};

		TransactionEntryViewModel removeEntry2 = removeTx2.Entries[0];
		TaxLotAssignment consumingAssignment2 = Assert.Single(this.GetTaxLotAssignments(removeEntry2, addEntry.CreatedTaxLot));
		Assert.False(consumingAssignment2.Pinned);
	}

	[Fact]
	public void Remove_ConsumedTaxLot_MultipleAtOnce()
	{
		InvestingTransactionViewModel addTx1 = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		TransactionEntryViewModel addEntry1 = addTx1.Entries[0];
		Assert.NotNull(addEntry1.CreatedTaxLot);
		int taxLotId1 = addEntry1.CreatedTaxLot.Id;
		Assert.NotEqual(0, taxLotId1);

		InvestingTransactionViewModel addTx2 = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 2,
		};

		TransactionEntryViewModel addEntry2 = addTx2.Entries[0];
		Assert.NotNull(addEntry2.CreatedTaxLot);
		int taxLotId2 = addEntry2.CreatedTaxLot.Id;

		InvestingTransactionViewModel removeTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 2.5m,
		};

		TransactionEntryViewModel removeEntry = removeTx.Entries[0];
		Assert.Equal(2, this.Money.TaxLotAssignments.Count());
		TaxLotAssignment consumingAssignment1 = this.Money.TaxLotAssignments.Single(tla => tla.TaxLotId == taxLotId1 && tla.ConsumingTransactionEntryId == removeEntry.Id);
		TaxLotAssignment consumingAssignment2 = this.Money.TaxLotAssignments.Single(tla => tla.TaxLotId == taxLotId2 && tla.ConsumingTransactionEntryId == removeEntry.Id);
		Assert.False(consumingAssignment1.Pinned);
		Assert.False(consumingAssignment2.Pinned);
		Assert.Equal(addTx1.DepositAmount, consumingAssignment1.Amount);
		Assert.Equal(addTx2.DepositAmount, consumingAssignment2.Amount);
	}

	[Fact]
	public void Remove_ConsumedTaxLot_AssignmentDisappearsWithDeletionOfRemoval()
	{
		InvestingTransactionViewModel addTx1 = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		TransactionEntryViewModel addEntry1 = addTx1.Entries[0];
		Assert.NotNull(addEntry1.CreatedTaxLot);
		int taxLotId1 = addEntry1.CreatedTaxLot.Id;
		Assert.NotEqual(0, taxLotId1);

		InvestingTransactionViewModel removeTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 2.5m,
		};

		TransactionEntryViewModel removeEntry = removeTx.Entries[0];
		Assert.NotEmpty(this.Money.TaxLotAssignments);

		this.Money.Delete(removeTx.Transaction);
		Assert.Empty(this.Money.TaxLotAssignments);
	}

	[Fact]
	public void Remove_MoreThanWeHaveTaxLotsFor()
	{
		InvestingTransactionViewModel addTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		TransactionEntryViewModel addEntry = addTx.Entries[0];
		Assert.NotNull(addEntry.CreatedTaxLot);
		int taxLotId = addEntry.CreatedTaxLot.Id;

		InvestingTransactionViewModel removeTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 2,
		};

		TransactionEntryViewModel removeEntry = removeTx.Entries[0];
		TaxLotAssignment tla = Assert.Single(this.GetTaxLotAssignments(removeEntry, addEntry.CreatedTaxLot));
		Assert.Equal(addTx.DepositAmount, tla.Amount);
	}

	[Fact]
	public void Remove_ThenChangeAsset()
	{
		InvestingTransactionViewModel addTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		InvestingTransactionViewModel removeTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 1,
		};

		// The removal should be from a tax lot -- until changed to an asset for which no tax lot exists.
		Assert.NotEmpty(this.Money.TaxLotAssignments);
		removeTx.WithdrawAsset = this.aapl;
		Assert.Empty(this.Money.TaxLotAssignments);
	}

	[Fact]
	public void Remove_ThenIncreaseAmount()
	{
		InvestingTransactionViewModel addTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		InvestingTransactionViewModel removeTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 0.5m,
		};

		removeTx.WithdrawAmount = 0.8m;
		TaxLotViewModel? taxLot = addTx.Entries[0].CreatedTaxLot;
		Assert.NotNull(taxLot);
		TaxLotAssignment tla = Assert.Single(this.GetTaxLotAssignments(removeTx.Entries[0], taxLot));
		Assert.Equal(removeTx.WithdrawAmount, tla.Amount);
	}

	[Fact]
	public void Remove_ThenDecreaseAmount()
	{
		InvestingTransactionViewModel addTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Add,
			DepositAccount = this.brokerageAccount,
			DepositAsset = this.msft,
			DepositAmount = 1,
		};

		InvestingTransactionViewModel removeTx = new(this.brokerageAccount)
		{
			Action = TransactionAction.Remove,
			WithdrawAccount = this.brokerageAccount,
			WithdrawAsset = this.msft,
			WithdrawAmount = 0.5m,
		};

		removeTx.WithdrawAmount = 0.3m;
		TaxLotViewModel? taxLot = addTx.Entries[0].CreatedTaxLot;
		Assert.NotNull(taxLot);
		TaxLotAssignment tla = Assert.Single(this.GetTaxLotAssignments(removeTx.Entries[0], taxLot));
		Assert.Equal(removeTx.WithdrawAmount, tla.Amount);
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

	/// <summary>
	/// Gets the assignments between a specified tax lot and a transaction entry that consumes it (via a sale or removal.)
	/// </summary>
	private TableQuery<TaxLotAssignment> GetTaxLotAssignments(TransactionEntryViewModel transactionEntry, TaxLotViewModel taxLot)
	{
		return this.Money.TaxLotAssignments.Where(tla => tla.ConsumingTransactionEntryId == transactionEntry.Id && tla.TaxLotId == taxLot.Id);
	}
}
