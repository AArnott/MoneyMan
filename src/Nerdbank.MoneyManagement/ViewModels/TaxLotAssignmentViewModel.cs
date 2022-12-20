// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Nerdbank.MoneyManagement.ViewModels;

public class TaxLotAssignmentViewModel : EntityViewModel<TaxLotAssignment>
{
	private readonly DocumentViewModel documentViewModel;
	private readonly TaxLotSelectionViewModel selectionViewModel;
	private DateTime acquisitionDate;
	private decimal price;
	private decimal available;
	private decimal assigned;

	public TaxLotAssignmentViewModel(DocumentViewModel documentViewModel, TaxLotSelectionViewModel selectionViewModel, TaxLotAssignment? model = null)
		: base(documentViewModel.MoneyFile, model)
	{
		this.documentViewModel = documentViewModel;
		this.selectionViewModel = selectionViewModel;

		this.RegisterDependentProperty(nameof(this.Assigned), nameof(this.GainLoss));
		this.RegisterDependentProperty(nameof(this.Price), nameof(this.GainLoss));
		this.RegisterDependentProperty(nameof(this.Price), nameof(this.PriceFormatted));
		this.RegisterDependentProperty(nameof(this.GainLoss), nameof(this.GainLossFormatted));
	}

	/// <inheritdoc cref="TaxLotAssignment.TaxLotId" />
	public int TaxLotId { get; set; }

	/// <inheritdoc cref="TaxLotAssignment.ConsumingTransactionEntryId" />
	public TransactionEntryViewModel? ConsumingTransactionEntry { get; set; }

	public DateTime AcquisitionDate
	{
		get => this.acquisitionDate;
		set => this.SetProperty(ref this.acquisitionDate, value);
	}

	/// <summary>
	/// Gets the per-unit price of this asset when it was acquired.
	/// </summary>
	public decimal Price
	{
		get => this.price;
		internal set => this.SetProperty(ref this.price, value);
	}

	public AssetViewModel? CostBasisAsset { get; set; }

	public string? PriceFormatted => this.CostBasisAsset?.Format(this.Price);

	/// <summary>
	/// Gets the amount of the asset that may still be assigned in this tax lot.
	/// </summary>
	public decimal Available
	{
		get => this.available;
		internal set => this.SetProperty(ref this.available, value);
	}

	/// <inheritdoc cref="TaxLotAssignment.Amount" />
	public decimal Assigned
	{
		get => this.assigned;
		set => this.SetProperty(ref this.assigned, value);
	}

	public decimal? GainLoss => this.selectionViewModel.SalePrice is not null ? this.Assigned * (this.selectionViewModel.SalePrice.Value - this.Price) : null;

	public string? GainLossFormatted => this.selectionViewModel.Transaction.ThisAccount.CurrencyAsset?.Format(this.GainLoss);

	/// <inheritdoc cref="TaxLotAssignment.Pinned"/>
	public bool Pinned { get; set; }

	public override bool IsReadyToSave => base.IsReadyToSave && this.ConsumingTransactionEntry is not null;

	internal void OnSelectionViewModelPropertyChanged(string? propertyName)
	{
		if (propertyName is nameof(TaxLotSelectionViewModel.SalePrice))
		{
			this.OnPropertyChanged(nameof(this.GainLoss));
		}
	}

	protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);
		this.selectionViewModel.OnAssignmentPropertyChanged(this, propertyName);
	}

	protected override void ApplyToCore()
	{
		this.Model.TaxLotId = this.TaxLotId;
		this.Model.Amount = this.Assigned;
		this.Model.ConsumingTransactionEntryId = this.ConsumingTransactionEntry!.Id;
		this.Model.Pinned = this.Pinned;
	}

	protected override void CopyFromCore()
	{
		this.TaxLotId = this.Model.TaxLotId;
		this.Assigned = this.Model.Amount;
		this.ConsumingTransactionEntry = this.documentViewModel.GetTransactionEntry(this.Model.ConsumingTransactionEntryId);
		this.Pinned = this.Model.Pinned;
	}
}
