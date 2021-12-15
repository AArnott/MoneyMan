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

	public AssetPriceViewModel(DocumentViewModel documentViewModel, AssetPrice? model)
		: base(documentViewModel.MoneyFile)
	{
		this.AutoSave = true;
		this.Model = model;
		this.documentViewModel = documentViewModel;
		if (model is object)
		{
			this.CopyFrom(model);
		}
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

	public decimal Price
	{
		get => this.priceInReferenceAsset;
		set => this.SetProperty(ref this.priceInReferenceAsset, value);
	}

	protected override void ApplyToCore(AssetPrice model)
	{
		model.AssetId = this.Asset?.Id ?? 0;
		model.When = this.When;
		model.ReferenceAssetId = this.ReferenceAsset?.Id ?? 0;
		model.PriceInReferenceAsset = this.Price;
	}

	protected override void CopyFromCore(AssetPrice model)
	{
		this.Asset = this.documentViewModel.GetAsset(model.AssetId);
		this.When = model.When;
		this.ReferenceAsset = this.documentViewModel.GetAsset(model.ReferenceAssetId);
		this.Price = model.PriceInReferenceAsset;
	}
}
