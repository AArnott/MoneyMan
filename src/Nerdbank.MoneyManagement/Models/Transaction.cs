// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Describes a deposit, withdrawal, or transfer regarding one or two accounts.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Transaction : ModelBase
{
	public Transaction()
	{
	}

	internal Transaction(TransactionAndEntry transactionAndEntry)
	{
		this.Id = transactionAndEntry.TransactionId;
		this.When = transactionAndEntry.When;
		this.Action = transactionAndEntry.Action;
		this.CheckNumber = transactionAndEntry.CheckNumber;
		this.RelatedAssetId = transactionAndEntry.RelatedAssetId;
	}

	/// <summary>
	/// Gets or sets the date the transaction is to be sorted by.
	/// </summary>
	/// <remarks>
	/// The time component and timezone components are to be ignored.
	/// We don't want a change in the user's timezone to change the date that is displayed for a transaction.
	/// </remarks>
	public DateTime When { get; set; }

	/// <summary>
	/// Gets or sets the kind of investment action this represents.
	/// </summary>
	public TransactionAction Action { get; set; }

	/// <summary>
	/// Gets or sets the check number associated with this transaction, if any.
	/// </summary>
	public int? CheckNumber { get; set; }

	/// <summary>
	/// Gets or sets a memo to go with this transaction.
	/// </summary>
	public string? Memo { get; set; }

	/// <summary>
	/// Gets or sets the party receiving or funding this transaction.
	/// </summary>
	public string? Payee { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset" /> that is related to this transaction
	/// but not directly a credited or debited asset.
	/// For example this may be the asset that produced a <see cref="TransactionAction.Dividend"/>.
	/// </summary>
	public int? RelatedAssetId { get; set; }

	private string DebuggerDisplay => $"{this.When} {this.Action}";
}
