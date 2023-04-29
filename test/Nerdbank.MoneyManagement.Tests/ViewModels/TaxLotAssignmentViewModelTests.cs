// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

public class TaxLotAssignmentViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel brokerageAccount;
	private AssetViewModel someAsset;

	public TaxLotAssignmentViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.brokerageAccount = this.DocumentViewModel.AccountsPanel.NewInvestingAccount("Brokerage");
		this.someAsset = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft", "MSFT");
	}

	////[Fact]
	////public void ApplyToModel()
	////{
	////	TransactionEntryViewModel buyTe = this.CreateBuyTransactionEntry(new DateTime(2022, 1, 2));
	////	TransactionEntryViewModel sellTe = this.CreateSellTransactionEntry(new DateTime(2022, 2, 2));
	////	TaxLotViewModel createdTaxLot = SingleCreatedTaxLot(buyTe);
	////	TaxLotAssignmentViewModel viewModel = new(this.DocumentViewModel)
	////	{
	////		Assigned = 2,
	////		ConsumingTransactionEntry = sellTe,
	////		TaxLotId = createdTaxLot.Id,
	////		Pinned = true,
	////	};
	////	viewModel.ApplyToModel();
	////	Assert.Equal(viewModel.Assigned, viewModel.Model.Amount);
	////	Assert.Equal(sellTe.Id, viewModel.Model.ConsumingTransactionEntryId);
	////	Assert.Equal(createdTaxLot.Id, viewModel.Model.TaxLotId);
	////	Assert.Equal(viewModel.Pinned, viewModel.Model.Pinned);
	////}

	////[Fact]
	////public void CopyFrom()
	////{
	////	TransactionEntryViewModel buyTe = this.CreateBuyTransactionEntry(new DateTime(2022, 1, 2));
	////	TransactionEntryViewModel sellTe = this.CreateSellTransactionEntry(new DateTime(2022, 2, 2));
	////	TaxLotViewModel createdTaxLot = SingleCreatedTaxLot(buyTe);
	////	TaxLotAssignment model = new()
	////	{
	////		Amount = 2,
	////		ConsumingTransactionEntryId = sellTe.Id,
	////		TaxLotId = createdTaxLot.Id,
	////		Pinned = true,
	////	};
	////	TaxLotAssignmentViewModel viewModel = new(this.DocumentViewModel);
	////	viewModel.CopyFrom(model);
	////	Assert.Equal(model.Amount, viewModel.Assigned);
	////	Assert.Same(sellTe, viewModel.ConsumingTransactionEntry);
	////	Assert.Equal(createdTaxLot.Id, viewModel.TaxLotId);
	////	Assert.Equal(model.Pinned, viewModel.Pinned);
	////}

	private TransactionEntryViewModel CreateBuyTransactionEntry(DateTime acquired)
	{
		InvestingTransactionViewModel tx = this.brokerageAccount.NewTransaction();
		tx.Action = TransactionAction.Buy;
		tx.When = acquired;
		tx.SimpleAmount = 2;
		tx.SimpleAsset = this.someAsset;
		tx.SimplePrice = 5.13m;
		TransactionEntryViewModel txEntry = tx.Entries.Single(te => te.Asset == this.someAsset);
		return txEntry;
	}

	private TransactionEntryViewModel CreateSellTransactionEntry(DateTime acquired)
	{
		InvestingTransactionViewModel tx = this.brokerageAccount.NewTransaction();
		tx.Action = TransactionAction.Sell;
		tx.When = acquired;
		tx.SimpleAmount = 2;
		tx.SimpleAsset = this.someAsset;
		tx.SimplePrice = 6.50m;
		TransactionEntryViewModel txEntry = tx.Entries.Single(te => te.Asset == this.someAsset);
		return txEntry;
	}
}
