// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class TransactionEntry : ModelBase
{
	public TransactionEntry()
	{
	}

	public TransactionEntry(TransactionAndEntry te)
	{
		this.TransactionId = te.TransactionId;
		this.Memo = te.TransactionEntryMemo;
		this.AccountId = te.AccountId;
		this.Amount = te.Amount;
		this.AssetId = te.AssetId;
		this.Cleared = te.Cleared;
	}

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Transaction"/> that this entry belongs to.
	/// </summary>
	public int TransactionId { get; set; }

	/// <summary>
	/// Gets or sets a memo to go with this transaction.
	/// </summary>
	public string? Memo { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Account" /> to be credited the <see cref="Amount"/> of this <see cref="Transaction"/>.
	/// </summary>
	public int AccountId { get; set; }

	/// <summary>
	/// Gets or sets the amount of an asset that was credited to the <see cref="AccountId"/> <see cref="Account"/>.
	/// </summary>
	/// <remarks>
	/// This value should be 0 for split transactions, allowing their split members to have non-zero Amounts that contribute to the account balance.
	/// </remarks>
	public decimal Amount { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> that was credited to the <see cref="AccountId"/> <see cref="Account"/>.
	/// </summary>
	public int AssetId { get; set; }

	/// <summary>
	/// Gets or sets the cleared or reconciled state of the credit half of this transaction.
	/// </summary>
	public ClearedState Cleared { get; set; }

	private string DebuggerDisplay => $"{this.Amount}";
}
