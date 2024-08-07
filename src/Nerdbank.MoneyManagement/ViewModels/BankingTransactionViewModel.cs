﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using System.Windows.Input;
using PCLCommandBase;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class BankingTransactionViewModel : TransactionViewModel
{
	private ObservableCollection<TransactionEntryViewModel>? splits;
	private int? checkNumber;
	private decimal amount;
	private string? payee;
	private decimal balance;
	private TransactionEntryViewModel? selectedSplit;
	private AccountViewModel? otherAccount;
	private bool wasEverNonEmpty;

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
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.AmountFormatted));
		this.RegisterDependentProperty(nameof(this.Balance), nameof(this.BalanceFormatted));
		this.RegisterDependentProperty(nameof(this.Payee), nameof(this.IsEmpty));
		this.RegisterDependentProperty(nameof(this.Memo), nameof(this.IsEmpty));
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.IsEmpty));
		this.RegisterDependentProperty(nameof(this.ContainsSplits), nameof(this.IsEmpty));

		this.PropertyChanged += (s, e) => this.wasEverNonEmpty |= !this.IsEmpty;

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
		set
		{
			Verify.Operation(value == this.ThisAccount.DocumentViewModel.SplitCategory || !this.ContainsSplits, "Split transactions cannot have their category set on the top-level.");
			this.SetProperty(ref this.otherAccount, value);
		}
	}

	public IEnumerable<AccountViewModel?> AvailableTransactionTargets
		=> this.ThisAccount.DocumentViewModel.TransactionTargets.Where(tt => tt != this.ThisAccount && tt != this.ThisAccount.DocumentViewModel.SplitCategory);

	////[SplitSumMatchesTransactionAmount]
	public IReadOnlyList<TransactionEntryViewModel> Splits
	{
		get
		{
			if (this.splits is null)
			{
				this.splits = new();
				if (this.IsPersisted && this.Entries.Count > 2)
				{
					foreach (TransactionEntryViewModel entry in this.Entries)
					{
						entry.PropertyChanged += this.Splits_PropertyChanged;
						if (entry.Account != this.ThisAccount)
						{
							this.splits.Add(entry);
						}
					}

					this.CreateVolatileSplit();
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

	public bool IsEmpty => string.IsNullOrWhiteSpace(this.Payee) && string.IsNullOrWhiteSpace(this.Memo) && this.Amount == 0 && this.OtherAccount is null && !this.ContainsSplits;

	public override bool IsReadyToSave => string.IsNullOrEmpty(this.Error) && (!this.IsEmpty || this.wasEverNonEmpty) && this.NonVolatileSplits.All(e => e.IsReadyToSaveIsolated);

	private IEnumerable<TransactionEntryViewModel> NonVolatileSplits => this.Splits.Count > 0 && this.Splits[^1].IsEmpty ? this.Splits.Take(this.Splits.Count - 1) : this.Splits;

	/// <summary>
	/// Gets the first entry that impacts <see cref="ThisAccount"/>.
	/// </summary>
	private TransactionEntryViewModel? TopLevelEntry => this.Entries.FirstOrDefault(e => e.Account == this.ThisAccount);

	private string DebuggerDisplay => $"Transaction ({this.TransactionId}): {this.When} {this.Payee} {this.Amount}";

	public TransactionEntryViewModel NewSplit()
	{
		_ = this.Splits; // ensure initialized
		bool wasSplit = this.ContainsSplits;
		TransactionEntryViewModel split;
		using (this.SuspendAutoSave(saveOnDisposal: false))
		{
			split = new TransactionEntryViewModel(this)
			{
				Account = this.ThisAccount,
				Asset = this.ThisAccount.CurrencyAsset,
				Amount = wasSplit ? 0 : this.Amount,
			};
			if (this.OtherAccount is object && this.OtherAccount != this.ThisAccount.DocumentViewModel.SplitCategory)
			{
				split.Account = this.OtherAccount;
			}

			this.OtherAccount = this.ThisAccount.DocumentViewModel.SplitCategory;

			int insertPosition = this.splits!.Count > 0 && this.splits![this.splits.Count - 1].IsEmpty ? this.splits.Count - 1 : this.splits.Count;
			this.splits.Insert(insertPosition, split);
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

		using (this.SuspendAutoSave())
		{
			this.EntriesMutable.Remove(split);
			using (this.ApplyingToModel())
			{
				this.ThisAccount.MoneyFile.Delete(split.Model);
			}

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

				this.OtherAccount = split.Account != this.ThisAccount ? split.Account : null;
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
		using (this.SuspendAutoSave(saveOnDisposal: false))
		{
			base.NotifyReassignCategory(oldCategories, newCategory);

			// Update our transaction category if applicable.
			if (!this.ContainsSplits)
			{
				this.SetOtherAccountBasedOnEntries();
			}
		}
	}

	protected internal override void NotifyAccountDeleted(ICollection<int> accountIds)
	{
		base.NotifyAccountDeleted(accountIds);
		if (!this.ContainsSplits)
		{
			this.SetOtherAccountBasedOnEntries();
		}
	}

	protected override void ApplyToCore()
	{
		this.Transaction.Payee = this.Payee;
		this.Transaction.CheckNumber = this.CheckNumber;

		if (this.ContainsSplits)
		{
			// Calculate the amount for the 'balance'.
			decimal balanceAmount = this.Splits.Sum(split => split.Amount);

			// Review each split and create an entry if no match exists.
			HashSet<TransactionEntryViewModel> unrecognizedEntries = new(this.Entries);
			foreach (TransactionEntryViewModel split in this.Splits)
			{
				if (split.IsEmpty)
				{
					continue;
				}

				if (this.Entries.Contains(split))
				{
					unrecognizedEntries.Remove(split);
				}
				else
				{
					this.EntriesMutable.Add(split);
				}
			}

			// Look for one more entry that would match the 'rest' that this top-level transaction represents,
			// and update or add it if necessary.
			TransactionEntryViewModel? balanceEntry = unrecognizedEntries.FirstOrDefault(entry => entry.Account == this.ThisAccount);
			if (balanceEntry is null)
			{
				// Create one.
				balanceEntry = new TransactionEntryViewModel(this) { Account = this.ThisAccount, Amount = balanceAmount, Asset = this.ThisAccount.CurrencyAsset };
				this.EntriesMutable.Add(balanceEntry);
			}
			else
			{
				// Consider the balance entry recognized.
				unrecognizedEntries.Remove(balanceEntry);

				// Update the entry.
				balanceEntry.Amount = balanceAmount;
			}

			// Remove extraneous entries (from removed splits).
			foreach (TransactionEntryViewModel entry in unrecognizedEntries)
			{
				this.EntriesMutable.Remove(entry);
			}
		}
		else
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
						this.EntriesMutable.Add(new TransactionEntryViewModel(this)
						{
							Account = this.OtherAccount,
							Amount = this.Amount, // otherEntry will negate this amount because it's a foreign account.
							Asset = this.ThisAccount.CurrencyAsset,
						});
					}

					break;
				case 2:
					TransactionEntryViewModel ourEntry = this.Entries[0].Account == this.ThisAccount ? this.Entries[0] : this.Entries[1];
					TransactionEntryViewModel otherEntry = this.Entries[0].Account == this.ThisAccount ? this.Entries[1] : this.Entries[0];
					ourEntry.Amount = this.Amount;
					if (this.OtherAccount is object)
					{
						otherEntry.Amount = this.Amount; // otherEntry will negate this amount because it's a foreign account.
						otherEntry.Account = this.OtherAccount;
					}
					else
					{
						this.EntriesMutable.Remove(otherEntry);
					}

					break;
				default:
					// Was previously a split but is no longer.
					throw new NotSupportedException();
			}
		}

		this.Transaction.Action =
			this.Entries.Count == 2 && this.Entries.All(e => e.Account?.Type is not (null or Account.AccountType.Category)) ? TransactionAction.Transfer :
			this.Entries.Count == 1 && this.Entries[0].Amount > 0 ? TransactionAction.Deposit :
			this.Entries.Count == 1 && this.Entries[0].Amount < 0 ? TransactionAction.Withdraw :
			TransactionAction.Unspecified;

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
			_ => this.ThisAccount.DocumentViewModel.SplitCategory,
		};
	}

	private TransactionEntryViewModel CreateVolatileSplit()
	{
		// Always add one more "volatile" transaction at the end as a placeholder to add new data.
		_ = this.Splits;
		using (this.SuspendAutoSave(saveOnDisposal: false))
		{
			TransactionEntryViewModel volatileViewModel = new(this)
			{
				Asset = this.ThisAccount.CurrencyAsset,
			};
			this.splits!.Add(volatileViewModel);
			volatileViewModel.Saved += this.VolatileSplitTransaction_Saved;
			volatileViewModel.PropertyChanged += this.Splits_PropertyChanged;
			return volatileViewModel;
		}
	}

	private void VolatileSplitTransaction_Saved(object? sender, EventArgs args)
	{
		TransactionEntryViewModel? volatileSplit = (TransactionEntryViewModel?)sender;
		Assumes.NotNull(volatileSplit);
		volatileSplit.Saved -= this.VolatileSplitTransaction_Saved;

		// We need a new volatile transaction.
		this.CreateVolatileSplit();
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
					if (!await userNotification.ConfirmAsync("This operation will delete all splits.", defaultConfirm: false, cancellationToken))
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
