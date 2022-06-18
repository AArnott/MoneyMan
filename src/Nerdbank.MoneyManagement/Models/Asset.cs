// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// An asset, which may be a share of a company, a car, a cryptocurrency, etc.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public record Asset : ModelBase
{
	public enum AssetType
	{
		/// <summary>
		/// This asset represents a fiat currency.
		/// </summary>
		Currency = 0,

		/// <summary>
		/// This asset represents some security such as a stock or cryptocurrency.
		/// </summary>
		Security,
	}

	/// <summary>
	/// Gets or sets the name of this asset.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the ticker symbol for this asset.
	/// </summary>
	public string? TickerSymbol { get; set; }

	/// <summary>
	/// Gets or sets the type of asset.
	/// </summary>
	public AssetType Type { get; set; }

	/// <summary>
	/// Gets or sets the value to use for <see cref="NumberFormatInfo.CurrencySymbol"/>.
	/// Applicable when <see cref="Type"/> is set to <see cref="AssetType.Currency"/>.
	/// </summary>
	public string? CurrencySymbol { get; set; }

	/// <summary>
	/// Gets or sets the value to use for <see cref="NumberFormatInfo.CurrencyDecimalDigits"/> or <see cref="NumberFormatInfo.NumberDecimalDigits"/>.
	/// </summary>
	[SQLite.Column("CurrencyDecimalDigits")] // db schema compatibility with old property name
	public int? DecimalDigits { get; set; }

	private string? DebuggerDisplay => string.IsNullOrEmpty(this.TickerSymbol) ? this.Name : $"{this.Name} ({this.TickerSymbol})";
}
