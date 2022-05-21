// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public record TransactionAndEntry
{
	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="Transaction"/> this belongs to.
	/// </summary>
	public int TransactionId { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the account that is considered the context for this transaction-entry tuple.
	/// </summary>
	public int? ContextAccountId { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ModelBase.Id"/> of the <see cref="TransactionEntry"/> this belongs to.
	/// </summary>
	public int TransactionEntryId { get; set; }

	/// <inheritdoc cref="TransactionEntry.OfxFitId"/>
	public string? OfxFitId { get; set; }

	/// <inheritdoc cref="Transaction.When"/>
	public DateTime When { get; set; }

	/// <inheritdoc cref="Transaction.Action"/>
	public TransactionAction Action { get; set; }

	/// <inheritdoc cref="Transaction.CheckNumber"/>
	public int? CheckNumber { get; set; }

	/// <inheritdoc cref="Transaction.RelatedAssetId"/>
	public int? RelatedAssetId { get; set; }

	/// <inheritdoc cref="Transaction.Memo"/>
	public string? TransactionMemo { get; set; }

	/// <inheritdoc cref="TransactionEntry.Memo"/>
	public string? TransactionEntryMemo { get; set; }

	/// <inheritdoc cref="Transaction.Payee"/>
	public string? Payee { get; set; }

	/// <inheritdoc cref="TransactionEntry.AccountId"/>
	public int AccountId { get; set; }

	/// <inheritdoc cref="TransactionEntry.Amount"/>
	public decimal Amount { get; set; }

	/// <inheritdoc cref="TransactionEntry.AssetId"/>
	public int AssetId { get; set; }

	/// <inheritdoc cref="TransactionEntry.Cleared"/>
	public ClearedState Cleared { get; set; }

	private string DebuggerDisplay => $"{this.When} {this.Action}";
}
