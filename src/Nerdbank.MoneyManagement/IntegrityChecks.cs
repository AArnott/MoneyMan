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
		/// A <see cref="Transaction"/> is split but its collection of <see cref="SplitTransaction"/> items
		/// do not sum to match the <see cref="Transaction.Amount"/> of the <see cref="Transaction"/>.
		/// </summary>
		public class SplitTransactionTotalMismatch : Issue
		{
			public SplitTransactionTotalMismatch(Transaction transaction, decimal splitTotal)
				: base($"The sum of all split line items for transaction {transaction.Id} is {splitTotal} instead of the expected {transaction.Amount}.")
			{
				this.Transaction = transaction;
				this.SplitTotal = splitTotal;
			}

			public Transaction Transaction { get; }

			public decimal SplitTotal { get; }
		}
	}
}
