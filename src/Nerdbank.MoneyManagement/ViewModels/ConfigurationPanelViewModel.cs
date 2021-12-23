// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Nerdbank.MoneyManagement.ViewModels;

public class ConfigurationPanelViewModel : EntityViewModel<Configuration>
{
	private readonly DocumentViewModel documentViewModel;
	private int? preferredAssetId;

	public ConfigurationPanelViewModel(DocumentViewModel documentViewModel)
		: base(documentViewModel.MoneyFile)
	{
		this.documentViewModel = documentViewModel;
		this.AutoSave = true;
		this.CopyFrom(documentViewModel.MoneyFile.CurrentConfiguration);
	}

	public string Title => "Configuration";

	public string PreferredAssetLabel => "Default _asset";

	[Required]
	public AssetViewModel? PreferredAsset
	{
		get => this.documentViewModel.GetAsset(this.preferredAssetId);
		set => this.SetProperty(ref this.preferredAssetId, value?.Id);
	}

	protected override void ApplyToCore(Configuration model)
	{
		model.PreferredAssetId = this.preferredAssetId ?? 0;
	}

	protected override void CopyFromCore(Configuration model)
	{
		this.preferredAssetId = model.PreferredAssetId;
		this.OnPropertyChanged(nameof(this.PreferredAsset));
	}
}
