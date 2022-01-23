// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Transactions;
using System.Windows.Input;
using PCLCommandBase;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class BankingTransactionViewModel : TransactionViewModel
{
	private static readonly ReadOnlyCollection<ClearedStateViewModel> SharedClearedStates = new(new ClearedStateViewModel[]
	{
		new(ClearedState.None, "None", string.Empty),
		new(ClearedState.Cleared, "Cleared", "C"),
		new(ClearedState.Reconciled, "Reconciled", "R"),
	});

	private ObservableCollection<TransactionEntryViewModel>? splits;
	private int? checkNumber;
	private decimal amount;
	private ClearedState cleared = ClearedState.None;
	private string? payee;
	private decimal balance;
	private TransactionEntryViewModel? selectedSplit;
	private AccountViewModel? otherAccount;

	/// <summary>
	/// Initializes a new instance of the <see cref="BankingTransactionViewModel"/> class
	/// that is not backed by any pre-existing model in the database.
	/// </summary>
	/// <param name="thisAccount">The account this view model belongs to.</param>
	public BankingTransactionViewModel(BankingAccountViewModel thisAccount)
		: this(thisAccount, Array.Empty<TransactionAndEntry>())
	{
	}

	public BankingTransactionViewModel(BankingAccountViewModel thisAccount, IReadOnlyList<TransactionAndEntry> transactionAndEntries)
		: base(thisAccount, transactionAndEntries)
	{
		this.SplitCommand = new SplitCommandImpl(this);
		this.DeleteSplitCommand = new DeleteSplitCommandImpl(this);

		this.RegisterDependentProperty(nameof(this.ContainsSplits), nameof(this.SplitCommandToolTip));
		this.RegisterDependentProperty(nameof(this.Cleared), nameof(this.ClearedShortCaption));
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.AmountFormatted));
		this.RegisterDependentProperty(nameof(this.Balance), nameof(this.BalanceFormatted));
		this.RegisterDependentProperty(nameof(this.Payee), nameof(this.IsEmpty));
		this.RegisterDependentProperty(nameof(this.Memo), nameof(this.IsEmpty));
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.IsEmpty));
		this.RegisterDependentProperty(nameof(this.ContainsSplits), nameof(this.IsEmpty));

		this.CopyFrom(transactionAndEntries);
	}

	/// <summary>
	/// Gets a command that toggles between a split transaction and a simple one.
	/// </summary>
	public CommandBase SplitCommand { get; }

	public string SplitCommandToolTip => this.ContainsSplits ? "Delete all splits" : "Split transaction to allow assignment to multiple categories";

	/// <summary>
	/// Gets a command which deletes the <see cref="SelectedSplit"/> when executed.
	/// </summary>
	public ICommand DeleteSplitCommand { get; }

	/// <summary>
	/// Gets the caption for the <see cref="DeleteSplitCommand"/> command.
	/// </summary>
	public string DeleteSplitCommandCaption => "_Delete split";

	public ReadOnlyCollection<ClearedStateViewModel> ClearedStates => SharedClearedStates;

	public int? CheckNumber
	{
		get => this.checkNumber;
		set => this.SetProperty(ref this.checkNumber, value);
	}

	public decimal Amount
	{
		get => this.amount;
		set => this.SetProperty(ref this.amount, value);
	}

	public string? AmountFormatted => this.ThisAccount.CurrencyAsset?.Format(this.Amount);

	public ClearedState Cleared
	{
		get => this.cleared;
		set => this.SetProperty(ref this.cleared, value);
	}

	public string ClearedShortCaption => SharedClearedStates[(int)this.Cleared].ShortCaption;

	public string? Payee
	{
		get => this.payee;
		set => this.SetProperty(ref this.payee, value);
	}

	/// <summary>
	/// Gets or sets the category, transfer account, or the special "split" placeholder.
	/// </summary>
	public AccountViewModel? OtherAccount
	{
		get => this.otherAccount;
		set => this.SetProperty(ref this.otherAccount, value);
	}

	public IEnumerable<AccountViewModel> AvailableTransactionTargets
		=> this.ThisAccount.DocumentViewModel.TransactionTargets.Where(tt => tt != this.ThisAccount && tt != this.ThisAccount.DocumentViewModel.SplitCategory);

	////[SplitSumMatchesTransactionAmount]
	public IReadOnlyList<TransactionEntryViewModel> Splits
	{
		get
		{
			if (this.splits is null)
			{
				this.splits = new();
				if (this.IsPersisted)
				{
					////foreach (Transaction split in splits)
					////{
					////	TransactionEntryViewModel splitViewModel = new(this, split);
					////	this.splits.Add(splitViewModel);
					////	splitViewModel.PropertyChanged += this.Splits_PropertyChanged;
					////}

					if (this.splits.Count > 0)
					{
						this.CreateVolatileSplit();
					}
				}
			}

			return this.splits;
		}
	}

	/// <summary>
	/// Gets a value indicating whether this transaction can only be represented with splits.
	/// </summary>
	/// <remarks>
	/// A typical transaction will have 1 or 2 entries, which can be represented as a single row in a ledger.
	/// Any more than that indicates that a split took place, where money did not just go from one account/category to another.
	/// </remarks>
	public bool ContainsSplits => this.Splits.Count > 0;

	public TransactionEntryViewModel? SelectedSplit
	{
		get => this.selectedSplit;
		set => this.SetProperty(ref this.selectedSplit, value);
	}

	public decimal Balance
	{
		get => this.balance;
		set => this.SetProperty(ref this.balance, value);
	}

	public string? BalanceFormatted => this.ThisAccount.CurrencyAsset?.Format(this.Balance);

	/// <inheritdoc cref="TransactionViewModel.ThisAccount"/>
	public new BankingAccountViewModel ThisAccount => (BankingAccountViewModel)base.ThisAccount;

	public bool IsEmpty => string.IsNullOrWhiteSpace(this.Payee) && string.IsNullOrWhiteSpace(this.Memo) && this.Amount == 0 && !this.ContainsSplits;

	public override bool IsReadyToSave => string.IsNullOrEmpty(this.Error) && !this.IsEmpty && this.Splits.All(e => e.IsReadyToSave);

	/// <summary>
	/// Gets the first entry that impacts <see cref="ThisAccount"/>.
	/// </summary>
	private TransactionEntryViewModel? TopLevelEntry => this.Entries.FirstOrDefault(e => e.Account == this.ThisAccount);

	private string DebuggerDisplay => $"Transaction ({this.TransactionId}): {this.When} {this.Payee} {this.Amount}";

	public TransactionEntryViewModel NewSplit()
	{
		if (!this.IsPersisted)
		{
			// Persist this transaction so the splits can refer to it.
			this.Save();
		}

		_ = this.Splits; // ensure initialized
		bool wasSplit = this.ContainsSplits;
		TransactionEntryViewModel split = new(this)
		{
			Amount = wasSplit ? 0 : this.Amount,
		};
		using (this.SuspendAutoSave())
		{
			if (this.OtherAccount != this.ThisAccount.DocumentViewModel.SplitCategory)
			{
				split.Account = this.OtherAccount;
			}

			this.OtherAccount = this.ThisAccount.DocumentViewModel.SplitCategory;

			this.splits!.Add(split);
		}

		split.PropertyChanged += this.Splits_PropertyChanged;
		if (!wasSplit)
		{
			this.OnPropertyChanged(nameof(this.ContainsSplits));
			this.CreateVolatileSplit();
		}

		return split;
	}

	public void DeleteSplit(TransactionEntryViewModel split)
	{
		if (this.splits is null)
		{
			throw new InvalidOperationException("Splits haven't been initialized.");
		}

		int indexOfSplit = this.splits.IndexOf(split);
		if (!this.splits.Remove(split))
		{
			return;
		}

		this.ThisAccount.MoneyFile.Delete(split.Model);

		if (this.Splits.Count(s => s.IsPersisted) > 0)
		{
			this.SetAmountBasedOnSplits();

			if (!split.IsPersisted)
			{
				// We deleted the volatile transaction (new row placeholder). Recreate it.
				this.CreateVolatileSplit();
			}
		}
		else
		{
			decimal amount = this.Amount;
			this.splits.Clear();
			this.OnPropertyChanged(nameof(this.ContainsSplits));

			// Salvage some data from the last split.
			if (string.IsNullOrEmpty(this.Memo))
			{
				this.Memo = split.Memo;
			}

			this.OtherAccount = split.Account;
			this.Amount = amount;
		}

		if (this.SelectedSplit == split)
		{
			this.SelectedSplit =
				this.splits.Count > indexOfSplit ? this.splits[indexOfSplit] :
				this.splits.Count == indexOfSplit ? this.splits[indexOfSplit - 1] :
				null;
		}
	}

	public void DeleteAllSplits()
	{
		_ = this.Splits;

		if (this.ContainsSplits)
		{
			int originalSplitCount = this.Splits.Count(s => s.IsPersisted); // don't count the volatile split placeholder.
			decimal amount = this.Amount;
			foreach (TransactionEntryViewModel split in this.Splits.ToList())
			{
				this.DeleteSplit(split);
			}

			// If there was more than one split item, we don't know which category to apply to the original transaction,
			// so clear it explicitly since otherwise the DeleteSplit call above would cause us to inherit the last split's category.
			if (originalSplitCount > 1)
			{
				this.OtherAccount = null;

				// Also restore the Amount to the original multi-split value.
				this.Amount = amount;
			}
		}
	}

	public decimal GetSplitTotal()
	{
		Verify.Operation(this.ContainsSplits, "Not a split transaction.");
		return this.Splits.Sum(s => s.Amount);
	}

	protected internal override void NotifyReassignCategory(ICollection<CategoryAccountViewModel> oldCategories, CategoryAccountViewModel? newCategory)
	{
		base.NotifyReassignCategory(oldCategories, newCategory);

		// Update our transaction category if applicable.
		this.SetOtherAccountBasedOnEntries();
	}

	protected internal override void NotifyAccountDeleted(ICollection<int> accountIds)
	{
		base.NotifyAccountDeleted(accountIds);
		this.SetOtherAccountBasedOnEntries();
	}

	protected override void ApplyToCore()
	{
		this.Transaction.Payee = this.Payee;
		this.Transaction.When = this.When;
		this.Transaction.CheckNumber = this.CheckNumber;

		if (!this.ContainsSplits)
		{
			switch (this.Entries.Count)
			{
				case 0:
					this.EntriesMutable.Add(new TransactionEntryViewModel(this) { Account = this.ThisAccount, Amount = this.Amount, Asset = this.ThisAccount.CurrencyAsset });
					if (this.OtherAccount is not null)
					{
						this.EntriesMutable.Add(new TransactionEntryViewModel(this) { Account = this.OtherAccount, Amount = -this.Amount, Asset = this.ThisAccount.CurrencyAsset });
					}

					break;
				case 1:
					this.Entries[0].Amount = this.Amount;
					if (this.OtherAccount is not null)
					{
						this.EntriesMutable.Add(new TransactionEntryViewModel(this) { Account = this.OtherAccount, Amount = -this.Amount, Asset = this.ThisAccount.CurrencyAsset });
					}

					break;
				case 2:
					TransactionEntryViewModel ourEntry = this.Entries[0].Account == this.ThisAccount ? this.Entries[0] : this.Entries[1];
					TransactionEntryViewModel otherEntry = this.Entries[0].Account == this.ThisAccount ? this.Entries[1] : this.Entries[0];
					ourEntry.Amount = this.Amount;
					otherEntry.Amount = -this.Amount;
					otherEntry.Account = this.OtherAccount;
					break;
				default:
					throw new NotSupportedException();
			}

			this.Transaction.Action =
				this.Entries.Count == 2 && this.Entries.All(e => e.Account?.Type is not (null or Account.AccountType.Category)) ? TransactionAction.Transfer :
				this.Entries.Count == 1 && this.Entries[0].Amount > 0 ? TransactionAction.Deposit :
				this.Entries.Count == 1 && this.Entries[0].Amount < 0 ? TransactionAction.Withdraw :
				TransactionAction.Unspecified;
		}

		if (this.TopLevelEntry is object)
		{
			this.TopLevelEntry.Cleared = this.Cleared;
		}

		base.ApplyToCore();
	}

	protected override void CopyFromCore()
	{
		base.CopyFromCore();
		this.Payee = this.Transaction.Payee;
		this.CheckNumber = this.Transaction.CheckNumber;
		this.Cleared = this.TopLevelEntry?.Cleared ?? ClearedState.None;
		this.SetOtherAccountBasedOnEntries();
		this.Amount = this.Entries.Where(e => e.Account == this.ThisAccount).Sum(e => e.Amount);
	}

	protected override bool IsPersistedProperty(string propertyName)
	{
		if (!base.IsPersistedProperty(propertyName))
		{
			return false;
		}

		if (propertyName is nameof(this.Balance) or nameof(this.ContainsSplits) or nameof(this.SelectedSplit) or nameof(this.IsEmpty))
		{
			return false;
		}

		if (propertyName.EndsWith("IsReadOnly") || propertyName.EndsWith("ToolTip") || propertyName.EndsWith("Formatted"))
		{
			return false;
		}

		if (propertyName == nameof(this.Amount) && this.ContainsSplits)
		{
			return false;
		}

		return true;
	}

	private void Splits_PropertyChanged(object? sender, PropertyChangedEventArgs args)
	{
		if (this.ContainsSplits)
		{
			this.SetAmountBasedOnSplits();
		}
	}

	private void SetAmountBasedOnSplits()
	{
		this.Amount = this.Splits.Sum(s => s.Amount);
		this.ThisAccount.NotifyAmountChangedOnSplitTransaction(this);
	}

	private void SetOtherAccountBasedOnEntries()
	{
		this.OtherAccount = this.Entries.Count switch
		{
			0 or 1 => null,
			2 => this.Entries[0].Account != this.ThisAccount ? this.Entries[0].Account : this.Entries[1].Account,
			_ => throw new NotSupportedException(),
		};
	}

	private TransactionEntryViewModel CreateVolatileSplit()
	{
		// Always add one more "volatile" transaction at the end as a placeholder to add new data.
		_ = this.Splits;
		TransactionEntryViewModel volatileViewModel = new(this);
		this.splits!.Add(volatileViewModel);
		volatileViewModel.Saved += this.VolatileSplitTransaction_Saved;
		volatileViewModel.PropertyChanged += this.Splits_PropertyChanged;
		return volatileViewModel;
	}

	private void VolatileSplitTransaction_Saved(object? sender, EventArgs args)
	{
		TransactionEntryViewModel? volatileSplit = (TransactionEntryViewModel?)sender;
		Assumes.NotNull(volatileSplit);
		volatileSplit.Saved -= this.VolatileSplitTransaction_Saved;

		// We need a new volatile transaction.
		this.CreateVolatileSplit();
	}

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class ClearedStateViewModel : EnumValueViewModel<ClearedState>
	{
		public ClearedStateViewModel(ClearedState value, string caption, string shortCaption)
			: base(value, caption)
		{
			this.ShortCaption = shortCaption;
		}

		public string ShortCaption { get; }

		private string DebuggerDisplay => this.Caption;
	}

	private class SplitCommandImpl : CommandBase
	{
		private BankingTransactionViewModel transactionViewModel;

		public SplitCommandImpl(BankingTransactionViewModel transactionViewModel)
		{
			this.transactionViewModel = transactionViewModel;
			transactionViewModel.PropertyChanged += this.TransactionViewModel_PropertyChanged;
		}

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter);

		protected override async Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			if (!this.transactionViewModel.ContainsSplits)
			{
				this.transactionViewModel.NewSplit();
			}
			else
			{
				if (this.transactionViewModel.ThisAccount.DocumentViewModel.UserNotification is { } userNotification && this.transactionViewModel.Splits.Count(s => s.IsPersisted) > 1)
				{
					if (!await userNotification.ConfirmAsync("This operation will delete all splits.", defaultConfirm: false))
					{
						return;
					}
				}

				this.transactionViewModel.DeleteAllSplits();
			}
		}

		private void TransactionViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.transactionViewModel.ContainsSplits))
			{
				this.OnCanExecuteChanged();
			}
		}
	}

	private class DeleteSplitCommandImpl : CommandBase
	{
		private BankingTransactionViewModel transactionViewModel;

		public DeleteSplitCommandImpl(BankingTransactionViewModel transactionViewModel)
		{
			this.transactionViewModel = transactionViewModel;
			this.transactionViewModel.PropertyChanged += this.TransactionViewModel_PropertyChanged;
		}

		public override bool CanExecute(object? parameter = null)
		{
			return base.CanExecute(parameter) && this.transactionViewModel.SelectedSplit is object;
		}

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			if (this.transactionViewModel.SelectedSplit is { } split)
			{
				this.transactionViewModel.DeleteSplit(split);
			}

			return Task.CompletedTask;
		}

		private void TransactionViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.transactionViewModel.SelectedSplit))
			{
				this.OnCanExecuteChanged();
			}
		}
	}
}
