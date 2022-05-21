// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MoneyManagement;

public record struct Amount
{
	public Amount(decimal value, int assetId)
	{
		Requires.Range(value >= 0, nameof(value));
		Requires.Range(assetId > 0, nameof(assetId));

		this.Value = value;
		this.AssetId = assetId;
	}

	public decimal Value { get; init; }

	public int AssetId { get; init; }

	public (decimal Value, int AssetId) Deconstruct() => (this.Value, this.AssetId);
}
