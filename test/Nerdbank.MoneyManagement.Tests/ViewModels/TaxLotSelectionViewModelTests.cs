// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;

public class TaxLotSelectionViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel account;
	private InvestingAccountViewModel otherAccount;
	private InvestingTransactionViewModel transaction;
	private AssetViewModel msft;
	private AssetViewModel appl;

	public TaxLotSelectionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		Account thisAccountModel = this.Money.Insert(new Account { Name = "this", Type = Account.AccountType.Investing, CurrencyAssetId = this.Money.PreferredAssetId });
		Account otherAccountModel = this.Money.Insert(new Account { Name = "other", Type = Account.AccountType.Investing, CurrencyAssetId = this.Money.PreferredAssetId });
		this.account = (InvestingAccountViewModel)this.DocumentViewModel.GetAccount(thisAccountModel.Id);
		this.otherAccount = (InvestingAccountViewModel)this.DocumentViewModel.GetAccount(otherAccountModel.Id);
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.account;
		this.msft = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft", "MSFT");
		this.appl = this.DocumentViewModel.AssetsPanel.NewAsset("Apple", "AAPL");
		this.PopulateWithTaxLots();
		this.transaction = this.account.Transactions[^1];
	}

	[Fact]
	public void TransactionShowsSelectionViewOnlyForAppropriateAction()
	{
		Assert.Null(this.transaction.TaxLotSelection);

		TransactionAction[] expected = new[] { TransactionAction.Sell, TransactionAction.Remove, TransactionAction.Transfer, TransactionAction.Exchange };

		foreach (TransactionAction action in Enum.GetValues(typeof(TransactionAction)))
		{
			try
			{
				this.transaction.Action = action;
			}
			catch (NotImplementedException)
			{
				continue;
			}

			if (expected.Contains(action))
			{
				Assert.NotNull(this.transaction.TaxLotSelection);
			}
			else
			{
				Assert.Null(this.transaction.TaxLotSelection);
			}
		}
	}

	[Fact]
	public void Assignments_IncludesOnlyLotsCreatedBeforeTransactionDate()
	{
		this.transaction.Action = TransactionAction.Sell;
		this.transaction.WithdrawAsset = this.msft;
		Assert.NotNull(this.transaction.TaxLotSelection);

		this.transaction.When = new DateTime(1999, 1, 1);
		Assert.All(this.transaction.TaxLotSelection.Assignments, a => Assert.True(a.AcquisitionDate <= this.transaction.When));
		Assert.Equal(2, this.transaction.TaxLotSelection.Assignments.Count);

		TestUtilities.AssertCollectionChangedEvent(
			(INotifyCollectionChanged)this.transaction.TaxLotSelection.Assignments,
			() => this.transaction.When = new DateTime(1999, 4, 1));
		Assert.Equal(3, this.transaction.TaxLotSelection.Assignments.Count);
	}

	[Fact]
	public void RequiredAssignments()
	{
		this.transaction.Action = TransactionAction.Sell;
		Assert.NotNull(this.transaction.TaxLotSelection);
		Assert.Equal(0, this.transaction.TaxLotSelection.RequiredAssignments);

		this.transaction.WithdrawAsset = this.msft;
		this.transaction.WithdrawAccount = this.account;
		TestUtilities.AssertPropertyChangedEvent(
			this.transaction.TaxLotSelection,
			() => this.transaction.WithdrawAmount = 5,
			nameof(this.transaction.TaxLotSelection.RequiredAssignments));
		Assert.Equal(this.transaction.WithdrawAmount, this.transaction.TaxLotSelection.RequiredAssignments);
	}

	[Fact]
	public void ActualAssignments()
	{
		this.transaction.Action = TransactionAction.Sell;
		this.transaction.WithdrawAsset = this.msft;
		this.transaction.When = new DateTime(2000, 1, 1);
		Assert.NotNull(this.transaction.TaxLotSelection);
		Assert.Equal(0, this.transaction.TaxLotSelection.ActualAssignments);

		this.transaction.TaxLotSelection.Assignments[0].Assigned = 18;
		Assert.Equal(18, this.transaction.TaxLotSelection.ActualAssignments);
		this.transaction.TaxLotSelection.Assignments[1].Assigned = 3;
		Assert.Equal(21, this.transaction.TaxLotSelection.ActualAssignments);
	}

	[Fact]
	public void SalePrice()
	{
		this.transaction.Action = TransactionAction.Sell;
		Assert.NotNull(this.transaction.TaxLotSelection);
		Assert.Null(this.transaction.TaxLotSelection.SalePrice);
		this.transaction.SimpleAmount = 3;
		this.transaction.SimpleAsset = this.msft;
		TestUtilities.AssertPropertyChangedEvent(
			this.transaction.TaxLotSelection,
			() => this.transaction.SimplePrice = 200,
			nameof(this.transaction.TaxLotSelection.SalePrice),
			nameof(this.transaction.TaxLotSelection.SalePriceFormatted));
		Assert.Equal(200, this.transaction.TaxLotSelection.SalePrice);
	}

	[Fact]
	public void Assignment_Available()
	{
		this.transaction.Action = TransactionAction.Sell;
		this.transaction.SimpleAsset = this.msft;
		this.transaction.When = new DateTime(1999, 1, 1);
		Assert.NotNull(this.transaction.TaxLotSelection);
		Assert.Equal(3, this.transaction.TaxLotSelection.Assignments[0].Available);
	}

	[Fact]
	public void Assignment_Price()
	{
		this.transaction.Action = TransactionAction.Sell;
		this.transaction.SimpleAsset = this.msft;
		this.transaction.When = new DateTime(1999, 1, 1);
		Assert.NotNull(this.transaction.TaxLotSelection);
		Assert.Equal(50, this.transaction.TaxLotSelection.Assignments[0].Price);
		Assert.Equal("$50.00", this.transaction.TaxLotSelection.Assignments[0].PriceFormatted);
	}

	[Fact]
	public void Assignment_GainLoss()
	{
		this.transaction.Action = TransactionAction.Sell;
		this.transaction.SimpleAsset = this.msft;
		this.transaction.SimpleAmount = 5;
		this.transaction.SimplePrice = 60;
		this.transaction.When = new DateTime(1999, 1, 1);
		Assert.NotNull(this.transaction.TaxLotSelection);
		Assert.Equal((60 - 50) * 3, this.transaction.TaxLotSelection.Assignments[0].GainLoss);
		Assert.Equal((60 - 100) * 2, this.transaction.TaxLotSelection.Assignments[1].GainLoss);
		Assert.Equal("$30.00", this.transaction.TaxLotSelection.Assignments[0].GainLossFormatted);
	}

	[Fact]
	public void SalePrice_Transfer()
	{
		this.transaction.Action = TransactionAction.Transfer;
		Assert.NotNull(this.transaction.TaxLotSelection);
		Assert.Null(this.transaction.TaxLotSelection.SalePrice);
		Assert.Null(this.transaction.TaxLotSelection.SalePriceFormatted);
	}

	private TransactionEntryViewModel CreateBuyTransactionEntry(DateTime acquired)
	{
		InvestingTransactionViewModel tx = this.account.NewTransaction();
		tx.Action = TransactionAction.Buy;
		tx.When = acquired;
		tx.SimpleAmount = 2;
		tx.SimpleAsset = this.msft;
		tx.SimplePrice = 5.13m;
		TransactionEntryViewModel txEntry = tx.Entries.Single(te => te.Asset == this.msft);
		return txEntry;
	}

	private TransactionEntryViewModel CreateSellTransactionEntry(DateTime acquired)
	{
		InvestingTransactionViewModel tx = this.account.NewTransaction();
		tx.Action = TransactionAction.Sell;
		tx.When = acquired;
		tx.SimpleAmount = 2;
		tx.SimpleAsset = this.msft;
		tx.SimplePrice = 6.50m;
		TransactionEntryViewModel txEntry = tx.Entries.Single(te => te.Asset == this.msft);
		return txEntry;
	}

	private void PopulateWithTaxLots()
	{
		InvestingTransactionViewModel buyMsft1 = new(this.account)
		{
			Action = TransactionAction.Buy,
			When = new DateTime(1992, 1, 1),
			DepositAccount = this.account,
			DepositAsset = this.msft,
			DepositAmount = 3,
			WithdrawAccount = this.account,
			WithdrawAmount = 150,
			WithdrawAsset = this.account.CurrencyAsset,
		};
		buyMsft1.Save();

		InvestingTransactionViewModel buyMsft2 = new(this.account)
		{
			Action = TransactionAction.Buy,
			When = new DateTime(1995, 1, 1),
			DepositAccount = this.account,
			DepositAsset = this.msft,
			DepositAmount = 15,
			WithdrawAccount = this.account,
			WithdrawAmount = 1500,
			WithdrawAsset = this.account.CurrencyAsset,
		};
		buyMsft2.Save();

		InvestingTransactionViewModel buyAppl = new(this.account)
		{
			Action = TransactionAction.Add,
			When = new DateTime(1998, 3, 4),
			DepositAccount = this.account,
			DepositAsset = this.appl,
			DepositAmount = 3,
			AcquisitionDate = new DateTime(1993, 2, 3),
			AcquisitionPrice = 60,
		};
		buyAppl.Save();

		InvestingTransactionViewModel addMsft1 = new(this.account)
		{
			Action = TransactionAction.Add,
			When = new DateTime(1999, 3, 4),
			DepositAccount = this.account,
			DepositAsset = this.msft,
			DepositAmount = 5,
			AcquisitionDate = new DateTime(1993, 2, 3),
			AcquisitionPrice = 60,
		};
		addMsft1.Save();
	}
}
