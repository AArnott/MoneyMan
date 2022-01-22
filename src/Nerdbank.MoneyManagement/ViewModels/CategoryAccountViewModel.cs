// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class CategoryAccountViewModel : AccountViewModel
{
	public CategoryAccountViewModel(Account? model, DocumentViewModel documentViewModel)
		: base(model ?? new Account { Type = Account.AccountType.Category }, documentViewModel)
	{
		ThrowOnUnexpectedAccountType(nameof(model), Account.AccountType.Category, this.Model.Type);
		this.Type = Account.AccountType.Category;
		this.CopyFrom(this.Model);
	}

	public override string? TransferTargetName => this.Name;

	protected override bool IsEmpty => this.MoneyFile.IsAccountInUse(this.Id);

	protected override bool IsPopulated => throw new NotImplementedException();

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
	}

	protected override void RemoveTransactionFromViewModel(TransactionViewModel transaction) => throw new NotSupportedException();
}
