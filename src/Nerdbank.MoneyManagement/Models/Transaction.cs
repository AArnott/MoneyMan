// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using SQLite;
using Validation;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Describes a deposit, withdrawal, or transfer regarding one or two accounts.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Transaction : ModelBase
{
	private decimal? creditAmount;
	private decimal? debitAmount;

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
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Category"/> assigned to this transaction.
	/// </summary>
	/// <remarks>
	/// Use <see cref="Category.Split"/> for the value where the transaction is split across multiple categories.
	/// </remarks>
	public int? CategoryId { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Account" /> to be credited the <see cref="CreditAmount"/> of this <see cref="Transaction"/>.
	/// </summary>
	public int? CreditAccountId { get; set; }

	/// <summary>
	/// Gets or sets the amount of an asset that was credited to the <see cref="CreditAccountId"/> <see cref="Account"/>.
	/// </summary>
	/// <remarks>
	/// This value should be 0 for split transactions, allowing their split members to have non-zero Amounts that contribute to the account balance.
	/// </remarks>
	public decimal? CreditAmount
	{
		get => this.creditAmount;
		set
		{
			Requires.Range(value is null or >= 0, nameof(value));
			this.creditAmount = value;
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> that was credited to the <see cref="CreditAccountId"/> <see cref="Account"/>.
	/// </summary>
	public int? CreditAssetId { get; set; }

	/// <summary>
	/// Gets or sets the cleared or reconciled state of the credit half of this transaction.
	/// </summary>
	public ClearedState CreditCleared { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Account" /> to be debited the <see cref="DebitAmount"/> of this <see cref="Transaction"/>.
	/// </summary>
	public int? DebitAccountId { get; set; }

	/// <summary>
	/// Gets or sets the amount of an asset that was debited from the <see cref="DebitAccountId"/> <see cref="Account"/>.
	/// </summary>
	/// <remarks>
	/// This value should be 0 for split transactions, allowing their split members to have non-zero Amounts that contribute to the account balance.
	/// </remarks>
	public decimal? DebitAmount
	{
		get => this.debitAmount;
		set
		{
			Requires.Range(value is null or >= 0, nameof(value));
			this.debitAmount = value;
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> that was debited from the <see cref="DebitAccountId"/> <see cref="Account"/>.
	/// </summary>
	public int? DebitAssetId { get; set; }

	/// <summary>
	/// Gets or sets the cleared or reconciled state of the debit half of this transaction.
	/// </summary>
	public ClearedState DebitCleared { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of a <em>split</em> <see cref="Transaction"/>
	/// that this transaction is a member of.
	/// </summary>
	[Indexed]
	public int? ParentTransactionId { get; set; }

	private string DebuggerDisplay => $"{this.When} {this.Action} {this.Payee} (+{this.CreditAmount}/-{this.DebitAmount})";
}
