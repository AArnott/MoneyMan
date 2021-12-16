// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Nerdbank.MoneyManagement.ViewModels;

public class InvestingTransactionViewModel : EntityViewModel<Transaction>
{
	private DateTime when;
	private TransactionAction? action;
	private AccountViewModel? creditAccount;
	private AccountViewModel? debitAccount;
	private AssetViewModel? creditAsset;
	private AssetViewModel? debitAsset;
	private decimal? creditAmount;
	private decimal? debitAmount;

	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount, Transaction? transaction)
		: base(thisAccount.MoneyFile)
	{
		this.ThisAccount = thisAccount;
		this.AutoSave = true;

		if (transaction is object)
		{
			this.CopyFrom(transaction);
		}
	}

	/// <inheritdoc cref="Transaction.When"/>
	public DateTime When
	{
		get => this.when;
		set => this.SetProperty(ref this.when, value);
	}

	/// <inheritdoc cref="Transaction.Action"/>
	[Required, NonZero]
	public TransactionAction? Action
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
						case TransactionAction.Buy:
							this.CreditAccount ??= this.ThisAccount;
							this.DebitAccount ??= this.ThisAccount;
							this.DebitAsset ??= this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Sell:
							this.CreditAccount ??= this.ThisAccount;
							this.DebitAccount ??= this.ThisAccount;
							this.CreditAsset ??= this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Exchange:
							this.CreditAccount ??= this.ThisAccount;
							this.DebitAccount ??= this.ThisAccount;
							break;
						case TransactionAction.Remove:
							this.CreditAccount = null;
							this.CreditAmount = null;
							this.CreditAsset = null;
							this.DebitAccount = this.ThisAccount;
							break;
						case TransactionAction.Withdraw:
							this.CreditAccount = null;
							this.CreditAmount = null;
							this.CreditAsset = null;
							this.DebitAccount = this.ThisAccount;
							this.DebitAsset = this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Interest:
						case TransactionAction.Deposit:
							this.CreditAccount = this.ThisAccount;
							this.CreditAsset = this.ThisAccount.CurrencyAsset;
							this.DebitAccount = null;
							this.DebitAmount = null;
							this.DebitAsset = null;
							break;
						case TransactionAction.Add:
						case TransactionAction.Dividend:
							this.CreditAccount = this.ThisAccount;
							this.DebitAccount = null;
							this.DebitAmount = null;
							this.DebitAsset = null;
							break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Gets a collection of <see cref="TransactionAction"/> that may be set to <see cref="Action"/>,
	/// with human-readable captions.
	/// </summary>
	public ReadOnlyCollection<EnumValueViewModel<TransactionAction>> Actions { get; } = new ReadOnlyCollection<EnumValueViewModel<TransactionAction>>(new EnumValueViewModel<TransactionAction>[]
	{
		new(TransactionAction.Buy, nameof(TransactionAction.Buy)),
		new(TransactionAction.Sell, nameof(TransactionAction.Sell)),
		new(TransactionAction.Exchange, nameof(TransactionAction.Exchange)),
		new(TransactionAction.Dividend, nameof(TransactionAction.Dividend)),
		new(TransactionAction.Interest, nameof(TransactionAction.Interest)),
		new(TransactionAction.Add, nameof(TransactionAction.Add)),
		new(TransactionAction.Remove, nameof(TransactionAction.Remove)),
		new(TransactionAction.Deposit, nameof(TransactionAction.Deposit)),
		new(TransactionAction.Withdraw, nameof(TransactionAction.Withdraw)),
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

	protected override void ApplyToCore(Transaction model)
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

	protected override void CopyFromCore(Transaction model)
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
