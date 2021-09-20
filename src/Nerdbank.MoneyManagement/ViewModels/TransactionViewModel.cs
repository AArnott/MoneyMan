// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
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

		private DateTime when;
		private int? checkNumber;
		private decimal amount;
		private string? memo;
		private ClearedStateViewModel cleared = SharedClearedStates[0];
		private AccountViewModel? transferAccount;
		private string? payee;
		private ITransactionTarget? categoryOrTransfer;

		[Obsolete("Do not use this constructor.")]
		public TransactionViewModel()
		{
			// This constructor exists only to get WPF to allow the user to add transaction rows.
			throw new NotSupportedException();
		}

		public TransactionViewModel(AccountViewModel thisAccount, Transaction? transaction, MoneyFile? moneyFile)
			: base(moneyFile)
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

		public AccountViewModel? Transfer
		{
			get => this.transferAccount;
			set => this.SetProperty(ref this.transferAccount, value);
		}

		public string? Payee
		{
			get => this.payee;
			set => this.SetProperty(ref this.payee, value);
		}

		public ITransactionTarget? CategoryOrTransfer
		{
			get => this.categoryOrTransfer;
			set => this.SetProperty(ref this.categoryOrTransfer, value);
		}

		/// <summary>
		/// Gets the account this transaction was created to be displayed within.
		/// </summary>
		public AccountViewModel ThisAccount { get; }

		private string DebuggerDisplay => $"Transaction: {this.When} {this.Payee} {this.Amount}";

		protected override void ApplyToCore(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			transaction.Payee = this.Payee;
			transaction.When = this.When;
			transaction.Amount = Math.Abs(this.Amount);
			transaction.Memo = this.Memo;
			transaction.CheckNumber = this.CheckNumber;
			transaction.Cleared = this.Cleared.Value;
			transaction.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;

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
				this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetCategory(categoryId) ?? throw new InvalidOperationException();
			}
			else
			{
				if (transaction.CreditAccountId is int creditId && this.ThisAccount.Id != creditId)
				{
					this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(creditId) ?? throw new InvalidOperationException();
				}
				else if (transaction.DebitAccountId is int debitId && this.ThisAccount.Id != debitId)
				{
					this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(debitId) ?? throw new InvalidOperationException();
				}
			}
		}

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
