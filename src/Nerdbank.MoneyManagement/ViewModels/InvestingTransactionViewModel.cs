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

	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount, Transaction transaction)
		: base(thisAccount, transaction)
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
		this.RegisterDependentProperty(nameof(this.SimpleAccount), nameof(this.Assets));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Accounts));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimplePriceApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimpleAssetApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.CreditAmount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.CreditAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.CreditAccount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DebitAmount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DebitAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DebitAccount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.RelatedAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.CreditAmount), nameof(this.CreditAmountFormatted));
		this.RegisterDependentProperty(nameof(this.CreditAsset), nameof(this.CreditAmountFormatted));
		this.RegisterDependentProperty(nameof(this.DebitAmount), nameof(this.DebitAmountFormatted));
		this.RegisterDependentProperty(nameof(this.DebitAsset), nameof(this.DebitAmountFormatted));
		this.RegisterDependentProperty(nameof(this.SimpleCurrencyImpact), nameof(this.SimpleCurrencyImpactFormatted));

		this.CopyFrom(this.Model);
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
							this.DebitAccount = this.ThisAccount;
							this.CreditAccount = null;
							this.CreditAmount = null;
							this.CreditAsset = null;
							break;
						case TransactionAction.Withdraw:
							this.DebitAccount = this.ThisAccount;
							this.CreditAccount = null;
							this.CreditAmount = null;
							this.CreditAsset = null;
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
		new(TransactionAction.Transfer, nameof(TransactionAction.Transfer)),
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

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
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

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
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

	[NonNegativeTransactionAmount]
	public decimal? SimpleAmount
	{
		get
		{
			if (this.IsCreditOperation)
			{
				return this.CreditAmount;
			}
			else if (this.IsDebitOperation)
			{
				return this.DebitAmount;
			}
			else if (this.Action == TransactionAction.Transfer)
			{
				return this.CreditAccount == this.ThisAccount ? this.CreditAmount : -this.DebitAmount;
			}
			else
			{
				return null;
			}
		}

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
			else if (this.Action == TransactionAction.Transfer)
			{
				this.DebitAmount = this.CreditAmount = value.HasValue ? Math.Abs(value.Value) : null;
				if (value > 0)
				{
					AccountViewModel? other = this.CreditAccount;
					this.CreditAccount = this.ThisAccount;
					if (other != this.ThisAccount && other is not null)
					{
						this.DebitAccount = other;
					}
				}
				else
				{
					AccountViewModel? other = this.DebitAccount;
					this.DebitAccount = this.ThisAccount;
					if (other != this.ThisAccount && other is not null)
					{
						this.CreditAccount = other;
					}
				}
			}
			else
			{
				ThrowNotSimpleAction();
			}

			this.OnPropertyChanged();
		}
	}

	public AssetViewModel? SimpleAsset
	{
		get =>
			this.Action == TransactionAction.Dividend ? this.RelatedAsset :
			this.IsCreditOperation ? this.CreditAsset :
			this.IsDebitOperation ? this.DebitAsset :
			this.Action == TransactionAction.Transfer && this.CreditAsset == this.DebitAsset ? this.CreditAsset :
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
			else if (this.Action == TransactionAction.Transfer)
			{
				this.DebitAsset = this.CreditAsset = value;
			}
			else
			{
				ThrowNotSimpleAction();
			}

			this.OnPropertyChanged();
		}
	}

	public bool IsSimpleAssetApplicable => this.Action is TransactionAction.Dividend or TransactionAction.Buy or TransactionAction.Sell or TransactionAction.Add or TransactionAction.Remove;

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
	public decimal? SimplePrice
	{
		get
		{
			if (this.Action is TransactionAction.Buy)
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

			this.OnPropertyChanged();
		}
	}

	public bool IsSimplePriceApplicable => this.Action is TransactionAction.Buy or TransactionAction.Sell;

	public AccountViewModel? SimpleAccount
	{
		get => this.SimpleAmount > 0 ? this.DebitAccount : this.CreditAccount;
		set
		{
			if (this.SimpleAmount > 0)
			{
				this.CreditAccount = this.ThisAccount;
				this.DebitAccount = value;
			}
			else
			{
				this.DebitAccount = this.ThisAccount;
				this.CreditAccount = value;
			}

			this.OnPropertyChanged();
		}
	}

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

	public string? SimpleCurrencyImpactFormatted => this.ThisAccount.CurrencyAsset?.Format(this.SimpleCurrencyImpact);

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
				TransactionAction.Interest => $"+{this.CreditAmountFormatted}",
				TransactionAction.Dividend => $"{this.RelatedAsset?.TickerOrName} +{this.CreditAmountFormatted}",
				TransactionAction.Sell => $"{this.DebitAmount} {this.DebitAsset?.TickerOrName} @ {this.SimplePriceFormatted}",
				TransactionAction.Buy => $"{this.CreditAmount} {this.CreditAsset?.TickerOrName} @ {this.SimplePriceFormatted}",
				TransactionAction.Transfer => this.ThisAccount == this.CreditAccount ? $"{this.DebitAccount?.Name} -> {this.CreditAmountFormatted} {this.CreditAsset?.TickerOrName}" : $"{this.CreditAccount?.Name} <- {this.DebitAmountFormatted} {this.DebitAsset?.TickerOrName}",
				TransactionAction.Deposit => $"{this.CreditAmountFormatted}",
				TransactionAction.Withdraw => $"{this.DebitAmountFormatted}",
				TransactionAction.Exchange => $"{this.DebitAmount} {this.DebitAsset?.TickerOrName} -> {this.CreditAmount} {this.CreditAsset?.TickerOrName}",
				_ => string.Empty,
			};
		}
	}

	public string? CreditAmountFormatted => this.CreditAsset?.Format(this.CreditAmount);

	public string? DebitAmountFormatted => this.DebitAsset?.Format(this.DebitAmount);

	public string? SimplePriceFormatted => this.ThisAccount.CurrencyAsset?.Format(this.SimplePrice);

	public IEnumerable<AssetViewModel> Assets => this.ThisAccount.DocumentViewModel.AssetsPanel.Assets.Where(a => (this.Action == TransactionAction.Transfer && (this.SimpleAccount is not BankingAccountViewModel || (a.Type == Asset.AssetType.Currency))) || (this.Action != TransactionAction.Transfer && this.IsCashTransaction == (a.Type == Asset.AssetType.Currency)));

	public IEnumerable<AccountViewModel> Accounts => this.ThisAccount.DocumentViewModel.AccountsPanel.Accounts.Where(a => a != this.ThisAccount);

	private bool IsCreditOperation => this.Action is TransactionAction.Buy or TransactionAction.Add or TransactionAction.Deposit or TransactionAction.Interest or TransactionAction.Dividend;

	private bool IsDebitOperation => this.Action is TransactionAction.Sell or TransactionAction.Remove or TransactionAction.Withdraw;

	private bool IsCashTransaction => this.Action is TransactionAction.Deposit or TransactionAction.Withdraw;

	protected override void ApplyToCore()
	{
		this.Model.When = this.When;
		this.Model.Memo = this.Memo;
		this.Model.Action = this.Action!.Value;

		this.Model.CreditAccountId = this.CreditAccount?.Id;
		this.Model.CreditAssetId = this.CreditAsset?.Id;
		this.Model.CreditAmount = this.CreditAmount;

		this.Model.DebitAccountId = this.DebitAccount?.Id;
		this.Model.DebitAssetId = this.DebitAsset?.Id;
		this.Model.DebitAmount = this.DebitAmount;

		this.Model.RelatedAssetId = this.RelatedAsset?.Id;
	}

	protected override void CopyFromCore()
	{
		this.When = this.Model.When;
		this.Memo = this.Model.Memo;
		this.SetProperty(ref this.action, this.Model.Action, nameof(this.Action));

		this.CreditAccount = this.ThisAccount.DocumentViewModel.GetAccount(this.Model.CreditAccountId);
		this.CreditAmount = this.Model.CreditAmount;
		this.CreditAsset = this.ThisAccount.DocumentViewModel.GetAsset(this.Model.CreditAssetId);

		this.DebitAccount = this.ThisAccount.DocumentViewModel.GetAccount(this.Model.DebitAccountId);
		this.DebitAsset = this.ThisAccount.DocumentViewModel.GetAsset(this.Model.DebitAssetId);
		this.DebitAmount = this.Model.DebitAmount;

		this.RelatedAsset = this.ThisAccount.DocumentViewModel.GetAsset(this.Model.RelatedAssetId);
	}

	protected override bool IsPersistedProperty(string propertyName)
	{
		if (propertyName.EndsWith("IsReadOnly") || propertyName.EndsWith("ToolTip") || propertyName.EndsWith("Formatted"))
		{
			return false;
		}

		return base.IsPersistedProperty(propertyName) && propertyName is not (nameof(this.SimpleAsset) or nameof(this.SimpleAmount) or nameof(this.SimplePrice) or nameof(this.SimpleCurrencyImpact));
	}

	[DoesNotReturn]
	private static Exception ThrowNotSimpleAction() => throw new InvalidOperationException("Not a simple operation.");
}
