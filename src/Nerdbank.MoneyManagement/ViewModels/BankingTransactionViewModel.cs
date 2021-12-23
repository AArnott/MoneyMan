// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

	private ObservableCollection<SplitTransactionViewModel>? splits;
	private DateTime when;
	private int? checkNumber;
	private decimal amount;
	private string? memo;
	private ClearedState cleared = ClearedState.None;
	private string? payee;
	private ITransactionTarget? categoryOrTransfer;
	private decimal balance;
	private SplitTransactionViewModel? selectedSplit;

	public BankingTransactionViewModel(BankingAccountViewModel thisAccount, Transaction? transaction)
		: base(thisAccount)
	{
		this.AutoSave = true;

		if (transaction is object)
		{
			this.CopyFrom(transaction);
		}

		this.SplitCommand = new SplitCommandImpl(this);
		this.DeleteSplitCommand = new DeleteSplitCommandImpl(this);

		this.RegisterDependentProperty(nameof(this.AmountIsReadOnly), nameof(this.CategoryOrTransferIsReadOnly));
		this.RegisterDependentProperty(nameof(this.ContainsSplits), nameof(this.CategoryOrTransferIsReadOnly));
		this.RegisterDependentProperty(nameof(this.ContainsSplits), nameof(this.SplitCommandToolTip));
		this.RegisterDependentProperty(nameof(this.Cleared), nameof(this.ClearedShortCaption));
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.AmountFormatted));
		this.RegisterDependentProperty(nameof(this.Balance), nameof(this.BalanceFormatted));
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

	public DateTime When
	{
		get => this.when;
		set
		{
			this.ThrowIfSplitInForeignAccount();
			this.SetProperty(ref this.when, value);

			foreach (SplitTransactionViewModel split in this.Splits.Where(s => s.IsPersisted))
			{
				if (split.Model is object)
				{
					split.Model.When = value;
					split.Save();
				}
			}
		}
	}

	/// <summary>
	/// Gets a value indicating whether the <see cref="When"/> property should be considered readonly.
	/// </summary>
	public bool WhenIsReadOnly => this.IsSplitInForeignAccount;

	public int? CheckNumber
	{
		get => this.checkNumber;
		set => this.SetProperty(ref this.checkNumber, value);
	}

	public decimal Amount
	{
		get => this.amount;
		set
		{
			this.ThrowIfSplitInForeignAccount();
			this.SetProperty(ref this.amount, value);
		}
	}

	public string? AmountFormatted => this.ThisAccount.CurrencyAsset?.Format(this.Amount);

	/// <summary>
	/// Gets a value indicating whether the <see cref="Amount"/> property should be considered readonly.
	/// </summary>
	public bool AmountIsReadOnly => this.IsSplitInForeignAccount || this.ContainsSplits;

	public string? Memo
	{
		get => this.memo;
		set => this.SetProperty(ref this.memo, value);
	}

	public ClearedState Cleared
	{
		get => this.cleared;
		set => this.SetProperty(ref this.cleared, value);
	}

	public string ClearedShortCaption => SharedClearedStates[(int)this.Cleared].ShortCaption;

	public string? Payee
	{
		get => this.payee;
		set
		{
			this.ThrowIfSplitInForeignAccount();
			this.SetProperty(ref this.payee, value);

			foreach (SplitTransactionViewModel split in this.Splits.Where(s => s.IsPersisted))
			{
				if (split.Model is object)
				{
					split.Model.Payee = value;
					split.Save();
				}
			}
		}
	}

	/// <summary>
	/// Gets a value indicating whether the <see cref="Payee"/> property should be considered readonly.
	/// </summary>
	public bool PayeeIsReadOnly => this.IsSplitInForeignAccount;

	public ITransactionTarget? CategoryOrTransfer
	{
		get => this.categoryOrTransfer;
		set
		{
			if (value == this.categoryOrTransfer)
			{
				return;
			}

			Verify.Operation(!this.ContainsSplits || value == SplitCategoryPlaceholder.Singleton, "Cannot set category or transfer on a transaction containing splits.");
			this.ThrowIfSplitInForeignAccount();
			this.SetProperty(ref this.categoryOrTransfer, value);
		}
	}

	/// <summary>
	/// Gets a value indicating whether the <see cref="CategoryOrTransfer"/> property should be considered readonly.
	/// </summary>
	public bool CategoryOrTransferIsReadOnly => this.ContainsSplits || this.IsSplitInForeignAccount;

	public IEnumerable<ITransactionTarget> AvailableTransactionTargets
		=> this.ThisAccount.DocumentViewModel.TransactionTargets.Where(tt => tt != this.ThisAccount && tt != SplitCategoryPlaceholder.Singleton);

	////[SplitSumMatchesTransactionAmount]
	public IReadOnlyList<SplitTransactionViewModel> Splits
	{
		get
		{
			if (this.splits is null)
			{
				this.splits = new();
				if (this.MoneyFile is object && this.IsPersisted)
				{
					SQLite.TableQuery<Transaction> splits = this.MoneyFile.Transactions
						.Where(tx => tx.ParentTransactionId == this.Id);
					foreach (Transaction split in splits)
					{
						SplitTransactionViewModel splitViewModel = new(this, split);
						this.splits.Add(splitViewModel);
						splitViewModel.PropertyChanged += this.Splits_PropertyChanged;
					}

					if (this.splits.Count > 0)
					{
						this.CreateVolatileSplit();
					}
				}
			}

			return this.splits;
		}
	}

	public bool ContainsSplits => this.Splits.Count > 0;

	public SplitTransactionViewModel? SelectedSplit
	{
		get => this.selectedSplit;
		set => this.SetProperty(ref this.selectedSplit, value);
	}

	/// <summary>
	/// Gets a value indicating whether this "transaction" is really just synthesized to represent the split line item(s)
	/// of a transaction in another account that transfer to/from this account.
	/// </summary>
	public bool IsSplitMemberOfParentTransaction => this.Model?.ParentTransactionId.HasValue is true;

	/// <summary>
	/// Gets a value indicating whether this is a member of a split transaction that appears (as its own top-level transaction) in another account (as a transfer).
	/// </summary>
	public bool IsSplitInForeignAccount => this.Model?.ParentTransactionId is int parentTransactionId && this.ThisAccount.FindTransaction(parentTransactionId) is null;

	public decimal Balance
	{
		get => this.balance;
		set => this.SetProperty(ref this.balance, value);
	}

	public string? BalanceFormatted => this.ThisAccount.CurrencyAsset?.Format(this.Balance);

	/// <inheritdoc cref="TransactionViewModel.ThisAccount"/>
	public new BankingAccountViewModel ThisAccount => (BankingAccountViewModel)base.ThisAccount;

	private string DebuggerDisplay => $"Transaction ({this.Id}): {this.When} {this.Payee} {this.Amount}";

	public SplitTransactionViewModel NewSplit()
	{
		Verify.Operation(!this.IsSplitMemberOfParentTransaction, "Cannot split a transaction that is already a member of a split transaction.");

		if (!this.IsPersisted)
		{
			// Persist this transaction so the splits can refer to it.
			this.Save();
		}

		_ = this.Splits; // ensure initialized
		bool wasSplit = this.ContainsSplits;
		SplitTransactionViewModel split = new(this, null)
		{
			Amount = wasSplit ? 0 : this.Amount,
		};
		using (this.SuspendAutoSave())
		{
			if (this.CategoryOrTransfer != SplitCategoryPlaceholder.Singleton)
			{
				split.CategoryOrTransfer = this.CategoryOrTransfer;
			}

			this.CategoryOrTransfer = SplitCategoryPlaceholder.Singleton;

			split.Model = new()
			{
				When = this.When,
				Payee = this.Payee,
			};
			this.splits!.Add(split);
		}

		split.Save();
		split.PropertyChanged += this.Splits_PropertyChanged;
		if (!wasSplit)
		{
			this.OnPropertyChanged(nameof(this.ContainsSplits));
			this.CreateVolatileSplit();
		}

		return split;
	}

	public void DeleteSplit(SplitTransactionViewModel split)
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

		if (split.Model is object)
		{
			this.ThisAccount.MoneyFile?.Delete(split.Model);
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

			this.CategoryOrTransfer = split.CategoryOrTransfer;
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
			foreach (SplitTransactionViewModel split in this.Splits.ToList())
			{
				this.DeleteSplit(split);
			}

			// If there was more than one split item, we don't know which category to apply to the original transaction,
			// so clear it explicitly since otherwise the DeleteSplit call above would cause us to inherit the last split's category.
			if (originalSplitCount > 1)
			{
				this.CategoryOrTransfer = null;

				// Also restore the Amount to the original multi-split value.
				this.Amount = amount;
			}
		}
	}

	public BankingTransactionViewModel? GetSplitParent()
	{
		int? splitParentId = this.Model?.ParentTransactionId;
		if (splitParentId is null || this.MoneyFile is null)
		{
			return null;
		}

		Transaction parentTransaction = this.MoneyFile.Transactions.First(t => t.Id == splitParentId);

		// TODO: How to determine which account is preferable when a split transaction exists in two accounts?
		int? accountId = parentTransaction.CreditAccountId ?? parentTransaction.DebitAccountId;
		if (accountId is null)
		{
			return null;
		}

		BankingAccountViewModel parentAccount = (BankingAccountViewModel)this.ThisAccount.DocumentViewModel.GetAccount(accountId.Value);
		return parentAccount.Transactions.First(tx => tx.Id == parentTransaction.Id);
	}

	public void JumpToSplitParent()
	{
		BankingTransactionViewModel? splitParent = this.GetSplitParent();
		Verify.Operation(splitParent is object, "Cannot jump to split parent from a transaction that is not a member of a split transaction.");
		this.ThisAccount.DocumentViewModel.BankingPanel.SelectedAccount = splitParent.ThisAccount;
		this.ThisAccount.DocumentViewModel.SelectedTransaction = splitParent;
	}

	public decimal GetSplitTotal()
	{
		Verify.Operation(this.ContainsSplits, "Not a split transaction.");
		return this.Splits.Sum(s => s.Amount);
	}

	internal void ThrowIfSplitInForeignAccount() => Verify.Operation(!this.IsSplitInForeignAccount, "This operation is not allowed when applied to a split transaction in the context of a transfer account. Retry on the split in the account of the top-level account.");

	protected override void ApplyToCore(Transaction transaction)
	{
		Requires.NotNull(transaction, nameof(transaction));

		transaction.Payee = this.Payee;
		transaction.When = this.When;
		transaction.Memo = this.Memo;
		transaction.CheckNumber = this.CheckNumber;

		if (this.ContainsSplits)
		{
			transaction.CategoryId = Category.Split;
			foreach (SplitTransactionViewModel split in this.Splits.ToList())
			{
				split.Save();
			}

			// Split transactions always record the same account for credit and debit,
			// and always have their own Amount set to 0.
			transaction.CreditAccountId = this.ThisAccount.Id;
			transaction.DebitAccountId = this.ThisAccount.Id;
			transaction.CreditAmount = null;
			transaction.DebitAmount = null;
			transaction.CreditAssetId = null;
			transaction.DebitAssetId = null;
		}
		else
		{
			transaction.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;

			if (this.Amount < 0)
			{
				transaction.DebitCleared = this.Cleared;
				transaction.DebitAccountId = this.ThisAccount.Id;
				transaction.DebitAmount = -this.Amount;
				transaction.DebitAssetId = this.ThisAccount.CurrencyAsset?.Id;
				if (this.CategoryOrTransfer is AccountViewModel xfer)
				{
					transaction.CreditAccountId = xfer.Id;
					transaction.CreditAmount = -this.Amount;
					transaction.Action = TransactionAction.Transfer;
				}
				else
				{
					transaction.CreditAccountId = null;
					transaction.CreditAmount = null;
					transaction.Action = TransactionAction.Withdraw;
				}
			}
			else
			{
				transaction.CreditCleared = this.Cleared;
				transaction.CreditAccountId = this.ThisAccount.Id;
				transaction.CreditAmount = this.Amount;
				transaction.CreditAssetId = this.ThisAccount.CurrencyAsset?.Id;
				if (this.CategoryOrTransfer is AccountViewModel xfer)
				{
					transaction.DebitAccountId = xfer.Id;
					transaction.DebitAmount = this.Amount;
					transaction.Action = TransactionAction.Transfer;
				}
				else
				{
					transaction.DebitAccountId = null;
					transaction.DebitAmount = null;
					transaction.Action = TransactionAction.Deposit;
				}
			}
		}
	}

	protected override void CopyFromCore(Transaction transaction)
	{
		Requires.NotNull(transaction, nameof(transaction));

		this.SetProperty(ref this.payee, transaction.Payee, nameof(this.Payee));
		this.SetProperty(ref this.when, transaction.When, nameof(this.When));
		this.Memo = transaction.Memo;
		this.CheckNumber = transaction.CheckNumber;
		this.Cleared = transaction.CreditAccountId == this.ThisAccount.Id ? transaction.CreditCleared : transaction.DebitCleared;

		if (transaction.CategoryId is int categoryId)
		{
			if (categoryId == Category.Split)
			{
				// The split entities themselves are lazily initialized.
				this.categoryOrTransfer = SplitCategoryPlaceholder.Singleton;
			}
			else
			{
				this.SetProperty(ref this.categoryOrTransfer, this.ThisAccount.DocumentViewModel?.GetCategory(categoryId) ?? throw new InvalidOperationException(), nameof(this.CategoryOrTransfer));
				if (this.splits is object)
				{
					this.splits.Clear();
				}

				this.OnPropertyChanged(nameof(this.Splits));
			}
		}
		else if (transaction.CreditAccountId is int creditId && this.ThisAccount.Id != creditId)
		{
			this.SetProperty(ref this.categoryOrTransfer, this.ThisAccount.DocumentViewModel?.GetAccount(creditId) ?? throw new InvalidOperationException(), nameof(this.CategoryOrTransfer));
		}
		else if (transaction.DebitAccountId is int debitId && this.ThisAccount.Id != debitId)
		{
			this.SetProperty(ref this.categoryOrTransfer, this.ThisAccount.DocumentViewModel?.GetAccount(debitId) ?? throw new InvalidOperationException(), nameof(this.CategoryOrTransfer));
		}
		else
		{
			this.SetProperty(ref this.categoryOrTransfer, null, nameof(this.CategoryOrTransfer));
		}

		// Split transactions' amounts are always calculated to be the sum of the splits.
		if (this.ContainsSplits)
		{
			this.SetAmountBasedOnSplits();
		}
		else
		{
			decimal amount =
				transaction.CreditAccountId == this.ThisAccount.Id ? (transaction.CreditAmount ?? 0) :
				transaction.DebitAccountId == this.ThisAccount.Id ? (-transaction.DebitAmount ?? 0) :
				0;
			this.SetProperty(ref this.amount, amount, nameof(this.Amount));
		}
	}

	protected override bool IsPersistedProperty(string propertyName)
	{
		if (propertyName is nameof(this.Balance) or nameof(this.ContainsSplits) or nameof(this.SelectedSplit))
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

	private SplitTransactionViewModel CreateVolatileSplit()
	{
		// Always add one more "volatile" transaction at the end as a placeholder to add new data.
		_ = this.Splits;
		var volatileModel = new Transaction()
		{
			When = this.When,
			Payee = this.Payee,
		};
		SplitTransactionViewModel volatileViewModel = new(this, volatileModel);
		this.splits!.Add(volatileViewModel);
		volatileViewModel.Saved += this.VolatileSplitTransaction_Saved;
		volatileViewModel.PropertyChanged += this.Splits_PropertyChanged;
		return volatileViewModel;
	}

	private void VolatileSplitTransaction_Saved(object? sender, EventArgs args)
	{
		SplitTransactionViewModel? volatileSplit = (SplitTransactionViewModel?)sender;
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
