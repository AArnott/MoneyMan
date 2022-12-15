// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

internal record ConsumedTaxLot
{
	public int TaxLotId { get; set; }

	public int ConsumingTransactionEntryId { get; set; }

	public DateTime AcquiredDate { get; set; }

	public decimal Amount { get; set; }

	public decimal CostBasisAmount { get; set; }

	public int CostBasisAssetId { get; set; }
}
