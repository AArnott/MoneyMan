// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using System;
	using System.Diagnostics;
	using SQLite;
	using Validation;

	/// <summary>
	/// Describes a deposit, withdrawal, or transfer regarding one or two accounts.
	/// </summary>
	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class Transaction
	{
		private decimal amount;

		/// <summary>
		/// Gets or sets the primary key of this database entity.
		/// </summary>
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the date the transaction is to be sorted by.
		/// </summary>
		/// <remarks>
		/// The time component and timezone components are to be ignored.
		/// We don't want a change in the user's timezone to change the date that is displayed for a transaction.
		/// </remarks>
		[NotNull]
		public DateTime When { get; set; }

		/// <summary>
		/// Gets or sets the check number associated with this transaction, if any.
		/// </summary>
		public int? CheckNumber { get; set; }

		/// <summary>
		/// Gets or sets the amount of the transaction. Always non-negative.
		/// </summary>
		[NotNull]
		public decimal Amount
		{
			get => this.amount;
			set
			{
				Requires.Range(value >= 0, nameof(value));
				this.amount = value;
			}
		}

		/// <summary>
		/// Gets or sets a memo to go with this transaction.
		/// </summary>
		public string? Memo { get; set; }

		/// <summary>
		/// Gets or sets the party receiving or funding this transaction.
		/// </summary>
		public string? Payee { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Category.Id"/> of the <see cref="Category"/> assigned to this transaction.
		/// </summary>
		/// <remarks>
		/// Use <see cref="Category.Split"/> for the value where the transaction is split across multiple categories.
		/// </remarks>
		[Indexed]
		public int? CategoryId { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Account.Id"/> of the account to be credited the <see cref="Amount"/> of this <see cref="Transaction"/>.
		/// </summary>
		public int? CreditAccountId { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Account.Id"/> of the account to be debited the <see cref="Amount"/> of this <see cref="Transaction"/>.
		/// </summary>
		public int? DebitAccountId { get; set; }

		/// <summary>
		/// Gets or sets the cleared or reconciled state of the transaction.
		/// </summary>
		[NotNull]
		public ClearedState Cleared { get; set; }

		private string DebuggerDisplay => $"{this.When} {this.Payee} {this.Amount}";
	}
}
