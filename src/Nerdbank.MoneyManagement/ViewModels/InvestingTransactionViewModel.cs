// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Nerdbank.MoneyManagement.ViewModels;

public class InvestingTransactionViewModel : EntityViewModel<InvestingTransaction>
{
	private DateTime when;
	private InvestmentAction? action;

	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount, InvestingTransaction? transaction)
		: base(thisAccount.MoneyFile)
	{
		this.AutoSave = true;

		if (transaction is object)
		{
			this.CopyFrom(transaction);
		}
	}

	/// <inheritdoc cref="InvestingTransaction.When"/>
	public DateTime When
	{
		get => this.when;
		set => this.SetProperty(ref this.when, value);
	}

	/// <inheritdoc cref="InvestingTransaction.Action"/>
	[Required]
	public InvestmentAction? Action
	{
		get => this.action;
		set => this.SetProperty(ref this.action, value);
	}

	protected override void ApplyToCore(InvestingTransaction model)
	{
	}

	protected override void CopyFromCore(InvestingTransaction model)
	{
	}
}
