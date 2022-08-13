// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement.Models;

/// <summary>
/// A many-to-many table for binding asset purchases to their eventual sales
/// for purposes of tax reporting.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public record TaxLotAssignment : ModelBase
{
	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="TransactionEntry"/>
	/// that acquired the tax lot.
	/// </summary>
	public int AcquiredTransactionEntryId { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="TransactionEntry"/>
	/// that dispensed with some or all of the tax lot.
	/// </summary>
	public int DispenseTransactionEntryId { get; set; }

	/// <summary>
	/// Gets or sets the amount dispensed.
	/// </summary>
	public decimal Amount { get; set; }

	private string? DebuggerDisplay => $"A: {this.AcquiredTransactionEntryId} -> D: {this.DispenseTransactionEntryId}: {this.Amount}";
}
