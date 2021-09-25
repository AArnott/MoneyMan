// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using PCLCommandBase;
	using Validation;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class AccountViewModel : EntityViewModel<Account>, ITransactionTarget
	{
		private ObservableCollection<TransactionViewModel>? transactions;
		private string name = string.Empty;
		private bool isClosed;
		private decimal balance;

		public AccountViewModel()
			: this(null, null, null)
		{
		}

		public AccountViewModel(Account? model, MoneyFile? moneyFile, DocumentViewModel? documentViewModel)
			: base(moneyFile)
		{
			this.RegisterDependentProperty(nameof(this.Name), nameof(this.TransferTargetName));
			this.AutoSave = true;

			this.DocumentViewModel = documentViewModel;
			if (model is object)
			{
				this.CopyFrom(model);
			}
		}

		public string Name
		{
			get => this.name;
			set => this.SetProperty(ref this.name, value);
		}

		public string? TransferTargetName => $"[{this.Name}]";

		public bool IsClosed
		{
			get => this.isClosed;
			set => this.SetProperty(ref this.isClosed, value);
		}

		public decimal Balance
		{
			get => this.balance;
			set => this.SetProperty(ref this.balance, value);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] // It's lazily initialized, and we don't want the debugger to trip over it.
		public ObservableCollection<TransactionViewModel> Transactions
		{
			get
			{
				if (this.transactions is null)
				{
					this.transactions = new();
					if (this.MoneyFile is object)
					{
						SQLite.TableQuery<Transaction> transactions = this.MoneyFile.Transactions.Where(tx => tx.CreditAccountId == this.Id || tx.DebitAccountId == this.Id);
						foreach (Transaction transaction in transactions)
						{
							TransactionViewModel transactionViewModel = new(this, transaction, this.MoneyFile);
							this.transactions.Add(transactionViewModel);
						}
					}
				}

				return this.transactions;
			}
		}

		internal DocumentViewModel? DocumentViewModel { get; }

		private string? DebuggerDisplay => this.Name;

		internal void NotifyTransactionDeleted(Transaction transaction)
		{
			if (this.transactions is null)
			{
				// Nothing to refresh.
				return;
			}

			if (this.FindTransaction(transaction.Id) is { } transactionViewModel)
			{
				this.Transactions.Remove(transactionViewModel);
			}
		}

		internal void NotifyTransactionChanged(Transaction transaction)
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
					this.Transactions.Remove(transactionViewModel);
				}
				else
				{
					transactionViewModel.CopyFrom(transaction);
				}
			}
			else if (!removedFromAccount)
			{
				// This may be a new transaction we need to add.
				this.Transactions.Add(new TransactionViewModel(this, transaction, this.MoneyFile));
			}
		}

		protected override void ApplyToCore(Account account)
		{
			Requires.NotNull(account, nameof(account));

			account.Name = this.name;
			account.IsClosed = this.IsClosed;
		}

		protected override void CopyFromCore(Account account)
		{
			Requires.NotNull(account, nameof(account));

			this.Name = account.Name;
			this.IsClosed = account.IsClosed;

			if (this.MoneyFile is object && account is object)
			{
				this.balance = this.MoneyFile.GetBalance(account);
			}

			// Force reinitialization.
			this.transactions = null;
		}

		protected override bool IsPersistedProperty(string propertyName) => propertyName is not nameof(this.Balance);

		private TransactionViewModel? FindTransaction(int id)
		{
			foreach (TransactionViewModel transactionViewModel in this.Transactions)
			{
				if (transactionViewModel.Model?.Id == id)
				{
					return transactionViewModel;
				}
			}

			return null;
		}
	}
}
