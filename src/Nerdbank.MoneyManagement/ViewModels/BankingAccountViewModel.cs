// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class BankingAccountViewModel : AccountViewModel
{
	private SortedObservableCollection<BankingTransactionViewModel>? transactions;

	public BankingAccountViewModel(Account? model, DocumentViewModel documentViewModel)
		: base(model, documentViewModel)
	{
		this.Type = Account.AccountType.Banking;
		this.RegisterDependentProperty(nameof(this.IsEmpty), nameof(this.CurrencyAssetIsReadOnly));
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)] // It's lazily initialized, and we don't want the debugger to trip over it.
	public IReadOnlyList<BankingTransactionViewModel> Transactions
	{
		get
		{
			if (this.transactions is null)
			{
				this.transactions = new(TransactionSort.Instance);
				if (this.MoneyFile is object && this.Id.HasValue)
				{
					List<Transaction> transactions = this.MoneyFile.GetTopLevelTransactionsFor(this.Id.Value);
					foreach (Transaction transaction in transactions)
					{
						BankingTransactionViewModel transactionViewModel = new(this, transaction);
						this.transactions.Add(transactionViewModel);
					}

					this.UpdateBalances(0);
				}

				// Always add one more "volatile" transaction at the end as a placeholder to add new data.
				this.CreateVolatileTransaction();

				this.transactions.CollectionChanged += this.Transactions_CollectionChanged;
			}

			return this.transactions;
		}
	}

	protected override bool IsPopulated => this.transactions is object;

	protected override bool IsEmpty => !this.Transactions.Any(t => t.IsPersisted);

	/// <summary>
	/// Creates a new <see cref="BankingTransactionViewModel"/> for this account.
	/// </summary>
	/// <returns>A new <see cref="BankingTransactionViewModel"/> for an uninitialized transaction.</returns>
	public BankingTransactionViewModel NewTransaction()
	{
		// Make sure our collection has been initialized by now.
		_ = this.Transactions;

		BankingTransactionViewModel viewModel = new(this, null);
		viewModel.When = DateTime.Now;
		viewModel.Model = new();

		_ = this.Transactions; // make sure our collection is initialized.
		this.transactions!.Add(viewModel);
		viewModel.Save();

		return viewModel;
	}

	public override void DeleteTransaction(TransactionViewModel transaction)
	{
		Requires.Argument(transaction.ThisAccount == this, nameof(transaction), "This transaction does not belong to this account.");
		Verify.Operation(this.transactions is object, "Our transactions are not initialized yet.");
		var bankingTransaction = (BankingTransactionViewModel)transaction;
		bankingTransaction.ThrowIfSplitInForeignAccount();

		if (this.MoneyFile is object && bankingTransaction.Model is object)
		{
			using IDisposable? undo = this.MoneyFile.UndoableTransaction($"Deleted transaction from {bankingTransaction.When.Date}", bankingTransaction.Model);
			foreach (SplitTransactionViewModel split in bankingTransaction.Splits)
			{
				if (split.Model is object)
				{
					this.MoneyFile.Delete(split.Model);
				}
			}

			if (!this.MoneyFile.Delete(bankingTransaction.Model))
			{
				// We may be removing a view model whose model was never persisted. Make sure we directly remove the view model from our own collection.
				this.RemoveTransactionFromViewModel(bankingTransaction);
			}
		}

		if (!bankingTransaction.IsPersisted)
		{
			// We deleted the volatile transaction (new row placeholder). Recreate it.
			this.CreateVolatileTransaction();
		}
	}

	public override BankingTransactionViewModel? FindTransaction(int id)
	{
		foreach (BankingTransactionViewModel transactionViewModel in this.Transactions)
		{
			if (transactionViewModel.Model?.Id == id)
			{
				return transactionViewModel;
			}
		}

		return null;
	}

	internal override void NotifyTransactionChanged(Transaction transaction)
	{
		if (this.transactions is null)
		{
			// Nothing to refresh.
			return;
		}

		// This transaction may have added or dropped our account as a transfer
		bool removedFromAccount = transaction.CreditAccountId != this.Id && transaction.DebitAccountId != this.Id;
		if (this.FindTransaction(transaction.Id) is { } transactionViewModel)
		{
			if (removedFromAccount)
			{
				this.transactions.Remove(transactionViewModel);
			}
			else
			{
				transactionViewModel.CopyFrom(transaction);
				int index = this.transactions.IndexOf(transactionViewModel);
				if (index >= 0)
				{
					this.UpdateBalances(index);
				}
			}
		}
		else if (transaction.ParentTransactionId.HasValue && this.FindTransaction(transaction.ParentTransactionId.Value) is { } parentTransactionViewModel)
		{
			SplitTransactionViewModel? splitViewModel = parentTransactionViewModel.Splits.FirstOrDefault(s => s.Id == transaction.Id);
			if (splitViewModel is object)
			{
				splitViewModel.CopyFrom(transaction);
				int index = this.transactions.IndexOf(parentTransactionViewModel);
				if (index >= 0)
				{
					this.UpdateBalances(index);
				}
			}
		}
		else if (!removedFromAccount)
		{
			// This may be a new transaction we need to add. Only add top-level transactions or foreign splits.
			if (transaction.ParentTransactionId is null || this.FindTransaction(transaction.ParentTransactionId.Value) is null)
			{
				this.transactions.Add(new BankingTransactionViewModel(this, transaction));
			}
		}
	}

	internal override void NotifyAccountDeleted(ICollection<Account> accounts)
	{
		if (this.transactions is object)
		{
			foreach (BankingTransactionViewModel transaction in this.transactions)
			{
				if (transaction.CategoryOrTransfer is AccountViewModel { Model: Account transferAccount } && accounts.Contains(transferAccount))
				{
					transaction.CategoryOrTransfer = null;
				}
			}
		}
	}

	internal void NotifyAmountChangedOnSplitTransaction(BankingTransactionViewModel transaction)
	{
		Requires.Argument(transaction.ContainsSplits, nameof(transaction), "Only split transactions should be raising this.");
		if (this.transactions is object)
		{
			var index = this.transactions.IndexOf(transaction);
			if (index >= 0)
			{
				this.UpdateBalances(index);
			}
		}
	}

	internal void NotifyReassignCategory(List<CategoryViewModel> oldCategories, CategoryViewModel? newCategory)
	{
		if (this.transactions is object)
		{
			foreach (BankingTransactionViewModel transaction in this.transactions)
			{
				if (transaction.CategoryOrTransfer == SplitCategoryPlaceholder.Singleton)
				{
					foreach (SplitTransactionViewModel split in transaction.Splits)
					{
						if (oldCategories.Contains(split.CategoryOrTransfer))
						{
							split.CategoryOrTransfer = newCategory;
						}
					}
				}
				else if (oldCategories.Contains(transaction.CategoryOrTransfer))
				{
					transaction.CategoryOrTransfer = newCategory;
				}
			}
		}
	}

	protected override bool IsPersistedProperty(string propertyName)
	{
		if (propertyName is nameof(this.Value) or nameof(this.TransferTargetName))
		{
			return false;
		}

		return base.IsPersistedProperty(propertyName);
	}

	protected override void CopyFromCore(Account account)
	{
		base.CopyFromCore(account);

		// Force reinitialization.
		this.transactions = null;
	}

	protected override void RemoveTransactionFromViewModel(TransactionViewModel transactionViewModel)
	{
		if (this.transactions is null)
		{
			// Nothing to remove when the collection isn't initialized.
			return;
		}

		int index = this.transactions.IndexOf((BankingTransactionViewModel)transactionViewModel);
		this.transactions.RemoveAt(index);
		this.UpdateBalances(index);
	}

	private void Transactions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		if (e.NewStartingIndex >= 0)
		{
			if (e.OldStartingIndex >= 0)
			{
				this.UpdateBalances(e.NewStartingIndex, e.OldStartingIndex);
			}
			else
			{
				this.UpdateBalances(e.NewStartingIndex);
			}
		}
		else if (e.OldStartingIndex >= 0)
		{
			this.UpdateBalances(e.OldStartingIndex);
		}
	}

	private void UpdateBalances(int changedIndex1, int changedIndex2 = -1)
	{
		int startingIndex = changedIndex2 == -1 ? changedIndex1 : Math.Min(changedIndex1, changedIndex2);
		decimal balance = startingIndex == 0 ? 0m : this.transactions![startingIndex - 1].Balance;
		for (int i = startingIndex; i < this.transactions!.Count; i++)
		{
			BankingTransactionViewModel transaction = this.transactions[i];
			balance += transaction.Amount;
			if (transaction.Balance == balance && i >= changedIndex2)
			{
				// The balance is already what it needs to be,
				// and we've already reached the last of the one or two positions where transactions may have changed.
				// Short circuit as a perf win.
				return;
			}

			transaction.Balance = balance;
		}
	}

	private void CreateVolatileTransaction()
	{
		// Always add one more "volatile" transaction at the end as a placeholder to add new data.
		_ = this.Transactions;
		var volatileModel = new Transaction()
		{
			When = DateTime.Today,
		};
		BankingTransactionViewModel volatileViewModel = new(this, volatileModel);
		this.transactions!.Add(volatileViewModel);
		volatileViewModel.Saved += this.VolatileTransaction_Saved;
	}

	private void VolatileTransaction_Saved(object? sender, EventArgs args)
	{
		BankingTransactionViewModel? volatileTransaction = (BankingTransactionViewModel?)sender;
		Assumes.NotNull(volatileTransaction);
		volatileTransaction.Saved -= this.VolatileTransaction_Saved;

		// We need a new volatile transaction.
		this.CreateVolatileTransaction();
	}

	private class TransactionSort : IOptimizedComparer<BankingTransactionViewModel>
	{
		internal static readonly TransactionSort Instance = new();

		private TransactionSort()
		{
		}

		public int Compare(BankingTransactionViewModel? x, BankingTransactionViewModel? y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}
			else if (y is null)
			{
				return 1;
			}

			int order = Utilities.CompareNullOrZeroComesLast(x.Id, y.Id);
			if (order != 0)
			{
				return order;
			}

			order = x.When.CompareTo(y.When);
			if (order != 0)
			{
				return order;
			}

			order = -x.Amount.CompareTo(y.Amount);
			if (order != 0)
			{
				return order;
			}

			order = x.Id is null
				? (y.Id is null ? 0 : -1)
				: (y.Id is null) ? 1 : 0;
			if (order != 0)
			{
				return order;
			}

			return StringComparer.CurrentCultureIgnoreCase.Compare(x.Payee, y.Payee);
		}

		public bool IsPropertySignificant(string propertyName) => propertyName is nameof(BankingTransactionViewModel.When) or nameof(BankingTransactionViewModel.Amount) or nameof(BankingTransactionViewModel.Id) or nameof(BankingTransactionViewModel.Payee);
	}
}
