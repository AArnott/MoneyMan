// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class InvestingTransaction : ModelBase
{
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
	public InvestmentAction Action { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Account" /> to be credited the <see cref="CreditAmount"/> of this <see cref="Transaction"/>.
	/// </summary>
	public int? CreditAccountId { get; set; }

	/// <summary>
	/// Gets or sets the amount of an asset that was credited to the <see cref="CreditAccountId"/> <see cref="Account"/>.
	/// </summary>
	public decimal? CreditAmount { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> that was credited to the <see cref="CreditAccountId"/> <see cref="Account"/>.
	/// </summary>
	public int? CreditAssetId { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Account" /> to be debited the <see cref="DebitAmount"/> of this <see cref="Transaction"/>.
	/// </summary>
	public int? DebitAccountId { get; set; }

	/// <summary>
	/// Gets or sets the amount of an asset that was debited from the <see cref="DebitAccountId"/> <see cref="Account"/>.
	/// </summary>
	public decimal? DebitAmount { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> that was debited from the <see cref="DebitAccountId"/> <see cref="Account"/>.
	/// </summary>
	public int? DebitAssetId { get; set; }

	/// <summary>
	/// Gets or sets the cash value of the transaction (before fees are taken out).
	/// </summary>
	public decimal ValueInCash { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Asset"/> that was debited from the <see cref="FeeAccountId"/> <see cref="Account"/> as a fee for this transaction.
	/// </summary>
	public int? FeeAssetId { get; set; }

	/// <summary>
	/// Gets or sets the fee charged by the bank, exchange, a transfer fee or otherwise lost from the <see cref="FeeAccountId"/> <see cref="Account"/> as a cost in the transaction.
	/// Measured in units of the asset identified by <see cref="FeeAssetId"/>.
	/// </summary>
	public decimal FeeAmount { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Account" /> to be debited the <see cref="FeeAmount"/> for this <see cref="Transaction"/>.
	/// </summary>
	public int? FeeAccountId { get; set; }

	private string DebuggerDisplay => $"{this.When} {this.Action}";
}
