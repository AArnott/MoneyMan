// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Nerdbank.MoneyManagement.ViewModels;

public class AssetPriceViewModel : EntityViewModel<AssetPrice>
{
	private readonly DocumentViewModel documentViewModel;
	private DateTime when;
	private AssetViewModel? asset;
	private AssetViewModel? referenceAsset;
	private decimal priceInReferenceAsset;

	public AssetPriceViewModel(DocumentViewModel documentViewModel, AssetPrice model)
		: base(documentViewModel.MoneyFile, model)
	{
		this.RegisterDependentProperty(nameof(this.Price), nameof(this.PriceFormatted));

		this.AutoSave = true;
		this.documentViewModel = documentViewModel;
		this.CopyFrom(this.Model);
	}

	[Required]
	public AssetViewModel? Asset
	{
		get => this.asset;
		set => this.SetProperty(ref this.asset, value);
	}

	public DateTime When
	{
		get => this.when;
		set => this.SetProperty(ref this.when, value);
	}

	[Required]
	public AssetViewModel? ReferenceAsset
	{
		get => this.referenceAsset;
		set => this.SetProperty(ref this.referenceAsset, value);
	}

	[NonZero]
	public decimal Price
	{
		get => this.priceInReferenceAsset;
		set => this.SetProperty(ref this.priceInReferenceAsset, value);
	}

	public string? PriceFormatted => this.documentViewModel.DefaultCurrency?.Format(this.Price);

	protected override bool IsPersistedProperty(string propertyName)
	{
		if (propertyName.EndsWith("Formatted"))
		{
			return false;
		}

		return base.IsPersistedProperty(propertyName);
	}

	protected override void ApplyToCore()
	{
		this.Model.AssetId = this.Asset?.Id ?? 0;
		this.Model.When = this.When;
		this.Model.ReferenceAssetId = this.ReferenceAsset?.Id ?? 0;
		this.Model.PriceInReferenceAsset = this.Price;
	}

	protected override void CopyFromCore()
	{
		this.Asset = this.documentViewModel.GetAsset(this.Model.AssetId);
		this.When = this.Model.When;
		this.ReferenceAsset = this.documentViewModel.GetAsset(this.Model.ReferenceAssetId);
		this.Price = this.Model.PriceInReferenceAsset;
	}
}
