// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using System;
	using System.Diagnostics;
	using SQLite;
	using Validation;

	/// <summary>
	/// Describes a bank account.
	/// </summary>
	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class Account : ModelBase
	{
		/// <summary>
		/// Gets or sets the name of this account.
		/// </summary>
		[NotNull]
		public string? Name { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the account has been closed.
		/// </summary>
		/// <remarks>
		/// Closed accounts are excluded from most queries by default.
		/// </remarks>
		[NotNull]
		public bool IsClosed { get; set; }

		private string? DebuggerDisplay => this.Name;

		/// <summary>
		/// Creates a new <see cref="Transaction"/> that describes a withdrawal from this account.
		/// </summary>
		/// <param name="amount">The amount to withdraw.</param>
		/// <returns>The created transaction that has not yet been added to the database.</returns>
		public Transaction Withdraw(decimal amount)
		{
			Verify.Operation(this.Id != 0, "This account has not been saved yet.");
			return new Transaction
			{
				When = DateTime.Now,
				DebitAccountId = this.Id,
				Amount = amount,
			};
		}

		/// <summary>
		/// Creates a new <see cref="Transaction"/> that describes a deposit to this account.
		/// </summary>
		/// <param name="amount">The amount to deposit.</param>
		/// <returns>The created transaction that has not yet been added to the database.</returns>
		public Transaction Deposit(decimal amount)
		{
			Verify.Operation(this.Id != 0, "This account has not been saved yet.");
			return new Transaction
			{
				When = DateTime.Now,
				CreditAccountId = this.Id,
				Amount = amount,
			};
		}

		/// <summary>
		/// Creates a new <see cref="Transaction"/> that describes a transfer from this account to another one.
		/// </summary>
		/// <param name="receivingAccount">The account to receive funds from this account.</param>
		/// <param name="amount">The amount to transfer.</param>
		/// <returns>The created transaction that has not yet been added to the database.</returns>
		public Transaction Transfer(Account receivingAccount, decimal amount)
		{
			Requires.NotNull(receivingAccount, nameof(receivingAccount));
			Requires.Range(amount >= 0, nameof(amount), "Must be a non-negative amount.");

			Transaction? transaction = this.Withdraw(amount);
			transaction.CreditAccountId = receivingAccount.Id;
			return transaction;
		}
	}
}
