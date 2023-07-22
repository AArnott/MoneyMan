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

	public class Special : TaxLotSelectionViewModelTests
	{
		public Special(ITestOutputHelper logger)
			: base(logger)
		{
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
		public void RequiredAssignments()
		{
			this.transaction.Action = TransactionAction.Sell;
			Assert.NotNull(this.transaction.TaxLotSelection);
			Assert.Null(this.transaction.TaxLotSelection.RequiredAssignments);

			this.transaction.SimpleAsset = this.msft;
			this.transaction.SimpleAccount = this.account;
			TestUtilities.AssertPropertyChangedEvent(
				this.transaction.TaxLotSelection,
				() => this.transaction.SimpleAmount = 5,
				nameof(this.transaction.TaxLotSelection.RequiredAssignments));
			Assert.Equal(this.transaction.WithdrawAmount, this.transaction.TaxLotSelection.RequiredAssignments);
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
		public void SalePrice_Transfer()
		{
			this.transaction.Action = TransactionAction.Transfer;
			Assert.NotNull(this.transaction.TaxLotSelection);
			Assert.Null(this.transaction.TaxLotSelection.SalePrice);
			Assert.Null(this.transaction.TaxLotSelection.SalePriceFormatted);
		}

		[Fact]
		public void IsGainLossColumnVisible()
		{
			this.transaction.Action = TransactionAction.Transfer;
			Assert.False(this.transaction.TaxLotSelection?.IsGainLossColumnVisible);
			TestUtilities.AssertPropertyChangedEvent(
				this.transaction.TaxLotSelection!,
				() => this.transaction.Action = TransactionAction.Exchange,
				nameof(this.transaction.TaxLotSelection.IsGainLossColumnVisible));
			Assert.True(this.transaction.TaxLotSelection?.IsGainLossColumnVisible);
			this.transaction.Action = TransactionAction.Sell;
			Assert.True(this.transaction.TaxLotSelection?.IsGainLossColumnVisible);
		}

		[Fact]
		public void Assignment_ZeroRowsAreRemoved()
		{
			this.transaction.Action = TransactionAction.Remove;
			this.transaction.SimpleAsset = this.msft;
			this.transaction.SimpleAmount = 1;
			this.transaction.When = new DateTime(1999, 1, 1);
			Assert.NotNull(this.transaction.TaxLotSelection);
			this.transaction.TaxLotSelection.Assignments[0].Assigned = 1;
			this.transaction.TaxLotSelection.Assignments[0].Assigned = 0;
			Assert.Empty(this.Money.GetTaxLotAssignments(this.transaction.Entries.Single().Id));
		}
	}

	public class Sell : TaxLotSelectionViewModelTests
	{
		private TaxLotSelectionViewModel viewModel;

		public Sell(ITestOutputHelper logger)
			: base(logger)
		{
			this.transaction.When = new DateTime(1999, 1, 1);
			this.transaction.Action = TransactionAction.Sell;

			// Besides asserting that the selector becomes visible early on,
			// but checking this forces its initialization, which matches the View's behavior,
			// and allows these tests to assert that subsequent initialization of the sale parameters
			// is correctly compensated for in the tax lot selection view.
			Assert.NotNull(this.transaction.TaxLotSelection);

			this.transaction.SimpleAsset = this.msft;
			this.transaction.SimpleAmount = 5;
			this.transaction.SimplePrice = 60;
			Assert.NotNull(this.transaction.TaxLotSelection);
			this.viewModel = this.transaction.TaxLotSelection;
		}

		[Fact]
		public void Assignment_ChangesAreSaved()
		{
			this.viewModel.Assignments[0].Assigned = 1;
			Assert.Equal(string.Empty, this.viewModel.Assignments[0].Error);
			Assert.False(this.viewModel.Assignments[0].IsDirty);
			Assert.Equal(1, this.viewModel.Assignments[0].Model.Amount);
		}

		[Fact]
		public void Assignment_GainLoss()
		{
			Assert.Equal((60 - 50) * 3, this.viewModel.Assignments[0].GainLoss);
			Assert.Equal((60 - 100) * 2, this.viewModel.Assignments[1].GainLoss);
			Assert.Equal("$30.00", this.viewModel.Assignments[0].GainLossFormatted);
		}

		[Fact]
		public void Assignment_Available()
		{
			Assert.Equal(3, this.viewModel.Assignments[0].Available);
		}

		[Fact]
		public void ActualAssignments()
		{
			Assert.Equal(5, this.viewModel.ActualAssignments);

			this.viewModel.Assignments[0].Assigned = 18;
			Assert.Equal(18 + 2, this.viewModel.ActualAssignments);
			this.viewModel.Assignments[1].Assigned = 3;
			Assert.Equal(18 + 3, this.viewModel.ActualAssignments);
		}

		[Fact]
		public void Assignments_IncludesOnlyLotsCreatedBeforeTransactionDate()
		{
			Assert.All(this.viewModel.Assignments, a => Assert.True(a.AcquisitionDate <= this.transaction.When));
			Assert.Equal(2, this.viewModel.Assignments.Count);

			TestUtilities.AssertCollectionChangedEvent(
				(INotifyCollectionChanged)this.viewModel.Assignments,
				() => this.transaction.When = new DateTime(1999, 4, 1));
			Assert.Equal(3, this.viewModel.Assignments.Count);
		}

		[Fact]
		public void Assignment_Price()
		{
			Assert.Equal(50, this.viewModel.Assignments[0].Price);
			Assert.Equal("$50.00", this.viewModel.Assignments[0].PriceFormatted);
		}

		[Fact]
		public void ShowAllTaxLots_Toggle()
		{
			Assert.True(this.viewModel.ShowAllTaxLots);
			TestUtilities.AssertPropertyChangedEvent(
				this.viewModel,
				() => this.viewModel.ShowAllTaxLots = !this.viewModel.ShowAllTaxLots,
				nameof(this.viewModel.ShowAllTaxLots),
				nameof(this.viewModel.ShowAllTaxLotsLabel));
			Assert.False(this.viewModel.ShowAllTaxLots);
		}

		[Fact]
		public void ShowAllTaxLots_ChangesVisibleTransactions()
		{
			this.transaction.SimpleAmount = 1;
			this.viewModel.Assignments[0].Assigned = 1;
			this.viewModel.Assignments[1].Assigned = 0;
			Assert.Equal(1, this.viewModel.ActualAssignments);
			Assert.True(this.viewModel.Assignments.Count > 1);
			this.viewModel.ShowAllTaxLots = false;
			Assert.Single(this.viewModel.Assignments);
			this.viewModel.ShowAllTaxLots = true;
			Assert.True(this.viewModel.Assignments.Count > 1);
		}

		[Fact]
		public void Assignments_UpdateWithOtherTransactionChanges()
		{
			// The selected transaction has already initialized its tax lot view. Capture the number of tax lots currently visible.
			int oldCount = this.viewModel.Assignments.Count;

			// Create a new transaction that adds a tax lot, with a date that precedes the transaction we were looking at.
			InvestingTransactionViewModel newTx = this.account.Transactions[^1];
			this.DocumentViewModel.SelectedTransaction = newTx;
			newTx.When = new DateTime(1998, 1, 1);
			newTx.Action = TransactionAction.Add;
			newTx.SimplePrice = 30;
			newTx.SimpleAsset = this.msft;
			newTx.SimpleAmount = 3;
			Assert.False(newTx.IsDirty);

			// Now go back to the original (later) transaction and verify that it shows the new tax lot.
			this.DocumentViewModel.SelectedTransaction = this.transaction;
			Assert.NotNull(this.transaction.TaxLotSelection);
			this.viewModel = this.transaction.TaxLotSelection;
			Assert.Equal(oldCount + 1, this.viewModel.Assignments.Count);
		}
	}
}
