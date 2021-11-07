// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Diagnostics;
	using Validation;

	/// <summary>
	/// Represents a <see cref="Transaction"/> that is a member of a split transaction,
	/// as it is represented in its "home" account, such that it only appears nested under the parent transaction.
	/// </summary>
	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class SplitTransactionViewModel : EntityViewModel<Transaction>
	{
		private decimal amount;
		private string? memo;
		private ITransactionTarget? categoryOrTransfer;

		[Obsolete("Do not use this constructor.")]
		public SplitTransactionViewModel()
		{
			// This constructor exists only to get WPF to allow the user to add transaction rows.
			throw new NotSupportedException();
		}

		public SplitTransactionViewModel(TransactionViewModel parent, Transaction? splitTransaction)
		{
			this.ParentTransaction = parent;
			this.AutoSave = true;

			if (splitTransaction is object)
			{
				this.CopyFrom(splitTransaction);
			}
		}

		public TransactionViewModel ParentTransaction { get; }

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

		public ITransactionTarget? CategoryOrTransfer
		{
			get => this.categoryOrTransfer;
			set => this.SetProperty(ref this.categoryOrTransfer, value);
		}

		private string DebuggerDisplay => $"Split: {this.Memo} {this.CategoryOrTransfer} {this.Amount}";

		protected override void ApplyToCore(Transaction split)
		{
			Requires.NotNull(split, nameof(split));

			split.ParentTransactionId = this.ParentTransaction.Id ?? throw new InvalidOperationException("Cannot save a split before its parent transaction.");
			split.Amount = Math.Abs(this.Amount);
			split.Memo = this.Memo;
			split.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;

			if (this.Amount < 0)
			{
				split.DebitAccountId = this.ParentTransaction.ThisAccount.Id;
				split.CreditAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
			}
			else
			{
				split.CreditAccountId = this.ParentTransaction.ThisAccount.Id;
				split.DebitAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
			}
		}

		protected override void CopyFromCore(Transaction split)
		{
			Requires.NotNull(split, nameof(split));

			this.Amount = split.Amount;
			this.Memo = split.Memo;

			if (split.CategoryId is int categoryId)
			{
				this.CategoryOrTransfer = this.ParentTransaction.ThisAccount.DocumentViewModel?.GetCategory(categoryId) ?? throw new InvalidOperationException();
			}
			else if (split.CreditAccountId is int creditId && this.ParentTransaction.ThisAccount.Id != creditId)
			{
				this.CategoryOrTransfer = this.ParentTransaction.ThisAccount.DocumentViewModel?.GetAccount(creditId) ?? throw new InvalidOperationException();
			}
			else if (split.DebitAccountId is int debitId && this.ParentTransaction.ThisAccount.Id != debitId)
			{
				this.CategoryOrTransfer = this.ParentTransaction.ThisAccount.DocumentViewModel?.GetAccount(debitId) ?? throw new InvalidOperationException();
			}
			else
			{
				this.CategoryOrTransfer = null;
			}
		}
	}
}
