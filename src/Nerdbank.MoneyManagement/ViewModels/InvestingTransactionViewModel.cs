// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Nerdbank.MoneyManagement.ViewModels;

public class InvestingTransactionViewModel : EntityViewModel<InvestingTransaction>
{
	private DateTime when;
	private InvestmentAction? action;
	private AccountViewModel? creditAccount;
	private AccountViewModel? debitAccount;
	private AssetViewModel? creditAsset;
	private AssetViewModel? debitAsset;
	private decimal? creditAmount;
	private decimal? debitAmount;

	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount, InvestingTransaction? transaction)
		: base(thisAccount.MoneyFile)
	{
		this.ThisAccount = thisAccount;
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
		set
		{
			if (this.action != value)
			{
				this.SetProperty(ref this.action, value);
				if (value.HasValue)
				{
					switch (value.Value)
					{
						case InvestmentAction.Buy:
							break;
						case InvestmentAction.Sell:
							break;
						case InvestmentAction.Exchange:
							break;
						case InvestmentAction.Remove:
						case InvestmentAction.Withdraw:
							this.CreditAccount = null;
							this.DebitAccount = this.ThisAccount;
							break;
						case InvestmentAction.AdjustShareBalance:
							break;
						case InvestmentAction.Interest:
						case InvestmentAction.Add:
						case InvestmentAction.Deposit:
						case InvestmentAction.Dividend:
							this.CreditAccount = this.ThisAccount;
							this.DebitAccount = null;
							break;
					}
				}
			}
		}
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

	/// <summary>
	/// Gets the account this transaction was created to be displayed within.
	/// </summary>
	public InvestingAccountViewModel ThisAccount { get; }

	public AssetViewModel? CreditAsset
	{
		get => this.creditAsset;
		set => this.SetProperty(ref this.creditAsset, value);
	}

	public decimal? CreditAmount
	{
		get => this.creditAmount;
		set => this.SetProperty(ref this.creditAmount, value);
	}

	public AccountViewModel? CreditAccount
	{
		get => this.creditAccount;
		set => this.SetProperty(ref this.creditAccount, value);
	}

	public AssetViewModel? DebitAsset
	{
		get => this.debitAsset;
		set => this.SetProperty(ref this.debitAsset, value);
	}

	public decimal? DebitAmount
	{
		get => this.debitAmount;
		set => this.SetProperty(ref this.debitAmount, value);
	}

	public AccountViewModel? DebitAccount
	{
		get => this.debitAccount;
		set => this.SetProperty(ref this.debitAccount, value);
	}

	protected override void ApplyToCore(InvestingTransaction model)
	{
		model.When = this.When;
		model.Action = this.Action!.Value;

		model.CreditAccountId = this.CreditAccount?.Id;
		model.CreditAssetId = this.CreditAsset?.Id;
		model.CreditAmount = this.CreditAmount;

		model.DebitAccountId = this.DebitAccount?.Id;
		model.DebitAssetId = this.DebitAsset?.Id;
		model.DebitAmount = this.DebitAmount;
	}

	protected override void CopyFromCore(InvestingTransaction model)
	{
		this.When = model.When;
		this.Action = model.Action;

		this.CreditAccount = this.ThisAccount.DocumentViewModel.GetAccount(model.CreditAccountId);
		this.CreditAmount = model.CreditAmount;
		this.CreditAsset = this.ThisAccount.DocumentViewModel.GetAsset(model.CreditAssetId);

		this.DebitAccount = this.ThisAccount.DocumentViewModel.GetAccount(model.DebitAccountId);
		this.DebitAsset = this.ThisAccount.DocumentViewModel.GetAsset(model.DebitAssetId);
		this.DebitAmount = model.DebitAmount;
	}
}
