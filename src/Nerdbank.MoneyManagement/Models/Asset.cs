// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// An asset, which may be a share of a company, a car, a cryptocurrency, etc.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Asset : ModelBase
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

	private string? DebuggerDisplay => this.Name;
}
