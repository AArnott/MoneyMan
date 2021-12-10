// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class AssetViewModel : EntityViewModel<Asset>
{
	private string name = string.Empty;
	private Asset.AssetType type;
	private decimal currentPrice;
	private bool? typeIsReadOnly;

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

	/// <inheritdoc cref="Asset.Type"/>
	public Asset.AssetType Type
	{
		get => this.type;
		set
		{
			if (this.type != value)
			{
				Verify.Operation(!this.TypeIsReadOnly, "This property is read only.");
				this.SetProperty(ref this.type, value);
			}
		}
	}

	/// <summary>
	/// Gets a value indicating whether <see cref="Type"/> can be changed.
	/// </summary>
	public bool TypeIsReadOnly => this.typeIsReadOnly ??= this.Id is not null && this.MoneyFile?.IsAssetInUse(this.Id.Value) is true;

	public ReadOnlyCollection<EnumValueViewModel<Asset.AssetType>> Types { get; } = new ReadOnlyCollection<EnumValueViewModel<Asset.AssetType>>(new EnumValueViewModel<Asset.AssetType>[]
	{
		new(Asset.AssetType.Currency, "Currency"),
		new(Asset.AssetType.Security, "Security"),
	});

	public decimal CurrentPrice
	{
		get => this.currentPrice;
		set => this.SetProperty(ref this.currentPrice, value);
	}

	protected string? DebuggerDisplay => this.Name;

	internal void NotifyUseChange()
	{
		if (this.typeIsReadOnly.HasValue)
		{
			this.typeIsReadOnly = null;
			this.OnPropertyChanged(nameof(this.TypeIsReadOnly));
		}
	}

	protected override bool IsPersistedProperty(string propertyName) => propertyName is not nameof(this.TypeIsReadOnly);

	protected override void ApplyToCore(Asset model)
	{
		model.Name = this.Name;
		model.Type = this.Type;
	}

	protected override void CopyFromCore(Asset model)
	{
		this.Name = model.Name;
		this.Type = model.Type;
	}
}
