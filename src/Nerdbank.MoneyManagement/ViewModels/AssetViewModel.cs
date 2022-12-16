// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class AssetViewModel : EntityViewModel<Asset>, ISelectableView
{
	private readonly DocumentViewModel documentViewModel;
	private string name = string.Empty;
	private string tickerSymbol = string.Empty;
	private Asset.AssetType type;
	private decimal currentPrice;
	private bool? typeIsReadOnly;
	private int? decimalDigits;
	private string? currencySymbol;
	private NumberFormatInfo? numberFormat;

	public AssetViewModel(Asset model, DocumentViewModel documentViewModel)
		: base(documentViewModel.MoneyFile, model)
	{
		this.RegisterDependentProperty(nameof(this.TickerSymbol), nameof(this.TickerOrName));
		this.RegisterDependentProperty(nameof(this.Name), nameof(this.TickerOrName));
		this.RegisterDependentProperty(nameof(this.CurrencySymbol), nameof(this.NumberFormat));
		this.RegisterDependentProperty(nameof(this.DecimalDigits), nameof(this.NumberFormat));
		this.RegisterDependentProperty(nameof(this.CurrentPrice), nameof(this.CurrentPriceFormatted));

		this.documentViewModel = documentViewModel;
		this.CopyFrom(this.Model);
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

	/// <inheritdoc cref="Asset.TickerSymbol"/>
	public string TickerSymbol
	{
		get => this.tickerSymbol;
		set => this.SetProperty(ref this.tickerSymbol, value);
	}

	/// <summary>
	/// Gets the value of <see cref="TickerSymbol"/> if non-empty; otherwise the value of <see cref="Name"/>.
	/// </summary>
	public string TickerOrName => string.IsNullOrEmpty(this.TickerSymbol) ? this.Name : this.TickerSymbol;

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
	public bool TypeIsReadOnly => this.typeIsReadOnly ??= this.MoneyFile.IsAssetInUse(this.Id) is true;

	/// <inheritdoc cref="Asset.CurrencySymbol"/>
	public string? CurrencySymbol
	{
		get => this.currencySymbol;
		set
		{
			if (this.currencySymbol != value)
			{
				this.SetProperty(ref this.currencySymbol, value);
				this.numberFormat = null;
			}
		}
	}

	/// <inheritdoc cref="Asset.DecimalDigits"/>
	public int? DecimalDigits
	{
		get => this.decimalDigits;
		set
		{
			if (this.decimalDigits != value)
			{
				this.SetProperty(ref this.decimalDigits, value);
				this.numberFormat = null;
			}
		}
	}

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

	public string? CurrentPriceFormatted => this.documentViewModel?.DefaultCurrency?.Format(this.CurrentPrice);

	internal NumberFormatInfo NumberFormat
	{
		get
		{
			if (this.numberFormat is { } format)
			{
				return format;
			}

			NumberFormatInfo numberFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
			if (this.CurrencySymbol is object)
			{
				numberFormat.CurrencySymbol = this.CurrencySymbol;
			}

			numberFormat.CurrencyDecimalDigits = this.DecimalDigits ?? 0;
			numberFormat.NumberDecimalDigits = this.DecimalDigits ?? 0;

			return this.numberFormat = numberFormat;
		}
	}

	protected string? DebuggerDisplay => this.Name;

	[return: NotNullIfNotNull("value")]
	public string? Format(decimal? value)
	{
		return this.Type switch
		{
			Asset.AssetType.Security => TrimPrecision(value?.ToString("N", this.NumberFormat)),
			Asset.AssetType.Currency => value?.ToString("C", this.NumberFormat),
			_ => throw new NotSupportedException($"Unsupported type: {this.Type}"),
		};

		string? TrimPrecision(string? value)
		{
			if (this.DecimalDigits > 0 && value is not null)
			{
				value = value.TrimEnd('0');
				if (value.EndsWith(this.NumberFormat.NumberDecimalSeparator, StringComparison.CurrentCulture))
				{
					value = value.Substring(0, value.Length - this.NumberFormat.NumberDecimalSeparator.Length);
				}
			}

			return value;
		}
	}

	void ISelectableView.Select()
	{
		this.documentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Assets;
		this.documentViewModel.AssetsPanel.SelectedAsset = this.documentViewModel.AssetsPanel.FindAsset(this.Id);
	}

	internal void NotifyUseChange()
	{
		if (this.typeIsReadOnly.HasValue)
		{
			this.typeIsReadOnly = null;
			this.OnPropertyChanged(nameof(this.TypeIsReadOnly));
		}
	}

	protected override bool IsPersistedProperty(string propertyName) => propertyName is not (nameof(this.TypeIsReadOnly) or nameof(this.TickerOrName));

	protected override void ApplyToCore()
	{
		this.Model.Name = this.Name;
		this.Model.TickerSymbol = string.IsNullOrWhiteSpace(this.TickerSymbol) ? null : this.TickerSymbol;
		this.Model.Type = this.Type;
		this.Model.CurrencySymbol = this.CurrencySymbol;
		this.Model.DecimalDigits = this.DecimalDigits;
	}

	protected override void CopyFromCore()
	{
		this.Name = this.Model.Name;
		this.TickerSymbol = this.Model.TickerSymbol ?? string.Empty;
		this.SetProperty(ref this.type, this.Model.Type);
		this.CurrencySymbol = this.Model.CurrencySymbol;
		this.DecimalDigits = this.Model.DecimalDigits;
	}
}
