// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class TransactionViewModel : EntityViewModel<Transaction>
{
	public TransactionViewModel(AccountViewModel thisAccount, Transaction model)
		: base(thisAccount.MoneyFile, model)
	{
		this.ThisAccount = thisAccount;
	}

	/// <summary>
	/// Gets the account this transaction was created to be displayed within.
	/// </summary>
	public AccountViewModel ThisAccount { get; }
}
