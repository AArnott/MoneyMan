// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Nerdbank.MoneyManagement.ViewModels;

public class ConfigurationPanelViewModel : EntityViewModel<Configuration>
{
	private readonly DocumentViewModel documentViewModel;
	private int? preferredAssetId;
	private int? commissionAccountId;

	public ConfigurationPanelViewModel(DocumentViewModel documentViewModel)
		: base(documentViewModel.MoneyFile, documentViewModel.MoneyFile.CurrentConfiguration)
	{
		this.documentViewModel = documentViewModel;
		this.CopyFrom(this.Model);
	}

	public string Title => "Configuration";

	public string PreferredAssetLabel => "Default _asset";

	[Required]
	public AssetViewModel? PreferredAsset
	{
		get => this.documentViewModel.GetAsset(this.preferredAssetId);
		set => this.SetProperty(ref this.preferredAssetId, value?.Id);
	}

	[Required]
	public CategoryAccountViewModel? CommissionCategory
	{
		get => this.documentViewModel.GetCategory(this.commissionAccountId);
		set => this.SetProperty(ref this.commissionAccountId, value?.Id);
	}

	protected override void ApplyToCore()
	{
		this.Model.PreferredAssetId = this.preferredAssetId ?? 0;
		this.Model.CommissionAccountId = this.commissionAccountId ?? 0;
	}

	protected override void CopyFromCore()
	{
		this.preferredAssetId = this.Model.PreferredAssetId;
		this.OnPropertyChanged(nameof(this.PreferredAsset));
		this.commissionAccountId = this.Model.CommissionAccountId;
		this.OnPropertyChanged(nameof(this.CommissionCategory));
	}
}
