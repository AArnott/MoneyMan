// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Linq;
	using Validation;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class TransactionViewModel : EntityViewModel<Transaction>
	{
		public static readonly ReadOnlyCollection<ClearedStateViewModel> SharedClearedStates = new(new[]
		{
			new ClearedStateViewModel(ClearedState.None, "None", string.Empty),
			new ClearedStateViewModel(ClearedState.Cleared, "Cleared", "C"),
			new ClearedStateViewModel(ClearedState.Reconciled, "Reconciled", "R"),
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
		}

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

		[SplitSumMatchesTransactionAmount]
		public IReadOnlyCollection<SplitTransactionViewModel> Splits
		{
			get
			{
				if (this.splits is null)
				{
					this.splits = new();
					if (this.MoneyFile is object)
					{
						SQLite.TableQuery<SplitTransaction> splits = this.MoneyFile.SplitTransactions
							.Where(tx => tx.TransactionId == this.Id);
						foreach (SplitTransaction split in splits)
						{
							SplitTransactionViewModel splitViewModel = new(this, split);
							this.splits.Add(splitViewModel);
						}
					}
				}

				return this.splits;
			}
		}

		public bool IsSplit => this.Splits.Count > 0;

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
			bool wasSplit = this.IsSplit;
			this.splits!.Add(split);
			if (!wasSplit)
			{
				this.OnPropertyChanged(nameof(this.IsSplit));
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

			if (!this.IsSplit)
			{
				this.OnPropertyChanged(nameof(this.IsSplit));
			}
		}

		protected override void ApplyToCore(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			transaction.Payee = this.Payee;
			transaction.When = this.When;
			transaction.Amount = Math.Abs(this.Amount);
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
	}
}
