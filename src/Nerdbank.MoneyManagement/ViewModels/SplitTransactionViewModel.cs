// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using PCLCommandBase;
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

		protected override void ApplyToCore(SplitTransaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			transaction.Amount = this.Amount;
			transaction.Memo = this.Memo;
			transaction.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;

			if (this.Amount < 0)
			{
				transaction.DebitAccountId = this.ParentTransaction.ThisAccount.Id;
				transaction.CreditAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
			}
			else
			{
				transaction.CreditAccountId = this.ParentTransaction.ThisAccount.Id;
				transaction.DebitAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
			}
		}

		protected override void CopyFromCore(SplitTransaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			this.Amount = transaction.Amount;
			this.Memo = transaction.Memo;

			if (transaction.CategoryId is int categoryId)
			{
				Requires.Argument(categories.TryGetValue(categoryId, out CategoryViewModel? categoryViewModel), nameof(categories), "No category with required ID found.");
				this.Category = categoryViewModel;
			}
			else
			{
				this.Category = null;
			}
		}
	}
}
