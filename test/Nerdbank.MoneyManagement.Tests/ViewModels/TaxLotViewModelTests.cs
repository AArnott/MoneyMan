// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

public class TaxLotViewModelTests : MoneyTestBase
{
	private InvestingAccountViewModel brokerageAccount;
	private AssetViewModel someAsset;
	private InvestingTransactionViewModel transaction;
	private TransactionEntryViewModel entry;

	public TaxLotViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.someAsset = this.DocumentViewModel.AssetsPanel.NewAsset("Microsoft", "MSFT");
		this.brokerageAccount = this.DocumentViewModel.AccountsPanel.NewInvestingAccount("Brokerage");
		this.transaction = this.brokerageAccount.NewTransaction();
		this.transaction.Action = TransactionAction.Add;
		this.transaction.DepositAsset = this.someAsset;
		this.transaction.DepositAmount = 1;
		this.transaction.DepositAccount = this.brokerageAccount;
		this.entry = this.transaction.Entries.Single();
	}

	[Fact]
	public void ApplyToModel()
	{
		DateTime acquired = new(2022, 9, 2);
		TransactionEntryViewModel txEntry = this.CreateBuyTransactionEntry(acquired);
		Asset currency = this.DocumentViewModel.ConfigurationPanel.PreferredAsset ?? throw Assumes.Fail(null);

		TaxLotViewModel viewModel = new(this.DocumentViewModel, txEntry)
		{
			CostBasisAmount = 15.35m,
			CostBasisAsset = currency,
			AcquiredDate = acquired,
		};
		viewModel.ApplyToModel();
		Assert.Equal(15.35m, viewModel.Model.CostBasisAmount);
		Assert.Equal(currency.Id, viewModel.Model.CostBasisAssetId);
		Assert.Equal(acquired, viewModel.Model.AcquiredDate);
		Assert.Equal(txEntry.Id, viewModel.Model.CreatingTransactionEntryId);
	}

	[Fact]
	public void Ctor_CallsCopyFrom()
	{
		Asset asset = this.DocumentViewModel.ConfigurationPanel.PreferredAsset ?? throw Assumes.Fail(null);
		TaxLot taxLot = new()
		{
			CostBasisAmount = 12.34m,
			CostBasisAssetId = asset.Id,
			CreatingTransactionEntryId = this.entry.Id,
		};
		TaxLotViewModel viewModel = new(this.DocumentViewModel, this.entry, taxLot);
		Assert.Equal(12.34m, viewModel.CostBasisAmount);
		Assert.Same(asset, viewModel.CostBasisAsset);
	}

	[Fact]
	public void CopyFrom()
	{
		DateTime acquired = new(2022, 9, 2);
		TransactionEntryViewModel txEntry = this.CreateBuyTransactionEntry(acquired);
		Asset currency = this.DocumentViewModel.ConfigurationPanel.PreferredAsset ?? throw Assumes.Fail(null);

		TaxLot taxLot = new()
		{
			CostBasisAmount = 12.34m,
			CostBasisAssetId = currency.Id,
			AcquiredDate = acquired,
			CreatingTransactionEntryId = txEntry.Id,
		};
		TaxLotViewModel viewModel = new(this.DocumentViewModel, txEntry);
		viewModel.CopyFrom(taxLot);
		Assert.Equal(12.34m, viewModel.CostBasisAmount);
		Assert.Same(currency, viewModel.CostBasisAsset);
		Assert.Equal(acquired, viewModel.AcquiredDate);
		Assert.Same(txEntry, viewModel.CreatingTransactionEntry);
	}

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
}
