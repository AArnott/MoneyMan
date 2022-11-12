// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public class TaxLotViewModel : EntityViewModel<TaxLot>
{
	private readonly DocumentViewModel documentViewModel;

	public TaxLotViewModel(DocumentViewModel documentViewModel, TransactionEntryViewModel creatingTransactionEntry, TaxLot? model = null)
		: base(documentViewModel.MoneyFile, model)
	{
		this.documentViewModel = documentViewModel;
		this.CreatingTransactionEntry = creatingTransactionEntry;
		if (model is null)
		{
			this.Model.CreatingTransactionEntryId = creatingTransactionEntry.Id;
		}

		this.CopyFrom(this.Model);
	}

	public DateTime? AcquiredDate { get; set; }

	public decimal? CostBasisAmount { get; set; }

	public AssetViewModel? CostBasisAsset { get; set; }

	public TransactionEntryViewModel CreatingTransactionEntry { get; }

	public override bool IsReadyToSave => base.IsReadyToSave && this.CreatingTransactionEntry is not null;

	protected override void ApplyToCore()
	{
		Assumes.NotNull(this.CreatingTransactionEntry);

		this.Model.CostBasisAmount = this.CostBasisAmount;
		this.Model.CostBasisAssetId = this.CostBasisAsset?.Id;
		this.Model.AcquiredDate = this.AcquiredDate;
		this.Model.CreatingTransactionEntryId = this.CreatingTransactionEntry.Id;
	}

	protected override void CopyFromCore()
	{
		this.CostBasisAmount = this.Model.CostBasisAmount;
		this.CostBasisAsset = this.documentViewModel.GetAsset(this.Model.CostBasisAssetId);
		this.AcquiredDate = this.Model.AcquiredDate;
		Assumes.True(this.CreatingTransactionEntry.Id == this.Model.CreatingTransactionEntryId);
	}
}
