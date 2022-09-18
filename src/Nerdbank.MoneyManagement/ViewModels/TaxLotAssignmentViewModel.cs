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
	public TaxLot? TaxLot { get; set; }

	/// <inheritdoc cref="TaxLotAssignment.ConsumingTransactionEntryId" />
	public TransactionEntry? ConsumingTransactionEntry { get; set; }

	/// <inheritdoc cref="TaxLotAssignment.Amount" />
	public decimal Amount { get; set; }

	public override bool IsReadyToSave => base.IsReadyToSave && this.TaxLot is not null && this.ConsumingTransactionEntry is not null;

	protected override void ApplyToCore()
	{
		throw new NotImplementedException();
	}

	protected override void CopyFromCore()
	{
		throw new NotImplementedException();
	}
}
