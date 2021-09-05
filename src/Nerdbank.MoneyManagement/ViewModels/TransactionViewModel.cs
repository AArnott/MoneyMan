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
		private CategoryViewModel? category;
		private bool isSelected;

		public TransactionViewModel()
			: this(null, null)
		{
		}

		public TransactionViewModel(Transaction? model, MoneyFile? moneyFile)
			: base(model, moneyFile)
		{
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

		protected override void ApplyToCore(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));
			transaction.Payee = this.Payee;
			transaction.When = this.When;
			transaction.Amount = this.Amount;
			transaction.Memo = this.Memo;
			transaction.CheckNumber = this.CheckNumber;
			transaction.Cleared = this.Cleared.Value;
		}

		protected override void CopyFromCore(Transaction transaction)
		{
			Requires.NotNull(transaction, nameof(transaction));

			this.payee = transaction.Payee;
			this.When = transaction.When;
			this.Amount = transaction.Amount;
			this.Memo = transaction.Memo;
			this.CheckNumber = transaction.CheckNumber;
			this.Cleared = SharedClearedStates.Single(cs => cs.Value == transaction.Cleared);
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
