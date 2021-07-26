// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using SQLite;
	using Validation;

	/// <summary>
	/// Describes a deposit, withdrawal, or transfer regarding one or two accounts.
	/// </summary>
	public class Transaction
	{
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
		public decimal Amount { get; set; }

		/// <summary>
		/// Gets or sets a memo to go with this transaction.
		/// </summary>
		public string? Memo { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Payee.Id"/> of the <see cref="Payee"/> receiving or funding this transaction.
		/// </summary>
		public int? PayeeId { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Category.Id"/> of the <see cref="Category"/> assigned to this transaction.
		/// </summary>
		public int? CategoryId { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Account.Id"/> of the account to be credited the <see cref="Amount"/> of this <see cref="Transaction"/>.
		/// </summary>
		public int? CreditAccountId { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Account.Id"/> of the account to be debited the <see cref="Amount"/> of this <see cref="Transaction"/>.
		/// </summary>
		public int? DebitAccountId { get; set; }

		private void Validate()
		{
			Assumes.True(this.Amount >= 0, "Amount must be non-negative.");
		}
	}
}
