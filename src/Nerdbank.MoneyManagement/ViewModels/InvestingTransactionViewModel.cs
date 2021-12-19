// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MoneyManagement.ViewModels;

public class InvestingTransactionViewModel : TransactionViewModel
{
	private DateTime when;
	private TransactionAction? action;
	private AccountViewModel? creditAccount;
	private AccountViewModel? debitAccount;
	private AssetViewModel? creditAsset;
	private AssetViewModel? debitAsset;
	private AssetViewModel? relatedAsset;
	private decimal? creditAmount;
	private decimal? debitAmount;
	private string? memo;

	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount, Transaction? transaction)
		: base(thisAccount)
	{
		this.RegisterDependentProperty(nameof(this.CreditAmount), nameof(this.SimpleAmount));
		this.RegisterDependentProperty(nameof(this.DebitAmount), nameof(this.SimpleAmount));
		this.RegisterDependentProperty(nameof(this.CreditAsset), nameof(this.SimpleAsset));
		this.RegisterDependentProperty(nameof(this.DebitAsset), nameof(this.SimpleAsset));
		this.RegisterDependentProperty(nameof(this.CreditAmount), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.DebitAmount), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.CreditAsset), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.DebitAsset), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.CreditAmount), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.DebitAmount), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Assets));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimplePriceApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimpleAssetApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.CreditAmount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.CreditAsset), nameof(this.Description));

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
							this.CreditAccount = this.ThisAccount;
							this.DebitAccount = null;
							this.DebitAmount = null;
							this.DebitAsset = null;
							break;
						case TransactionAction.Dividend:
							this.CreditAccount = this.ThisAccount;
							this.CreditAsset = this.ThisAccount.CurrencyAsset;
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

	/// <inheritdoc cref="TransactionViewModel.ThisAccount"/>
	public new InvestingAccountViewModel ThisAccount => (InvestingAccountViewModel)base.ThisAccount;

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

	/// <inheritdoc cref="Transaction.RelatedAssetId"/>
	public AssetViewModel? RelatedAsset
	{
		get => this.relatedAsset;
		set => this.SetProperty(ref this.relatedAsset, value);
	}

	public decimal? SimpleAmount
	{
		get => this.IsCreditOperation ? this.CreditAmount : this.IsDebitOperation ? this.DebitAmount : null;
		set
		{
			if (this.IsCreditOperation)
			{
				this.CreditAmount = value;
			}
			else if (this.IsDebitOperation)
			{
				this.DebitAmount = value;
			}
			else
			{
				ThrowNotSimpleAction();
			}
		}
	}

	public AssetViewModel? SimpleAsset
	{
		get =>
			this.Action == TransactionAction.Dividend ? this.RelatedAsset :
			this.IsCreditOperation ? this.CreditAsset :
			this.IsDebitOperation ? this.DebitAsset :
			null;
		set
		{
			if (this.Action == TransactionAction.Dividend)
			{
				this.RelatedAsset = value;
			}
			else if (this.Action is TransactionAction.Buy or TransactionAction.Add)
			{
				this.CreditAsset = value;
			}
			else if (this.Action is TransactionAction.Sell or TransactionAction.Remove)
			{
				this.DebitAsset = value;
			}
			else
			{
				ThrowNotSimpleAction();
			}
		}
	}

	public bool IsSimpleAssetApplicable => this.Action is TransactionAction.Dividend or TransactionAction.Buy or TransactionAction.Sell or TransactionAction.Add or TransactionAction.Remove;

	public decimal? SimplePrice
	{
		get
		{
			if (this.Action is TransactionAction.Add or TransactionAction.Buy)
			{
				return this.CreditAmount != 0 && this.DebitAmount != 0 ? this.DebitAmount / this.CreditAmount : null;
			}
			else if (this.Action is TransactionAction.Sell)
			{
				return this.CreditAmount != 0 && this.DebitAmount != 0 ? this.CreditAmount / this.DebitAmount : null;
			}
			else
			{
				return null;
			}
		}

		set
		{
			if (this.Action is TransactionAction.Buy)
			{
				this.DebitAmount = value * this.CreditAmount;
			}
			else if (this.Action is TransactionAction.Sell)
			{
				this.CreditAmount = value * this.DebitAmount;
			}
			else
			{
				throw ThrowNotSimpleAction();
			}
		}
	}

	public bool IsSimplePriceApplicable => this.Action is TransactionAction.Buy or TransactionAction.Sell;

	public decimal? SimpleCurrencyImpact
	{
		get
		{
			if (this.Action == TransactionAction.Dividend && this.CreditAsset == this.ThisAccount.CurrencyAsset)
			{
				return this.CreditAmount;
			}

			decimal impact = 0;
			if (this.DebitAsset == this.ThisAccount.CurrencyAsset && this.DebitAmount.HasValue)
			{
				impact -= this.DebitAmount.Value;
			}

			if (this.CreditAsset == this.ThisAccount.CurrencyAsset && this.CreditAmount.HasValue)
			{
				impact += this.CreditAmount.Value;
			}

			return impact;
		}
	}

	public string? Memo
	{
		get => this.memo;
		set => this.SetProperty(ref this.memo, value);
	}

	public string Description
	{
		get
		{
			return this.Action switch
			{
				TransactionAction.Add => $"{this.CreditAmount} {this.CreditAsset?.TickerOrName}",
				TransactionAction.Remove => $"{this.DebitAmount} {this.DebitAsset?.TickerOrName}",
				TransactionAction.Interest => $"+{this.CreditAmount:C}",
				TransactionAction.Dividend => $"{this.RelatedAsset?.TickerOrName} +{this.CreditAmount:C}",
				TransactionAction.Sell => $"{this.DebitAmount} {this.DebitAsset?.TickerOrName} @ {this.SimplePrice:C}",
				TransactionAction.Buy => $"{this.CreditAmount} {this.CreditAsset?.TickerOrName} @ {this.SimplePrice:C}",
				TransactionAction.Transfer => this.ThisAccount == this.CreditAccount ? $"+{this.CreditAmount} {this.CreditAsset?.TickerOrName}" : $"-{this.DebitAmount} {this.DebitAsset?.TickerOrName}",
				TransactionAction.Deposit => $"{this.CreditAmount:C}",
				TransactionAction.Withdraw => $"{this.DebitAmount:C}",
				TransactionAction.Exchange => $"{this.DebitAmount} {this.DebitAsset?.TickerOrName} -> {this.CreditAmount} {this.CreditAsset?.TickerOrName}",
				_ => string.Empty,
			};
		}
	}

	public IEnumerable<AssetViewModel> Assets => this.ThisAccount.DocumentViewModel.AssetsPanel.Assets.Where(a => this.IsCashTransaction == (a.Type == Asset.AssetType.Currency));

	private bool IsCreditOperation => this.Action is TransactionAction.Buy or TransactionAction.Add or TransactionAction.Deposit or TransactionAction.Interest or TransactionAction.Dividend;

	private bool IsDebitOperation => this.Action is TransactionAction.Sell or TransactionAction.Remove or TransactionAction.Withdraw;

	private bool IsCashTransaction => this.Action is TransactionAction.Deposit or TransactionAction.Withdraw;

	protected override void ApplyToCore(Transaction model)
	{
		model.When = this.When;
		model.Memo = this.Memo;
		model.Action = this.Action!.Value;

		model.CreditAccountId = this.CreditAccount?.Id;
		model.CreditAssetId = this.CreditAsset?.Id;
		model.CreditAmount = this.CreditAmount;

		model.DebitAccountId = this.DebitAccount?.Id;
		model.DebitAssetId = this.DebitAsset?.Id;
		model.DebitAmount = this.DebitAmount;

		model.RelatedAssetId = this.RelatedAsset?.Id;
	}

	protected override void CopyFromCore(Transaction model)
	{
		this.When = model.When;
		this.Memo = model.Memo;
		this.SetProperty(ref this.action, model.Action, nameof(this.Action));

		this.CreditAccount = this.ThisAccount.DocumentViewModel.GetAccount(model.CreditAccountId);
		this.CreditAmount = model.CreditAmount;
		this.CreditAsset = this.ThisAccount.DocumentViewModel.GetAsset(model.CreditAssetId);

		this.DebitAccount = this.ThisAccount.DocumentViewModel.GetAccount(model.DebitAccountId);
		this.DebitAsset = this.ThisAccount.DocumentViewModel.GetAsset(model.DebitAssetId);
		this.DebitAmount = model.DebitAmount;

		this.RelatedAsset = this.ThisAccount.DocumentViewModel.GetAsset(model.RelatedAssetId);
	}

	protected override bool IsPersistedProperty(string propertyName) =>
		base.IsPersistedProperty(propertyName) && propertyName is not (nameof(this.SimpleAsset) or nameof(this.SimpleAmount) or nameof(this.SimplePrice) or nameof(this.SimpleCurrencyImpact));

	[DoesNotReturn]
	private static Exception ThrowNotSimpleAction() => throw new InvalidOperationException("Not a simple operation.");
}
