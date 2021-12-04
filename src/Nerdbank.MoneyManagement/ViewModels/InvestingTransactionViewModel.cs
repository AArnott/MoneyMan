// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
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

	/// <summary>
	/// Gets a collection of <see cref="InvestmentAction"/> that may be set to <see cref="Action"/>,
	/// with human-readable captions.
	/// </summary>
	public ReadOnlyCollection<EnumValueViewModel<InvestmentAction>> Actions { get; } = new ReadOnlyCollection<EnumValueViewModel<InvestmentAction>>(new EnumValueViewModel<InvestmentAction>[]
	{
		new(InvestmentAction.Buy, nameof(InvestmentAction.Buy)),
		new(InvestmentAction.Sell, nameof(InvestmentAction.Sell)),
		new(InvestmentAction.Exchange, nameof(InvestmentAction.Exchange)),
		new(InvestmentAction.Dividend, nameof(InvestmentAction.Dividend)),
		new(InvestmentAction.Interest, nameof(InvestmentAction.Interest)),
		new(InvestmentAction.Add, nameof(InvestmentAction.Add)),
		new(InvestmentAction.Remove, nameof(InvestmentAction.Remove)),
		new(InvestmentAction.AdjustShareBalance, nameof(InvestmentAction.AdjustShareBalance)),
		new(InvestmentAction.Deposit, nameof(InvestmentAction.Deposit)),
		new(InvestmentAction.Withdraw, nameof(InvestmentAction.Withdraw)),
	});

	protected override void ApplyToCore(InvestingTransaction model)
	{
		model.When = this.When;
		model.Action = this.Action!.Value;
	}

	protected override void CopyFromCore(InvestingTransaction model)
	{
		this.When = model.When;
		this.Action = model.Action;
	}
}
