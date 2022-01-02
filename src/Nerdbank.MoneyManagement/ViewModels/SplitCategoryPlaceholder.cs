// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class SplitCategoryPlaceholder : AccountViewModel
{
	internal SplitCategoryPlaceholder(DocumentViewModel documentViewModel)
		: base(null, documentViewModel)
	{
		this.Model.Id = -1; // Block accidental persisting of this in-memory representation.
		this.Name = "--split--";
	}

	public override string TransferTargetName => this.Name;

	protected override bool IsEmpty => true;

	protected override bool IsPopulated => false;

	private new string DebuggerDisplay => this.TransferTargetName;

	public override void DeleteTransaction(TransactionViewModel transaction)
	{
		throw new NotImplementedException();
	}

	public override TransactionViewModel? FindTransaction(int? id)
	{
		throw new NotImplementedException();
	}

	internal override void NotifyAccountDeleted(ICollection<int> accountIds)
	{
		throw new NotImplementedException();
	}

	internal override void NotifyTransactionChanged(IReadOnlyList<TransactionAndEntry> transactionAndEntries)
	{
		throw new NotImplementedException();
	}

	protected override void RemoveTransactionFromViewModel(TransactionViewModel transaction)
	{
		throw new NotImplementedException();
	}
}
