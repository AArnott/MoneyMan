// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Validation;

	public static class IntegrityChecks
	{
		/// <summary>
		/// Checks the data file for any integrity issues.
		/// </summary>
		/// <param name="file">The data file to scan.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>A list of the issues found.</returns>
		public static IReadOnlyList<Issue> CheckIntegrity(this MoneyFile file, CancellationToken cancellationToken = default)
		{
			Verify.NotDisposed(file);
			List<Issue> issues = new();
			issues.AddRange(file.FindBadSplitTransactions(cancellationToken));
			return issues;
		}

		public abstract class Issue
		{
			internal Issue(string message)
			{
				this.Message = message;
			}

			public string Message { get; internal set; }
		}

		/// <summary>
		/// A <see cref="Transaction"/> is split into child transactions but its <see cref="Transaction.Amount"/> is not 0.
		/// </summary>
		public class SplitTransactionTotalMismatch : Issue
		{
			public SplitTransactionTotalMismatch(Transaction transaction)
				: base($"The {nameof(transaction.Amount)} for transaction {transaction.Id} is expected to be 0 because it contains child transactions, but is {transaction.Amount}.")
			{
				this.Transaction = transaction;
			}

			public Transaction Transaction { get; }
		}
	}
}
