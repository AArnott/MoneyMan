// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// A price point for an <see cref="Asset"/>.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public record AssetPrice : ModelBase
{
	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> being described.
	/// </summary>
	public int AssetId { get; set; }

	/// <summary>
	/// Gets or sets the date that this entity records a price point for.
	/// </summary>
	public DateTime When { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> used as a currency to describe the price of the asset referenced by <see cref="AssetId"/>.
	/// </summary>
	public int ReferenceAssetId { get; set; }

	/// <summary>
	/// Gets or sets the price of the <see cref="Asset"/> identified by <see cref="AssetId"/>.
	/// </summary>
	/// <value>The price of the asset, as measured in units of the <see cref="Asset"/> identified by <see cref="ReferenceAssetId"/>.</value>
	public decimal PriceInReferenceAsset { get; set; }

	private string? DebuggerDisplay => $"{this.When} {this.PriceInReferenceAsset}";
}
