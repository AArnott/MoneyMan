// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class BankingAccountViewModel : AccountViewModel
{
	private SortedObservableCollection<BankingTransactionViewModel>? transactions;

	public BankingAccountViewModel(Account model, DocumentViewModel documentViewModel)
		: base(model, documentViewModel)
	{
		ThrowOnUnexpectedAccountType(nameof(model), Account.AccountType.Banking, model.Type);
		this.Type = Account.AccountType.Banking;
		this.RegisterDependentProperty(nameof(this.IsEmpty), nameof(this.CurrencyAssetIsReadOnly));
		this.CopyFrom(model);
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)] // It's lazily initialized, and we don't want the debugger to trip over it.
	public IReadOnlyList<BankingTransactionViewModel> Transactions
	{
		get
		{
			if (this.transactions is null)
			{
				this.transactions = new(TransactionSort.Instance);
				if (this.IsPersisted)
				{
					this.transactions.AddRange(this.CreateEntryViewModels<BankingTransactionViewModel>());
					this.UpdateBalances(0);
				}

				// Always add one more "volatile" transaction at the end as a placeholder to add new data.
				this.CreateVolatileTransaction();

				this.transactions.CollectionChanged += this.Transactions_CollectionChanged;
			}

			return this.transactions;
		}
	}

	public override string? TransferTargetName => $"[{this.Name}]";

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

		BankingTransactionViewModel viewModel = new(this)
		{
			When = DateTime.Now,
		};

		_ = this.Transactions; // make sure our collection is initialized.
		this.transactions!.Add(viewModel);

		return viewModel;
	}

	public override void DeleteTransaction(TransactionViewModel transaction)
	{
		Requires.Argument(transaction.ThisAccount == this, nameof(transaction), "This transaction does not belong to this account.");
		Verify.Operation(this.transactions is object, "Our transactions are not initialized yet.");
		var bankingTransaction = (BankingTransactionViewModel)transaction;

		using IDisposable? undo = this.MoneyFile.UndoableTransaction($"Deleted transaction from {bankingTransaction.When.Date}", bankingTransaction);
		if (!this.MoneyFile.Delete(bankingTransaction.Transaction))
		{
			// We may be removing a view model whose model was never persisted. Make sure we directly remove the view model from our own collection.
			this.RemoveTransactionFromViewModel(bankingTransaction);
		}

		if (!bankingTransaction.IsPersisted)
		{
			// We deleted the volatile transaction (new row placeholder). Recreate it.
			this.CreateVolatileTransaction();
		}
	}

	public override BankingTransactionViewModel? FindTransaction(int? id)
	{
		if (id is null or 0)
		{
			return null;
		}

		foreach (BankingTransactionViewModel transactionViewModel in this.Transactions)
		{
			if (transactionViewModel.Transaction.Id == id)
			{
				return transactionViewModel;
			}
		}

		return null;
	}

	internal override void NotifyAccountDeleted(ICollection<int> accountIds)
	{
		if (this.transactions is object && accountIds.Count > 0)
		{
			foreach (BankingTransactionViewModel transaction in this.transactions)
			{
				transaction.NotifyAccountDeleted(accountIds);
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

	internal void NotifyReassignCategory(ICollection<CategoryAccountViewModel> oldCategories, CategoryAccountViewModel? newCategory)
	{
		if (this.transactions is object)
		{
			foreach (BankingTransactionViewModel transaction in this.transactions)
			{
				transaction.NotifyReassignCategory(oldCategories, newCategory);
			}
		}
	}

	protected override BankingTransactionViewModel CreateTransactionViewModel(IReadOnlyList<TransactionAndEntry> transactionDetails) => new BankingTransactionViewModel(this, transactionDetails);

	protected override int AddTransaction(TransactionViewModel transactionViewModel) => this.transactions?.Add((BankingTransactionViewModel)transactionViewModel) ?? throw new InvalidOperationException();

	protected override int GetTransactionIndex(TransactionViewModel transaction) => this.transactions?.IndexOf((BankingTransactionViewModel)transaction) ?? throw new InvalidOperationException();

	protected override void UpdateBalances(int index) => this.UpdateBalances(index, -1);

	protected override bool IsPersistedProperty(string propertyName)
	{
		return base.IsPersistedProperty(propertyName) && propertyName is not (nameof(this.Value) or nameof(this.TransferTargetName));
	}

	protected override void CopyFromCore()
	{
		base.CopyFromCore();

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
		if (index >= 0)
		{
			this.transactions.RemoveAt(index);
			this.UpdateBalances(index);
		}
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
		BankingTransactionViewModel volatileViewModel = new(this)
		{
			When = DateTime.Today,
		};
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

			int order = Utilities.CompareNullOrZeroComesLast(x.TransactionId, y.TransactionId);
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

			return StringComparer.CurrentCultureIgnoreCase.Compare(x.Payee, y.Payee);
		}

		public bool IsPropertySignificant(string propertyName) =>
			propertyName is nameof(BankingTransactionViewModel.When) or nameof(BankingTransactionViewModel.Amount) or nameof(BankingTransactionViewModel.TransactionId) or nameof(BankingTransactionViewModel.Payee);
	}
}
