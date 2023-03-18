// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public class InvestingTransactionViewModel : TransactionViewModel
{
	private TransactionAction? action;
	private AccountViewModel? depositAccount;
	private AccountViewModel? withdrawAccount;
	private AssetViewModel? deposit;
	private AssetViewModel? withdrawAsset;
	private AssetViewModel? relatedAsset;
	private decimal? depositAmount;
	private decimal? withdrawAmount;
	private decimal? cashValue;
	private decimal? commission;
	private bool wasEverNonEmpty;
	private DateTime? acquisitionDate;
	private decimal? acquisitionPrice;
	private TaxLotSelectionViewModel? taxLotSelection;

	/// <summary>
	/// Initializes a new instance of the <see cref="InvestingTransactionViewModel"/> class
	/// that is not backed by any pre-existing model in the database.
	/// </summary>
	/// <param name="thisAccount">The account this view model belongs to.</param>
	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount)
		: this(thisAccount, Array.Empty<TransactionAndEntry>())
	{
	}

	public InvestingTransactionViewModel(InvestingAccountViewModel thisAccount, IReadOnlyList<TransactionAndEntry> models)
		: base(thisAccount, models)
	{
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.SimpleAmount));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.SimpleAmount));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.SimpleAsset));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.SimpleAsset));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.SimpleCurrencyImpact));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.CashValue), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.AcquisitionPrice), nameof(this.SimplePrice));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Assets));
		this.RegisterDependentProperty(nameof(this.SimpleAccount), nameof(this.Assets));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Accounts));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimplePriceApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.IsSimpleAssetApplicable));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.TaxLotSelection));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DepositAccount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.WithdrawAccount), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.RelatedAsset), nameof(this.Description));
		this.RegisterDependentProperty(nameof(this.DepositAmount), nameof(this.DepositAmountFormatted));
		this.RegisterDependentProperty(nameof(this.DepositAsset), nameof(this.DepositAmountFormatted));
		this.RegisterDependentProperty(nameof(this.WithdrawAmount), nameof(this.WithdrawAmountFormatted));
		this.RegisterDependentProperty(nameof(this.WithdrawAsset), nameof(this.WithdrawAmountFormatted));
		this.RegisterDependentProperty(nameof(this.CashValue), nameof(this.CashValueFormatted));
		this.RegisterDependentProperty(nameof(this.SimpleCurrencyImpact), nameof(this.SimpleCurrencyImpactFormatted));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.ShowTaxLotSelection));
		this.RegisterDependentProperty(nameof(this.Action), nameof(this.ShowCreateLots));

		this.PropertyChanged += (s, e) => this.wasEverNonEmpty |= !this.IsEmpty;

		this.CopyFrom(models);
	}

	public bool IsEmpty => this.Action == TransactionAction.Unspecified && this.DepositAmount is null && this.WithdrawAmount is null && this.RelatedAsset is null;

	public override bool IsReadyToSave => string.IsNullOrEmpty(this.Error) && (!this.IsEmpty || this.wasEverNonEmpty);

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
							this.DepositAccount ??= this.ThisAccount;
							this.WithdrawAccount ??= this.ThisAccount;
							this.WithdrawAsset ??= this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Sell:
							this.DepositAccount ??= this.ThisAccount;
							this.WithdrawAccount ??= this.ThisAccount;
							this.DepositAsset ??= this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Exchange:
							this.DepositAccount ??= this.ThisAccount;
							this.WithdrawAccount ??= this.ThisAccount;
							break;
						case TransactionAction.Remove:
							this.WithdrawAccount = this.ThisAccount;
							this.DepositAccount = null;
							this.DepositAmount = null;
							this.DepositAsset = null;
							break;
						case TransactionAction.Withdraw:
							this.WithdrawAccount = this.ThisAccount;
							this.DepositAccount = null;
							this.DepositAmount = null;
							this.DepositAsset = null;
							this.WithdrawAsset = this.ThisAccount.CurrencyAsset;
							break;
						case TransactionAction.Interest:
						case TransactionAction.Deposit:
							this.DepositAccount = this.ThisAccount;
							this.DepositAsset = this.ThisAccount.CurrencyAsset;
							this.DepositAmount ??= 0;
							this.WithdrawAccount = null;
							this.WithdrawAmount = null;
							this.WithdrawAsset = null;
							break;
						case TransactionAction.Add:
							this.DepositAccount = this.ThisAccount;
							this.WithdrawAccount = null;
							this.WithdrawAmount = null;
							this.WithdrawAsset = null;
							break;
						case TransactionAction.Dividend:
							this.DepositAccount = this.ThisAccount;
							this.DepositAsset = this.ThisAccount.CurrencyAsset;
							this.WithdrawAccount = null;
							this.WithdrawAmount = null;
							this.WithdrawAsset = null;
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

	public AssetViewModel? DepositAsset
	{
		get => this.deposit;
		set => this.SetProperty(ref this.deposit, value);
	}

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
	public decimal? DepositAmount
	{
		get => this.depositAmount;
		set => this.SetProperty(ref this.depositAmount, value);
	}

	public AccountViewModel? DepositAccount
	{
		get => this.depositAccount;
		set => this.SetProperty(ref this.depositAccount, value);
	}

	public AssetViewModel? WithdrawAsset
	{
		get => this.withdrawAsset;
		set => this.SetProperty(ref this.withdrawAsset, value);
	}

	[Range(0, int.MaxValue, ErrorMessage = "Must not be a negative number.")]
	public decimal? WithdrawAmount
	{
		get => this.withdrawAmount;
		set => this.SetProperty(ref this.withdrawAmount, value);
	}

	public AccountViewModel? WithdrawAccount
	{
		get => this.withdrawAccount;
		set => this.SetProperty(ref this.withdrawAccount, value);
	}

	/// <summary>
	/// Gets or sets the date the asset was acquired.
	/// Only applicable when <see cref="Action"/> is set to <see cref="TransactionAction.Add"/>.
	/// </summary>
	public DateTime AcquisitionDate
	{
		get => this.acquisitionDate ?? this.When;
		set => this.SetProperty(ref this.acquisitionDate, value);
	}

	/// <summary>
	/// Gets or sets the total cost of the asset.
	/// Only applicable when <see cref="Action"/> is set to <see cref="TransactionAction.Add"/>.
	/// </summary>
	public decimal? AcquisitionPrice
	{
		get => this.acquisitionPrice;
		set => this.SetProperty(ref this.acquisitionPrice, value);
	}

	public string? AcquisitionPriceFormatted => this.ThisAccount.CurrencyAsset?.Format(this.AcquisitionPrice);

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
			if (this.IsDepositOperation)
			{
				return this.DepositAmount;
			}
			else if (this.IsWithdrawOperation)
			{
				return this.WithdrawAmount;
			}
			else if (this.Action == TransactionAction.Transfer)
			{
				return this.DepositAccount == this.ThisAccount ? this.DepositAmount : -this.WithdrawAmount;
			}
			else
			{
				return null;
			}
		}

		set
		{
			if (this.IsDepositOperation)
			{
				this.DepositAmount = value;
			}
			else if (this.IsWithdrawOperation)
			{
				this.WithdrawAmount = value;
			}
			else if (this.Action == TransactionAction.Transfer)
			{
				this.WithdrawAmount = this.DepositAmount = value.HasValue ? Math.Abs(value.Value) : null;
				if (value > 0)
				{
					AccountViewModel? other = this.DepositAccount;
					this.DepositAccount = this.ThisAccount;
					if (other != this.ThisAccount && other is not null)
					{
						this.WithdrawAccount = other;
						if (other is BankingAccountViewModel)
						{
							this.SimpleAsset = other.CurrencyAsset;
						}
					}
				}
				else
				{
					AccountViewModel? other = this.WithdrawAccount;
					this.WithdrawAccount = this.ThisAccount;
					if (other != this.ThisAccount && other is not null)
					{
						this.DepositAccount = other;
					}

					if (other is BankingAccountViewModel)
					{
						this.SimpleAsset = other.CurrencyAsset;
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
			this.Action == TransactionAction.Dividend ? this.DepositAsset :
			this.IsDepositOperation ? this.DepositAsset :
			this.IsWithdrawOperation ? this.WithdrawAsset :
			this.Action == TransactionAction.Transfer && this.DepositAsset == this.WithdrawAsset ? this.DepositAsset :
			null;
		set
		{
			if (this.Action is TransactionAction.Buy or TransactionAction.Add or TransactionAction.Dividend)
			{
				this.DepositAsset = value;
			}
			else if (this.Action is TransactionAction.Sell or TransactionAction.Remove)
			{
				this.WithdrawAsset = value;
			}
			else if (this.Action == TransactionAction.Transfer)
			{
				this.WithdrawAsset = this.DepositAsset = value;
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
			if (this.Action is TransactionAction.Buy or TransactionAction.CoverShort)
			{
				return this.DepositAmount != 0 && this.WithdrawAmount != 0 ? this.WithdrawAmount / this.DepositAmount : null;
			}
			else if (this.Action is TransactionAction.Sell or TransactionAction.ShortSale)
			{
				return this.DepositAmount != 0 && this.WithdrawAmount != 0 ? this.DepositAmount / this.WithdrawAmount : null;
			}
			else if (this.Action is TransactionAction.Dividend && this.CashValue.HasValue && this.DepositAmount.HasValue)
			{
				decimal value = this.CashValue.Value / this.DepositAmount.Value;
				if (this.ThisAccount.CurrencyAsset?.DecimalDigits is int precision)
				{
					value = Math.Round(value, precision);
				}

				return value;
			}
			else if (this.Action is TransactionAction.Add)
			{
				return this.AcquisitionPrice;
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
				CheckPrereq(this.DepositAmount, nameof(this.DepositAmount));
				this.WithdrawAmount = value * this.DepositAmount;
			}
			else if (this.Action is TransactionAction.Sell)
			{
				CheckPrereq(this.WithdrawAmount, nameof(this.WithdrawAmount));
				this.DepositAmount = value * this.WithdrawAmount;
			}
			else if (this.Action is TransactionAction.Dividend)
			{
				CheckPrereq(this.DepositAmount, nameof(this.DepositAmount));
				this.CashValue = value * this.DepositAmount;
			}
			else if (this.Action is TransactionAction.Add)
			{
				this.AcquisitionPrice = value;
			}
			else
			{
				throw ThrowNotSimpleAction();
			}

			this.OnPropertyChanged();

			void CheckPrereq(decimal? value, string propertyName) => Verify.Operation(value is not null, $"{propertyName} must be set first.");
		}
	}

	public bool IsSimplePriceApplicable => this.Action is TransactionAction.Buy or TransactionAction.Sell or TransactionAction.CoverShort or TransactionAction.ShortSale or TransactionAction.Dividend or TransactionAction.Add;

	public decimal? CashValue
	{
		get => this.cashValue;
		set => this.SetProperty(ref this.cashValue, value);
	}

	public string? CashValueFormatted => this.ThisAccount.CurrencyAsset?.Format(this.CashValue);

	public AccountViewModel? SimpleAccount
	{
		get => this.SimpleAmount > 0 ? this.WithdrawAccount : this.DepositAccount;
		set
		{
			if (this.SimpleAmount > 0)
			{
				this.DepositAccount = this.ThisAccount;
				this.WithdrawAccount = value;
			}
			else
			{
				this.WithdrawAccount = this.ThisAccount;
				this.DepositAccount = value;
			}

			if (value is BankingAccountViewModel)
			{
				this.SimpleAsset = value.CurrencyAsset;
			}

			this.OnPropertyChanged();
		}
	}

	/// <summary>
	/// Gets or sets the commission for this transaction.
	/// </summary>
	/// <remarks>
	/// The commission is stored as an <see cref="TransactionEntryViewModel"/>,
	/// attributed to the category identified by <see cref="ConfigurationPanelViewModel.CommissionCategory"/>.
	/// </remarks>
	[Range(0, double.MaxValue)]
	public decimal? Commission
	{
		get => this.commission;
		set => this.SetProperty(ref this.commission, value);
	}

	public string? CommissionFormatted => this.ThisAccount.CurrencyAsset?.Format(this.Commission);

	public decimal? SimpleCurrencyImpact
	{
		get
		{
			if (this.CashValue is not null)
			{
				return this.CashValue;
			}

			if (this.Action == TransactionAction.Dividend && this.DepositAsset == this.ThisAccount.CurrencyAsset)
			{
				return this.DepositAmount;
			}

			decimal impact = 0;
			if (this.WithdrawAsset == this.ThisAccount.CurrencyAsset && this.WithdrawAmount.HasValue)
			{
				impact -= this.WithdrawAmount.Value;
			}

			if (this.DepositAsset == this.ThisAccount.CurrencyAsset && this.DepositAmount.HasValue)
			{
				impact += this.DepositAmount.Value;
			}

			return impact;
		}
	}

	public string? SimpleCurrencyImpactFormatted => this.ThisAccount.CurrencyAsset?.Format(this.SimpleCurrencyImpact);

	public string Description
	{
		get
		{
			return this.Action switch
			{
				TransactionAction.Add => $"{this.DepositAmount} {this.DepositAsset?.TickerOrName} @ {this.AcquisitionPriceFormatted}",
				TransactionAction.Remove => $"{this.WithdrawAmount} {this.WithdrawAsset?.TickerOrName}",
				TransactionAction.Interest => $"+{this.DepositAmountFormatted}",
				TransactionAction.Dividend when this.CashValue is not null => $"{this.DepositAsset?.TickerOrName} +{this.DepositAmountFormatted} ({this.CashValueFormatted})",
				TransactionAction.Dividend => $"{this.RelatedAsset?.TickerOrName} +{this.DepositAmountFormatted}",
				TransactionAction.Sell => $"{this.WithdrawAmount} {this.WithdrawAsset?.TickerOrName} @ {this.SimplePriceFormatted}{CommissionString()}",
				TransactionAction.Buy => $"{this.DepositAmount} {this.DepositAsset?.TickerOrName} @ {this.SimplePriceFormatted}{CommissionString()}",
				TransactionAction.Transfer => this.ThisAccount == this.DepositAccount ? $"{this.WithdrawAccount?.Name} -> {this.DepositAmountFormatted} {this.DepositAsset?.TickerOrName}" : $"{this.DepositAccount?.Name} <- {this.WithdrawAmountFormatted} {this.WithdrawAsset?.TickerOrName}",
				TransactionAction.Deposit => $"{this.DepositAmountFormatted}",
				TransactionAction.Withdraw => $"{this.WithdrawAmountFormatted}",
				TransactionAction.Exchange => $"{this.WithdrawAmount} {this.WithdrawAsset?.TickerOrName} -> {this.DepositAmount} {this.DepositAsset?.TickerOrName}",
				_ => string.Empty,
			};

			string CommissionString() => this.Commission > 0 ? $" (-{this.CommissionFormatted})" : string.Empty;
		}
	}

	public string? DepositAmountFormatted => this.DepositAsset?.Format(this.DepositAmount);

	public string? WithdrawAmountFormatted => this.WithdrawAsset?.Format(this.WithdrawAmount);

	public string? SimplePriceFormatted => this.ThisAccount.CurrencyAsset?.Format(this.SimplePrice);

	public IEnumerable<AssetViewModel> Assets => this.ThisAccount.DocumentViewModel.AssetsPanel.Assets.Where(a => (this.Action == TransactionAction.Transfer && (this.SimpleAccount is not BankingAccountViewModel || (a.Type == Asset.AssetType.Currency))) || (this.Action != TransactionAction.Transfer && this.IsCashTransaction == (a.Type == Asset.AssetType.Currency)));

	public IEnumerable<AccountViewModel> Accounts => this.ThisAccount.DocumentViewModel.AccountsPanel.Accounts.Where(a => a != this.ThisAccount);

	public bool ShowCreateLots => this.Action is TransactionAction.Add;

	public bool ShowTaxLotSelection => this.Action is TransactionAction.Transfer or TransactionAction.Remove or TransactionAction.Sell or TransactionAction.Exchange;

	/// <summary>
	/// Gets a view model to assist with tax lot selection if this transaction consumes tax lots.
	/// </summary>
	public TaxLotSelectionViewModel? TaxLotSelection
	{
		get
		{
			if (this.ShowTaxLotSelection)
			{
				this.taxLotSelection ??= new(this);
				return this.taxLotSelection;
			}

			return null;
		}
	}

	private decimal? WithdrawAmountWithValidation
	{
		set
		{
			Requires.Range(value is not < 0, nameof(value));
			this.WithdrawAmount = value;
		}
	}

	private decimal? DepositAmountWithValidation
	{
		set
		{
			Requires.Range(value is not < 0, nameof(value));
			this.DepositAmount = value;
		}
	}

	private bool IsDepositOperation => this.Action is TransactionAction.Buy or TransactionAction.Add or TransactionAction.Deposit or TransactionAction.Interest or TransactionAction.Dividend or TransactionAction.CoverShort;

	private bool IsWithdrawOperation => this.Action is TransactionAction.Sell or TransactionAction.Remove or TransactionAction.Withdraw or TransactionAction.ShortSale;

	private bool IsCashTransaction => this.Action is TransactionAction.Deposit or TransactionAction.Withdraw;

	private bool DepositFullyInitialized => this.DepositAmount.HasValue && this.DepositAccount is not null && this.DepositAsset is not null;

	private bool WithdrawFullyInitialized => this.WithdrawAmount.HasValue && this.WithdrawAccount is not null && this.WithdrawAsset is not null;

	/// <summary>
	/// Gets the entries that impact <see cref="ThisAccount"/>.
	/// </summary>
	private IEnumerable<TransactionEntryViewModel> TopLevelEntries => this.Entries.Where(e => e.Account == this.ThisAccount);

	internal override void Entry_PropertyChanged(TransactionEntryViewModel sender, PropertyChangedEventArgs args)
	{
		base.Entry_PropertyChanged(sender, args);
		this.taxLotSelection?.OnTransactionEntry_PropertyChanged(sender, args);
	}

	internal void TaxLotAssignmentChanged(TaxLotAssignment tla)
	{
		this.taxLotSelection?.OnTaxLotAssignmentChanged(tla);
	}

	protected override void ApplyToCore()
	{
		Verify.Operation(this.Action.HasValue, $"{nameof(this.Action)} must be set first.");

		base.ApplyToCore();
		this.Transaction.Action = this.Action.Value;
		this.Transaction.RelatedAssetId = this.RelatedAsset?.Id;

		switch (this.Action.Value)
		{
			case TransactionAction.Interest:
			case TransactionAction.Add:
			case TransactionAction.Deposit:
			case TransactionAction.Dividend:
				if (this.CashValue is null)
				{
					if (TryEnsureEntryCount(1, this.DepositFullyInitialized))
					{
						this.Entries[0].Account = this.ThisAccount;
						this.Entries[0].Asset = this.DepositAsset;
						this.Entries[0].Amount = this.DepositAmount ?? 0;
						this.Entries[0].Cleared = this.Cleared;
					}
				}
				else
				{
					if (TryEnsureEntryCount(3, this.DepositFullyInitialized))
					{
						this.Entries[0].Account = this.ThisAccount;
						this.Entries[1].Account = this.ThisAccount;
						this.Entries[2].Account = this.ThisAccount;

						this.Entries[0].Cleared = this.Cleared;
						this.Entries[1].Cleared = this.Cleared;
						this.Entries[2].Cleared = this.Cleared;

						this.Entries[0].Asset = this.DepositAsset;
						this.Entries[0].Amount = this.DepositAmount ?? 0;

						this.Entries[1].Asset = this.ThisAccount.CurrencyAsset;
						this.Entries[1].Amount = this.CashValue.Value;
						this.Entries[2].Asset = this.ThisAccount.CurrencyAsset;
						this.Entries[2].Amount = -this.CashValue.Value;
					}
				}

				break;
			case TransactionAction.Transfer:
				if (TryEnsureEntryCount(2, this.DepositFullyInitialized && this.WithdrawFullyInitialized))
				{
					bool deposit = this.DepositAccount == this.ThisAccount;
					TransactionEntryViewModel ourEntry = this.Entries[0].Account == this.ThisAccount ? this.Entries[0] : this.Entries[1];
					TransactionEntryViewModel otherEntry = this.Entries[0].Account == this.ThisAccount ? this.Entries[1] : this.Entries[0];
					ourEntry.Account = this.ThisAccount;
					otherEntry.Account = deposit ? this.WithdrawAccount : this.DepositAccount;
					ourEntry.Asset = deposit ? this.DepositAsset : this.WithdrawAsset;
					otherEntry.Asset = deposit ? this.WithdrawAsset : this.DepositAsset;
					ourEntry.Amount = deposit ? (this.DepositAmount ?? 0) : -(this.WithdrawAmount ?? 0);
					otherEntry.Amount = deposit ? (this.WithdrawAmount ?? 0) : -(this.DepositAmount ?? 0);
					ourEntry.Cleared = this.Cleared;
				}

				break;
			case TransactionAction.Sell:
			case TransactionAction.Buy:
			case TransactionAction.Exchange:
				int countRequired = this.Commission is null ? 2 : 3;
				if (TryEnsureEntryCount(countRequired, this.DepositFullyInitialized && this.WithdrawFullyInitialized))
				{
					bool deposit = this.DepositAccount == this.ThisAccount;
					this.TrySortOutEntriesWithPossibleCommission(out TransactionEntryViewModel ourEntry, out TransactionEntryViewModel otherEntry, out TransactionEntryViewModel? commissionEntry);
					ourEntry.Account = this.ThisAccount;
					if (deposit)
					{
						otherEntry.Account = this.WithdrawAccount;
						ourEntry.Asset = this.DepositAsset;
						otherEntry.Asset = this.WithdrawAsset;
						ourEntry.Amount = this.DepositAmount ?? 0;
						otherEntry.Amount = -(this.WithdrawAmount ?? 0);
					}
					else
					{
						otherEntry.Account = this.DepositAccount;
						ourEntry.Asset = this.WithdrawAsset;
						otherEntry.Asset = this.DepositAsset;
						ourEntry.Amount = -(this.WithdrawAmount ?? 0);
						otherEntry.Amount = this.DepositAmount ?? 0;
					}

					if (this.Commission is not null)
					{
						Assumes.NotNull(commissionEntry);
						commissionEntry.Account = this.ThisAccount.DocumentViewModel.ConfigurationPanel.CommissionCategory;
						commissionEntry.Asset = this.ThisAccount.CurrencyAsset;
						commissionEntry.Amount = -this.Commission.Value;

						if (ourEntry.Asset == commissionEntry.Asset && ourEntry.Account == this.ThisAccount)
						{
							ourEntry.Amount -= this.Commission.Value;
						}
						else if (otherEntry.Asset == commissionEntry.Asset && otherEntry.Account == this.ThisAccount)
						{
							otherEntry.Amount -= this.Commission.Value;
						}
					}
				}

				break;
			case TransactionAction.Withdraw:
			case TransactionAction.Remove:
				if (TryEnsureEntryCount(1, this.WithdrawFullyInitialized))
				{
					this.Entries[0].Account = this.ThisAccount;
					this.Entries[0].Asset = this.WithdrawAsset;
					this.Entries[0].Amount = -this.WithdrawAmount ?? 0;
				}

				break;
			default:
				throw new NotImplementedException("Action: " + this.Action.Value);
		}

		foreach (TransactionEntryViewModel entry in this.TopLevelEntries)
		{
			entry.Cleared = this.Cleared;
		}

		bool TryEnsureEntryCount(int requiredCount, bool condition = true)
		{
			if (condition)
			{
				while (this.Entries.Count < requiredCount)
				{
					this.EntriesMutable.Add(new(this));
				}

				while (this.Entries.Count > requiredCount)
				{
					this.EntriesMutable.RemoveAt(requiredCount);
				}

				return true;
			}

			this.EntriesMutable.Clear();
			return false;
		}
	}

	protected override void CopyFromCore()
	{
		base.CopyFromCore();
		this.SetProperty(ref this.action, this.Transaction.Action, nameof(this.Action));
		this.RelatedAsset = this.ThisAccount.DocumentViewModel.GetAsset(this.Transaction.RelatedAssetId);
		this.Cleared = this.TopLevelEntries.FirstOrDefault(e => e.Cleared != ClearedState.None)?.Cleared ?? ClearedState.None;
		switch (this.Action)
		{
			case TransactionAction.Transfer:
			case TransactionAction.Exchange:
			case TransactionAction.Sell:
			case TransactionAction.Buy:
			case TransactionAction.ShortSale:
			case TransactionAction.CoverShort:
				this.SortOutEntriesWithPossibleCommission(out TransactionEntryViewModel ourEntry, out TransactionEntryViewModel otherEntry, out TransactionEntryViewModel? commissionEntry);
				bool deposit = ourEntry.Amount > 0;
				this.Commission = -commissionEntry?.Amount;
				if (deposit)
				{
					this.DepositAmountWithValidation = Math.Abs(ourEntry.Amount);
					this.DepositAccount = ourEntry.Account;
					this.DepositAsset = ourEntry.Asset;
					this.WithdrawAmountWithValidation = Math.Abs(otherEntry.Amount);
					this.WithdrawAccount = otherEntry.Account;
					this.WithdrawAsset = otherEntry.Asset;
				}
				else
				{
					this.DepositAmountWithValidation = Math.Abs(otherEntry.Amount);
					this.DepositAccount = otherEntry.Account;
					this.DepositAsset = otherEntry.Asset;
					this.WithdrawAmountWithValidation = Math.Abs(ourEntry.Amount);
					this.WithdrawAccount = ourEntry.Account;
					this.WithdrawAsset = ourEntry.Asset;
				}

				// Now adjust for commission.
				if (commissionEntry is not null)
				{
					if (this.DepositAccount == this.ThisAccount && this.DepositAsset == commissionEntry.Asset)
					{
						this.DepositAmount -= commissionEntry.Amount;
					}
					else if (this.WithdrawAccount == this.ThisAccount && this.WithdrawAsset == commissionEntry.Asset)
					{
						this.WithdrawAmount += commissionEntry.Amount;
					}
				}

				break;
			case TransactionAction.Add:
			case TransactionAction.Deposit:
			case TransactionAction.Interest:
				TransactionEntryViewModel? securityAddEntry = null;
				TransactionEntryViewModel? categoryEntry = null;
				foreach (TransactionEntryViewModel entry in this.Entries)
				{
					if (entry.Account?.Id == this.ThisAccount.Id)
					{
						if (securityAddEntry is not null)
						{
							throw new NotSupportedException("Too many transaction entries associated with this account.");
						}

						securityAddEntry = entry;
					}
					else
					{
						if (categoryEntry is not null)
						{
							throw new NotSupportedException("Too many transaction entries associated with another account.");
						}

						categoryEntry = entry;
					}
				}

				Assumes.NotNull(securityAddEntry); // we wouldn't see this transaction at all if it didn't have an entry linked to this account.
				this.DepositAccount = securityAddEntry.Account;
				this.DepositAmountWithValidation = Math.Abs(securityAddEntry.Amount);
				this.DepositAsset = securityAddEntry.Asset;
				this.WithdrawAccount = categoryEntry?.Account;
				this.WithdrawAmountWithValidation = categoryEntry?.Amount;
				this.WithdrawAsset = categoryEntry?.Asset;
				break;
			case TransactionAction.Dividend:
				securityAddEntry = this.Entries.Count switch
				{
					1 => this.Entries[0],
					3 => this.Entries.Single(e => e.Asset != this.ThisAccount.CurrencyAsset),
					_ => throw new NotSupportedException($"Transaction action {this.Action} with {this.Entries.Count} entries is not supported."),
				};
				this.DepositAccount = securityAddEntry.Account;
				this.DepositAmountWithValidation = Math.Abs(securityAddEntry.Amount);
				this.DepositAsset = securityAddEntry.Asset;
				this.WithdrawAccount = null;
				this.WithdrawAmountWithValidation = null;
				this.WithdrawAsset = null;
				if (this.Entries.Count == 3)
				{
					// We know the cash value of this dividend.
					this.CashValue = this.Entries.Single(e => e.Asset == this.ThisAccount.CurrencyAsset && e.Amount > 0).Amount;
				}

				break;
			case TransactionAction.Remove:
			case TransactionAction.Withdraw:
				Assumes.True(this.Entries.Count == 1);
				this.WithdrawAccount = this.Entries[0].Account;
				this.WithdrawAmountWithValidation = Math.Abs(this.Entries[0].Amount);
				this.WithdrawAsset = this.Entries[0].Asset;
				this.DepositAccount = null;
				this.DepositAmountWithValidation = null;
				this.DepositAsset = null;
				break;
			case TransactionAction.Unspecified when this.Entries.Count(e => !e.IsEmpty) == 0:
				break;
			default:
				throw new NotImplementedException("Action is " + this.Action);
		}

		if (this.Action == TransactionAction.Add && this.Entries.Count == 1 && this.Entries[0].CreatedTaxLots?.Count == 1)
		{
			TaxLotViewModel taxLot = this.Entries[0].CreatedTaxLots![0];
			this.AcquisitionPrice = taxLot.CostBasisAmount / taxLot.Amount;
			this.AcquisitionDate = taxLot.AcquiredDate;
		}
	}

	protected override bool IsPersistedProperty(string propertyName)
	{
		return base.IsPersistedProperty(propertyName)
			&& propertyName is not (nameof(this.SimpleAsset) or nameof(this.SimpleAmount) or nameof(this.SimplePrice) or nameof(this.SimpleCurrencyImpact))
			&& !(propertyName.EndsWith("IsReadOnly") || propertyName.EndsWith("ToolTip") || propertyName.EndsWith("Formatted"));
	}

	protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);
		this.taxLotSelection?.OnTransactionPropertyChanged(propertyName);
	}

	protected override void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		base.Entries_CollectionChanged(sender, e);
		this.taxLotSelection?.OnTransactionEntriesChanged(e);
	}

	[DoesNotReturn]
	private static Exception ThrowNotSimpleAction() => throw new InvalidOperationException("Not a simple operation.");

	private void SortOutEntriesWithPossibleCommission([NotNull] out TransactionEntryViewModel? ourEntry, [NotNull] out TransactionEntryViewModel? otherEntry, out TransactionEntryViewModel? commissionEntry)
	{
		ourEntry = null;
		otherEntry = null;
		commissionEntry = null;
		foreach (TransactionEntryViewModel entryViewModel in this.Entries)
		{
			if (entryViewModel.Account == this.ThisAccount)
			{
				// Two entries are allowed to reference this account.
				if (ourEntry is null)
				{
					SetOnce(ref ourEntry, entryViewModel);
				}
				else
				{
					SetOnce(ref otherEntry, entryViewModel);
				}
			}
			else if (entryViewModel.Account == this.ThisAccount.DocumentViewModel.ConfigurationPanel.CommissionCategory)
			{
				SetOnce(ref commissionEntry, entryViewModel);
			}
			else
			{
				SetOnce(ref otherEntry, entryViewModel);
			}

			void SetOnce(ref TransactionEntryViewModel? local, TransactionEntryViewModel newValue)
			{
				Verify.Operation(local is null, "Only one entry is allowed per account.");
				local = newValue;
			}
		}

		Verify.Operation(ourEntry is not null && otherEntry is not null, "Required entries not found.");
	}

	private void TrySortOutEntriesWithPossibleCommission([NotNull] out TransactionEntryViewModel? ourEntry, [NotNull] out TransactionEntryViewModel? otherEntry, out TransactionEntryViewModel? commissionEntry)
	{
		ourEntry = null;
		otherEntry = null;
		commissionEntry = null;

		List<TransactionEntryViewModel> unclaimedEntries = this.Entries.ToList();
		for (int i = unclaimedEntries.Count - 1; i >= 0; i--)
		{
			if (unclaimedEntries[i].Account == this.ThisAccount)
			{
				if (ourEntry is null)
				{
					Claim(ref ourEntry);
				}
				else if (otherEntry is null)
				{
					Claim(ref otherEntry);
				}
			}
			else if (unclaimedEntries[i].Account == this.ThisAccount.DocumentViewModel.ConfigurationPanel.CommissionCategory)
			{
				Claim(ref commissionEntry);
			}

			void Claim(ref TransactionEntryViewModel? local)
			{
				local = unclaimedEntries[i];
				unclaimedEntries.RemoveAt(i);
			}
		}

		// Now assign unclaimed entries as required.
		FillNeed(ref ourEntry);
		FillNeed(ref otherEntry);
		if (unclaimedEntries.Count > 0)
		{
			FillNeed(ref commissionEntry);
		}

		void FillNeed(ref TransactionEntryViewModel? local)
		{
			if (local is null)
			{
				Index idx = ^1;
				local = unclaimedEntries[idx];
				unclaimedEntries.RemoveAt(idx.GetOffset(unclaimedEntries.Count));
			}
		}

		Verify.Operation(ourEntry is not null && otherEntry is not null, "Required entries not found.");
	}
}
