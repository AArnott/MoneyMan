// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

public class TaxLotAssignmentViewModel : EntityViewModel<TaxLotAssignment>
{
	private readonly DocumentViewModel documentViewModel;

	public TaxLotAssignmentViewModel(DocumentViewModel documentViewModel, TaxLotAssignment? model = null)
		: base(documentViewModel.MoneyFile, model)
	{
		this.documentViewModel = documentViewModel;
	}

	/// <inheritdoc cref="TaxLotAssignment.TaxLotId" />
	public int? TaxLotId { get; set; }

	/// <inheritdoc cref="TaxLotAssignment.ConsumingTransactionEntryId" />
	public TransactionEntryViewModel? ConsumingTransactionEntry { get; set; }

	/// <inheritdoc cref="TaxLotAssignment.Amount" />
	public decimal Amount { get; set; }

	public override bool IsReadyToSave => base.IsReadyToSave && this.TaxLotId is not null && this.ConsumingTransactionEntry is not null;

	protected override void ApplyToCore()
	{
		this.Model.TaxLotId = this.TaxLotId!.Value;
		this.Model.Amount = this.Amount;
		this.Model.ConsumingTransactionEntryId = this.ConsumingTransactionEntry!.Id;
	}

	protected override void CopyFromCore()
	{
		this.TaxLotId = this.Model.TaxLotId;
		this.Amount = this.Model.Amount;
		this.ConsumingTransactionEntry = this.documentViewModel.GetTransactionEntry(this.Model.ConsumingTransactionEntryId);
	}
}
