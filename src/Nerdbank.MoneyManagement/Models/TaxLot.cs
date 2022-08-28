// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement.Models;

/// <summary>
/// An entity to track tax lot information.
/// </summary>
/// <seealso cref="TaxLotAssignment"/>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public record TaxLot : ModelBase
{
	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="TransactionEntry"/>
	/// that is responsible for creating this tax lot.
	/// </summary>
	public int CreatingTransactionEntryId { get; set; }

	/// <summary>
	/// Gets or sets the date this lot was acquired for tax reporting purposes.
	/// When <see langword="null"/>, the date on the transaction should be used.
	/// </summary>
	public DateTime? AcquiredDate { get; set; }

	private string? DebuggerDisplay => $"TaxLot: {this.AcquiredDate} {this.CreatingTransactionEntryId}";
}
