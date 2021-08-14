// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Diagnostics;
	using Validation;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class TransactionViewModel : EntityViewModel<Transaction>
	{
		private DateTime when;
		private int? checkNumber;
		private decimal amount;
		private string? memo;
		private ClearedState cleared;
		private AccountViewModel? transferAccount;
		private PayeeViewModel? payee;
		private CategoryViewModel? category;
		private bool isSelected;

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

		public ClearedState Cleared
		{
			get => this.cleared;
			set => this.SetProperty(ref this.cleared, value);
		}

		public AccountViewModel? Transfer
		{
			get => this.transferAccount;
			set => this.SetProperty(ref this.transferAccount, value);
		}

		public PayeeViewModel? Payee
		{
			get => this.payee;
			set => this.SetProperty(ref this.payee, value);
		}

		public CategoryViewModel? Category
		{
			get => this.category;
			set => this.SetProperty(ref this.category, value);
		}

		public bool IsSelected
		{
			get => this.isSelected;
			set => this.SetProperty(ref this.isSelected, value);
		}

		private string DebuggerDisplay => $"Transaction: {this.When} {this.Payee} {this.Amount}";

		public override void ApplyTo(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));
			transaction.When = this.When;
			transaction.Amount = this.Amount;
			transaction.Memo = this.Memo;
			transaction.CheckNumber = this.CheckNumber;
			transaction.Cleared = this.Cleared;
		}

		public override void CopyFrom(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			this.When = transaction.When;
			this.Amount = transaction.Amount;
			this.Memo = transaction.Memo;
			this.CheckNumber = transaction.CheckNumber;
			this.Cleared = transaction.Cleared;
		}
	}
}
