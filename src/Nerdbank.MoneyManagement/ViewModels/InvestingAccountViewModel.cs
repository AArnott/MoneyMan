// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class InvestingAccountViewModel : AccountViewModel
{
	private SortedObservableCollection<InvestingTransactionViewModel>? transactions;

	public InvestingAccountViewModel(Account model, DocumentViewModel documentViewModel)
		: base(model, documentViewModel)
	{
		ThrowOnUnexpectedAccountType(nameof(model), Account.AccountType.Investing, model.Type);
		this.Type = Account.AccountType.Investing;
		this.CopyFrom(model);
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)] // It's lazily initialized, and we don't want the debugger to trip over it.
	public IReadOnlyList<InvestingTransactionViewModel> Transactions
	{
		get
		{
			if (this.transactions is null)
			{
				this.transactions = new(TransactionSort.Instance);
				if (this.IsPersisted)
				{
					this.transactions.AddRange(this.CreateEntryViewModels(tes => new InvestingTransactionViewModel(this, tes)));
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

	protected override bool IsEmpty => !this.Transactions.Any(t => t.IsPersisted);

	protected override bool IsPopulated => this.transactions is object;

	public override void DeleteTransaction(TransactionViewModel transaction)
	{
		Requires.Argument(transaction.ThisAccount == this, nameof(transaction), "This transaction does not belong to this account.");
		Verify.Operation(this.transactions is object, "Our transactions are not initialized yet.");
		var investingTransaction = (InvestingTransactionViewModel)transaction;

		using IDisposable? undo = this.MoneyFile.UndoableTransaction($"Deleted transaction from {investingTransaction.When.Date}", investingTransaction);

		if (!this.MoneyFile.Delete(investingTransaction.Transaction))
		{
			// We may be removing a view model whose model was never persisted. Make sure we directly remove the view model from our own collection.
			this.RemoveTransactionFromViewModel(investingTransaction);
		}

		if (!investingTransaction.IsPersisted)
		{
			// We deleted the volatile transaction (new row placeholder). Recreate it.
			this.CreateVolatileTransaction();
		}
	}

	public override InvestingTransactionViewModel? FindTransaction(int? id)
	{
		if (id is null or 0)
		{
			return null;
		}

		foreach (InvestingTransactionViewModel transactionViewModel in this.Transactions)
		{
			if (transactionViewModel.Transaction.Id == id)
			{
				return transactionViewModel;
			}
		}

		return null;
	}

	internal override void NotifyTransactionChanged(int transactionId)
	{
		if (this.transactions is null)
		{
			// Nothing to refresh.
			return;
		}

		////// This transaction may have added or dropped our account as a transfer
		////bool removedFromAccount = !transactionAndEntries.Any(te => te.AccountId == this.Id);
		////if (this.FindTransaction(transactionAndEntries.First().TransactionId) is { } transactionViewModel)
		////{
		////	if (removedFromAccount)
		////	{
		////		this.transactions.Remove(transactionViewModel);
		////	}
		////	else
		////	{
		////		transactionViewModel.CopyFrom(transactionAndEntries);
		////		int index = this.transactions.IndexOf(transactionViewModel);
		////		if (index >= 0)
		////		{
		////			this.UpdateBalances(index);
		////		}
		////	}
		////}
		////else if (!removedFromAccount)
		////{
		////	this.transactions.Add(new InvestingTransactionViewModel(this, transactionAndEntries));
		////}
	}

	internal override void NotifyAccountDeleted(ICollection<int> accountIds)
	{
		if (this.transactions is object)
		{
			foreach (InvestingTransactionViewModel transaction in this.transactions)
			{
				if (transaction.DepositAccount is AccountViewModel { Model: Account creditAccount } && accountIds.Contains(creditAccount.Id))
				{
					transaction.DepositAccount = null;
				}
				else if (transaction.WithdrawAccount is AccountViewModel { Model: Account debitAccount } && accountIds.Contains(debitAccount.Id))
				{
					transaction.WithdrawAccount = null;
				}
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
		TransactionAndEntry volatileModel = new()
		{
			When = DateTime.Today,
		};
		InvestingTransactionViewModel volatileViewModel = new(this, new[] { volatileModel });
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

			return 0;
		}

		public bool IsPropertySignificant(string propertyName) => propertyName is nameof(InvestingTransactionViewModel.When) or nameof(InvestingTransactionViewModel.TransactionId);
	}
}
