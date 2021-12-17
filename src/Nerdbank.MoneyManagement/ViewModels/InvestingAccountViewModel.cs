// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class InvestingAccountViewModel : AccountViewModel
{
	private SortedObservableCollection<InvestingTransactionViewModel>? transactions;

	public InvestingAccountViewModel(Account? model, DocumentViewModel documentViewModel)
		: base(model, documentViewModel)
	{
		this.Type = Account.AccountType.Investing;
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)] // It's lazily initialized, and we don't want the debugger to trip over it.
	public IReadOnlyList<InvestingTransactionViewModel> Transactions
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
						InvestingTransactionViewModel transactionViewModel = new(this, transaction);
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

	protected override bool IsEmpty => !this.Transactions.Any(t => t.IsPersisted);

	protected override bool IsPopulated => this.transactions is object;

	public override void DeleteTransaction(TransactionViewModel transaction)
	{
		Requires.Argument(transaction.ThisAccount == this, nameof(transaction), "This transaction does not belong to this account.");
		Verify.Operation(this.transactions is object, "Our transactions are not initialized yet.");
		var investingTransaction = (InvestingTransactionViewModel)transaction;

		if (this.MoneyFile is object && investingTransaction.Model is object)
		{
			using IDisposable? undo = this.MoneyFile.UndoableTransaction($"Deleted transaction from {investingTransaction.When.Date}", investingTransaction.Model);

			if (!this.MoneyFile.Delete(investingTransaction.Model))
			{
				// We may be removing a view model whose model was never persisted. Make sure we directly remove the view model from our own collection.
				this.RemoveTransactionFromViewModel(investingTransaction);
			}
		}

		if (!investingTransaction.IsPersisted)
		{
			// We deleted the volatile transaction (new row placeholder). Recreate it.
			this.CreateVolatileTransaction();
		}
	}

	internal override InvestingTransactionViewModel? FindTransaction(int id)
	{
		foreach (InvestingTransactionViewModel transactionViewModel in this.Transactions)
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
		else if (!removedFromAccount)
		{
			// This may be a new transaction we need to add. Only add top-level transactions or foreign splits.
			if (transaction.ParentTransactionId is null || this.FindTransaction(transaction.ParentTransactionId.Value) is null)
			{
				this.transactions.Add(new InvestingTransactionViewModel(this, transaction));
			}
		}
	}

	protected override void RemoveTransactionFromViewModel(TransactionViewModel transactionViewModel)
	{
		if (this.transactions is null)
		{
			// Nothing to remove when the collection isn't initialized.
			return;
		}

		int index = this.transactions.IndexOf((InvestingTransactionViewModel)transactionViewModel);
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
	}

	private void CreateVolatileTransaction()
	{
		// Always add one more "volatile" transaction at the end as a placeholder to add new data.
		_ = this.Transactions;
		Transaction volatileModel = new()
		{
			When = DateTime.Today,
		};
		InvestingTransactionViewModel volatileViewModel = new(this, volatileModel);
		this.transactions!.Add(volatileViewModel);
		volatileViewModel.Saved += this.VolatileTransaction_Saved;
	}

	private void VolatileTransaction_Saved(object? sender, EventArgs args)
	{
		InvestingTransactionViewModel? volatileTransaction = (InvestingTransactionViewModel?)sender;
		Assumes.NotNull(volatileTransaction);
		volatileTransaction.Saved -= this.VolatileTransaction_Saved;

		// We need a new volatile transaction.
		this.CreateVolatileTransaction();
	}

	private class TransactionSort : IOptimizedComparer<InvestingTransactionViewModel>
	{
		internal static readonly TransactionSort Instance = new();

		private TransactionSort()
		{
		}

		public int Compare(InvestingTransactionViewModel? x, InvestingTransactionViewModel? y)
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

			order = x.Id is null
				? (y.Id is null ? 0 : -1)
				: (y.Id is null) ? 1 : 0;
			if (order != 0)
			{
				return order;
			}

			return 0;
		}

		public bool IsPropertySignificant(string propertyName) => propertyName is nameof(InvestingTransactionViewModel.When) or nameof(InvestingTransactionViewModel.Id);
	}
}
