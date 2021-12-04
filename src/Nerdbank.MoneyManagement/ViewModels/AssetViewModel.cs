﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public class AssetViewModel : EntityViewModel<Asset>
{
	private string name = string.Empty;
	private decimal currentPrice;

	public AssetViewModel()
		: this(null, null)
	{
	}

	public AssetViewModel(Asset? model, MoneyFile? moneyFile)
		: base(moneyFile)
	{
		this.AutoSave = true;

		if (model is object)
		{
			this.CopyFrom(model);
		}
	}

	/// <inheritdoc cref="Asset.Name"/>
	[Required]
	public string Name
	{
		get => this.name;
		set
		{
			Requires.NotNull(value, nameof(value));
			this.SetProperty(ref this.name, value);
		}
	}

	public decimal CurrentPrice
	{
		get => this.currentPrice;
		set => this.SetProperty(ref this.currentPrice, value);
	}

	protected override void ApplyToCore(Asset model)
	{
		model.Name = this.Name;
	}

	protected override void CopyFromCore(Asset model)
	{
		this.Name = model.Name;
	}
}