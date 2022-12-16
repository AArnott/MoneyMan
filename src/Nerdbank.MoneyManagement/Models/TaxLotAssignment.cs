// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// A many-to-many table for <see cref="TransactionEntry"/> entities and the
/// <see cref="TaxLot"/> entities that they consume.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public record TaxLotAssignment : ModelBase
{
	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="TaxLot"/>
	/// that is wholly or partially consumed by this assignment.
	/// </summary>
	public int TaxLotId { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="TransactionEntry"/>
	/// that dispensed with some or all of the tax lot.
	/// </summary>
	public int ConsumingTransactionEntryId { get; set; }

	/// <summary>
	/// Gets or sets the amount dispensed.
	/// </summary>
	public decimal Amount { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the user explicitly chose to consume from this tax lot.
	/// </summary>
	/// <remarks>
	/// This value can be useful when tax lot assignments must be recomputed (e.g. a tax lot was removed entirely)
	/// and we want to preserve the explicit assignments but reorganize the implicit ones.
	/// </remarks>
	public bool Pinned { get; set; }

	private string? DebuggerDisplay => $"TL: {this.TaxLotId} TE: {this.ConsumingTransactionEntryId}: {this.Amount}";
}
