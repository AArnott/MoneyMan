// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using PCLCommandBase;
	using Validation;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class TransactionViewModel : EntityViewModel<Transaction>
	{
		public static readonly ClearedStateViewModel NotCleared = new ClearedStateViewModel(ClearedState.None, "None", string.Empty);
		public static readonly ClearedStateViewModel Matched = new ClearedStateViewModel(ClearedState.Cleared, "Cleared", "C");
		public static readonly ClearedStateViewModel Reconciled = new ClearedStateViewModel(ClearedState.Reconciled, "Reconciled", "R");

		public static readonly ReadOnlyCollection<ClearedStateViewModel> SharedClearedStates = new(new[]
		{
			NotCleared,
			Matched,
			Reconciled,
		});

		private ObservableCollection<SplitTransactionViewModel>? splits;
		private DateTime when;
		private int? checkNumber;
		private decimal amount;
		private string? memo;
		private ClearedStateViewModel cleared = SharedClearedStates[0];
		private string? payee;
		private ITransactionTarget? categoryOrTransfer;
		private decimal balance;

		[Obsolete("Do not use this constructor.")]
		public TransactionViewModel()
		{
			// This constructor exists only to get WPF to allow the user to add transaction rows.
			throw new NotSupportedException();
		}

		public TransactionViewModel(AccountViewModel thisAccount, Transaction? transaction)
			: base(thisAccount.MoneyFile)
		{
			this.ThisAccount = thisAccount;
			this.AutoSave = true;

			if (transaction is object)
			{
				this.CopyFrom(transaction);
			}

			this.SplitCommand = new SplitCommandImpl(this);
		}

		public ICommand SplitCommand { get; }

		public ReadOnlyCollection<ClearedStateViewModel> ClearedStates => SharedClearedStates;

		public DateTime When
		{
			get => this.when;
			set => this.SetProperty(ref this.when, value);
		}

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

		public string? Memo
		{
			get => this.memo;
			set => this.SetProperty(ref this.memo, value);
		}

		public ClearedStateViewModel Cleared
		{
			get => this.cleared;
			set => this.SetProperty(ref this.cleared, value);
		}

		public string? Payee
		{
			get => this.payee;
			set => this.SetProperty(ref this.payee, value);
		}

		public ITransactionTarget? CategoryOrTransfer
		{
			get => this.categoryOrTransfer;
			set
			{
				Verify.Operation(this.Splits.Count == 0 || value is null, "Cannot set category or transfer on a transaction containing splits.");
				this.SetProperty(ref this.categoryOrTransfer, value);
			}
		}

		////[SplitSumMatchesTransactionAmount]
		public IReadOnlyCollection<SplitTransactionViewModel> Splits
		{
			get
			{
				if (this.splits is null)
				{
					this.splits = new();
					if (this.MoneyFile is object && this.Id.HasValue)
					{
						SQLite.TableQuery<Transaction> splits = this.MoneyFile.Transactions
							.Where(tx => tx.ParentTransactionId == this.Id);
						foreach (Transaction split in splits)
						{
							SplitTransactionViewModel splitViewModel = new(this, split);
							this.splits.Add(splitViewModel);
						}
					}
				}

				return this.splits;
			}
		}

		public bool ContainsSplits => this.Splits.Count > 0;

		/// <summary>
		/// Gets a value indicating whether this "transaction" is really just synthesized to represent the split line item(s)
		/// of a transaction in another account that transfer to/from this account.
		/// </summary>
		public bool IsSplitMemberOfParentTransaction => this.Model?.ParentTransactionId.HasValue is true;

		public decimal Balance
		{
			get => this.balance;
			set => this.SetProperty(ref this.balance, value);
		}

		/// <summary>
		/// Gets the account this transaction was created to be displayed within.
		/// </summary>
		public AccountViewModel ThisAccount { get; }

		private string DebuggerDisplay => $"Transaction: {this.When} {this.Payee} {this.Amount}";

		public SplitTransactionViewModel NewSplit()
		{
			Verify.Operation(!this.IsSplitMemberOfParentTransaction, "Cannot split a transaction that is already a member of a split transaction.");

			if (this.Id is null)
			{
				// Persist this transaction so the splits can refer to it.
				this.Save();
			}

			SplitTransactionViewModel split = new(this, null)
			{
				MoneyFile = this.MoneyFile,
			};
			split.CategoryOrTransfer = this.CategoryOrTransfer;
			this.CategoryOrTransfer = null;

			split.Model = new();
			_ = this.Splits; // ensure initialized
			bool wasSplit = this.ContainsSplits;
			this.splits!.Add(split);
			if (!wasSplit)
			{
				this.OnPropertyChanged(nameof(this.ContainsSplits));
			}

			return split;
		}

		public void DeleteSplit(SplitTransactionViewModel split)
		{
			if (this.splits is null)
			{
				throw new InvalidOperationException("Splits haven't been initialized.");
			}

			this.splits.Remove(split);
			if (split.Model is object)
			{
				this.ThisAccount.MoneyFile?.Delete(split.Model);
			}

			if (!this.ContainsSplits)
			{
				this.OnPropertyChanged(nameof(this.ContainsSplits));
			}
		}

		public TransactionViewModel? GetSplitParent()
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

			AccountViewModel parentAccount = this.ThisAccount.DocumentViewModel.GetAccount(accountId.Value);
			return parentAccount.Transactions.First(tx => tx.Id == parentTransaction.Id);
		}

		public void JumpToSplitParent()
		{
			TransactionViewModel? splitParent = this.GetSplitParent();
			Verify.Operation(splitParent is object, "Cannot jump to split parent from a transaction that is not a member of a split transaction.");
			this.ThisAccount.DocumentViewModel.BankingPanel.SelectedAccount = splitParent.ThisAccount;
			this.ThisAccount.DocumentViewModel.SelectedTransaction = splitParent;
		}

		protected override void ApplyToCore(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			transaction.Payee = this.Payee;
			transaction.When = this.When;
			transaction.Amount = this.Splits.Count > 0 ? 0 : Math.Abs(this.Amount);
			transaction.Memo = this.Memo;
			transaction.CheckNumber = this.CheckNumber;
			transaction.Cleared = this.Cleared.Value;
			if (this.Splits.Count > 0)
			{
				transaction.CategoryId = Category.Split;
				foreach (SplitTransactionViewModel split in this.Splits)
				{
					split.Save();
				}
			}
			else
			{
				transaction.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;
			}

			if (this.Amount < 0)
			{
				transaction.DebitAccountId = this.ThisAccount.Id;
				transaction.CreditAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
			}
			else
			{
				transaction.CreditAccountId = this.ThisAccount.Id;
				transaction.DebitAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
			}
		}

		protected override void CopyFromCore(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			this.payee = transaction.Payee; // add test for property changed.
			this.When = transaction.When;
			this.Amount = transaction.CreditAccountId == this.ThisAccount.Id ? transaction.Amount : -transaction.Amount;
			this.Memo = transaction.Memo;
			this.CheckNumber = transaction.CheckNumber;
			this.Cleared = SharedClearedStates.Single(cs => cs.Value == transaction.Cleared);

			if (transaction.CategoryId is int categoryId)
			{
				if (categoryId == Category.Split)
				{
					// These are lazily initialized.
				}
				else
				{
					this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetCategory(categoryId) ?? throw new InvalidOperationException();
					if (this.splits is object)
					{
						this.splits.Clear();
					}

					this.OnPropertyChanged(nameof(this.Splits));
				}
			}
			else if (transaction.CreditAccountId is int creditId && this.ThisAccount.Id != creditId)
			{
				this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(creditId) ?? throw new InvalidOperationException();
			}
			else if (transaction.DebitAccountId is int debitId && this.ThisAccount.Id != debitId)
			{
				this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(debitId) ?? throw new InvalidOperationException();
			}
			else
			{
				this.CategoryOrTransfer = null;
			}
		}

		protected override bool IsPersistedProperty(string propertyName) => propertyName is not nameof(this.Balance);

		[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
		public class ClearedStateViewModel
		{
			public ClearedStateViewModel(ClearedState value, string caption, string shortCaption)
			{
				this.Value = value;
				this.Caption = caption;
				this.ShortCaption = shortCaption;
			}

			public ClearedState Value { get; }

			public string Caption { get; }

			public string ShortCaption { get; }

			private string DebuggerDisplay => this.Caption;
		}

		private class SplitCommandImpl : CommandBase
		{
			private TransactionViewModel transactionViewModel;

			public SplitCommandImpl(TransactionViewModel transactionViewModel)
			{
				this.transactionViewModel = transactionViewModel;
				transactionViewModel.PropertyChanged += this.TransactionViewModel_PropertyChanged;
			}

			public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && !this.transactionViewModel.ContainsSplits;

			protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
			{
				this.transactionViewModel.NewSplit();
				return Task.CompletedTask;
			}

			private void TransactionViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(this.transactionViewModel.ContainsSplits))
				{
					this.OnCanExecuteChanged();
				}
			}
		}
	}
}
