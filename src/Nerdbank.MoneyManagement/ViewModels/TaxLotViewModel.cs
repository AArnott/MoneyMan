// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public class TaxLotViewModel : EntityViewModel<TaxLot>
{
	private readonly DocumentViewModel documentViewModel;
	private decimal? amount;
	private DateTime? acquiredDate;

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

	/// <summary>
	/// Gets or sets the date this lot was acquired for tax reporting purposes.
	/// </summary>
	/// <remarks>
	/// If not explicitly set, this property will inherit from <see cref="Transaction.When"/> on the transaction to which the <see cref="CreatingTransactionEntry"/> belongs.
	/// To switch from an explicitly set value to an inherited one, set <see cref="AcquiredDateIsInherited"/> to <see langword="true" />.
	/// </remarks>
	public DateTime AcquiredDate
	{
		get => this.acquiredDate ?? this.CreatingTransactionEntry.Transaction.When;
		set => this.acquiredDate = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the value in the <see cref="AcquiredDate"/> property is inherited from the <see cref="TransactionViewModel.When"/> property of the <see cref="TransactionViewModel"/> to which the <see cref="CreatingTransactionEntry"/> belongs.
	/// </summary>
	public bool AcquiredDateIsInherited
	{
		get => this.acquiredDate is null;
		set
		{
			ThrowIfNotInheriting(value, nameof(this.AcquiredDate));
			this.acquiredDate = null;
		}
	}

	/// <summary>
	/// Gets or sets the amount of the asset created by the transaction entry identified by <see cref="CreatingTransactionEntry"/>
	/// that this lot represents.
	/// </summary>
	/// <remarks>
	/// If it is not explicitly set, this property will inherit from <see cref="TransactionEntryViewModel.ModelAmount"/> on the <see cref="CreatingTransactionEntry"/>.
	/// To switch from an explicitly set value to an inherited one, set <see cref="AmountIsInherited"/> to <see langword="true" />.
	/// </remarks>
	public decimal Amount
	{
		get => this.amount ?? this.CreatingTransactionEntry.ModelAmount;
		set => this.amount = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the value in the <see cref="Amount"/> property is inherited from the <see cref="TransactionEntryViewModel.Amount"/> in the <see cref="CreatingTransactionEntry"/>.
	/// </summary>
	public bool AmountIsInherited
	{
		get => this.amount is null;
		set
		{
			ThrowIfNotInheriting(value, nameof(this.Amount));
			this.amount = null;
		}
	}

	/// <inheritdoc cref="TaxLot.CostBasisAmount"/>
	public decimal? CostBasisAmount { get; set; }

	/// <inheritdoc cref="TaxLot.CostBasisAssetId"/>
	public AssetViewModel? CostBasisAsset { get; set; }

	/// <inheritdoc cref="TaxLot.CreatingTransactionEntryId"/>
	public TransactionEntryViewModel CreatingTransactionEntry { get; }

	public override bool IsReadyToSave => base.IsReadyToSave && this.CreatingTransactionEntry is not null;

	protected override void ApplyToCore()
	{
		Assumes.NotNull(this.CreatingTransactionEntry);

		this.Model.CostBasisAmount = this.CostBasisAmount;
		this.Model.CostBasisAssetId = this.CostBasisAsset?.Id;
		this.Model.AcquiredDate = this.acquiredDate;
		this.Model.CreatingTransactionEntryId = this.CreatingTransactionEntry.Id;
	}

	protected override void CopyFromCore()
	{
		this.CostBasisAmount = this.Model.CostBasisAmount;
		this.CostBasisAsset = this.documentViewModel.GetAsset(this.Model.CostBasisAssetId);
		this.acquiredDate = this.Model.AcquiredDate;
		Assumes.True(this.CreatingTransactionEntry.Id == this.Model.CreatingTransactionEntryId);
	}

	private static void ThrowIfNotInheriting(bool inheriting, string valuePropertyName)
	{
		if (!inheriting)
		{
			throw new ArgumentException($"This property can only be set to true. To disinherit, set the new value on the {valuePropertyName} property.", "value");
		}
	}
}
