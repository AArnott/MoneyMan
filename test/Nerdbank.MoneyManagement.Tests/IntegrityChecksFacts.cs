// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

public class IntegrityChecksFacts : MoneyTestBase
{
	public IntegrityChecksFacts(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void EmptyFileHasNoProblems()
	{
		Assert.Empty(IntegrityChecks.CheckIntegrity(this.Money));
	}

	[Fact]
	public void ValidFileWithTransactions()
	{
		// Add ordinary transaction.
		this.Money.Insert(new Transaction { Amount = 10 });

		// Add a split transaction.
		var transaction = new Transaction
		{
			Amount = 10,
		};
		this.Money.Insert(transaction);
		var splits = new SplitTransaction[]
		{
			new() { TransactionId = transaction.Id, Amount = 2 },
			new() { TransactionId = transaction.Id, Amount = 8 },
		};
		this.Money.InsertAll(splits);

		Assert.Empty(IntegrityChecks.CheckIntegrity(this.Money));
	}

	[Fact]
	public void BadSplitTransaction()
	{
		var transaction = new Transaction
		{
			CategoryId = Category.Split,
			Amount = 10,
		};
		this.Money.Insert(transaction);
		var splits = new SplitTransaction[]
		{
			new() { TransactionId = transaction.Id, Amount = 2 },
			new() { TransactionId = transaction.Id, Amount = 4 },
		};
		this.Money.InsertAll(splits);

		IReadOnlyList<IntegrityChecks.Issue> issues = IntegrityChecks.CheckIntegrity(this.Money);
		Assert.Single(issues);
		var issue = Assert.IsType<IntegrityChecks.SplitTransactionTotalMismatch>(issues[0]);
		Assert.Equal(splits.Sum(s => s.Amount), issue.SplitTotal);
		Assert.Equal(transaction.Id, issue.Transaction.Id);
	}
}
