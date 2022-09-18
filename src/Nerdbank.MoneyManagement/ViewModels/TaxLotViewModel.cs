// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public class TaxLotViewModel : EntityViewModel<TaxLot>
{
	private readonly DocumentViewModel documentViewModel;

	public TaxLotViewModel(DocumentViewModel documentViewModel, TaxLot? model = null)
		: base(documentViewModel.MoneyFile, model)
	{
		this.documentViewModel = documentViewModel;
		this.CopyFrom(this.Model);
	}

	public DateTime? AcquiredDate { get; set; }

	public decimal? CostBasisAmount { get; set; }

	public Asset? CostBasisAsset { get; set; }

	public TransactionEntryViewModel? CreatingTransactionEntry { get; set; }

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
		this.CreatingTransactionEntry = this.Model.CreatingTransactionEntryId > 0 ? this.documentViewModel.GetTransactionEntry(this.Model.CreatingTransactionEntryId) : null;
	}
}
