// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using Validation;

	public class SplitTransactionViewModel : EntityViewModel<SplitTransaction>
	{
		private decimal amount;
		private string? memo;
		private ITransactionTarget? categoryOrTransfer;

		public SplitTransactionViewModel(TransactionViewModel parent, SplitTransaction? splitTransaction)
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

		protected override void ApplyToCore(SplitTransaction split)
		{
			Requires.NotNull(split, nameof(split));

			split.Amount = this.Amount;
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

		protected override void CopyFromCore(SplitTransaction split)
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
