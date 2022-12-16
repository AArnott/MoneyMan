// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

internal record UnsoldAsset
{
	public DateTime AcquiredDate { get; set; }

	public int TransactionId { get; set; }

	public int AssetId { get; set; }

	public int TaxLotId { get; set; }

	public string? AssetName { get; set; }

	public decimal AcquiredAmount { get; set; }

	public decimal RemainingAmount { get; set; }
}
