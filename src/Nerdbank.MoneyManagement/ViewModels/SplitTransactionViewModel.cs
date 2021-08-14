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
		private CategoryViewModel? category;

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

		public CategoryViewModel? Category
		{
			get => this.category;
			set => this.SetProperty(ref this.category, value);
		}

		public override void ApplyTo(SplitTransaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			transaction.Amount = this.Amount;
			transaction.Memo = this.Memo;
			transaction.CategoryId = this.Category?.Id;
		}

		public void CopyFrom(SplitTransaction transaction, IReadOnlyDictionary<int, CategoryViewModel> categories)
		{
			Requires.NotNull(transaction, nameof(transaction));
			Requires.NotNull(categories, nameof(categories));

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

		public override void CopyFrom(SplitTransaction category) => throw new NotSupportedException("Use the other overload instead.");
	}
}
