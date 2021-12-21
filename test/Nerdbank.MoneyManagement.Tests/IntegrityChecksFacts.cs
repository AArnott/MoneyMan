// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

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
		this.Money.Insert(new Transaction { CreditAmount = 10 });

		// Add a split transaction.
		var transaction = new Transaction
		{
			CreditAmount = 10,
		};
		this.Money.Insert(transaction);
		var splits = new Transaction[]
		{
			new() { ParentTransactionId = transaction.Id, CreditAmount = 2 },
			new() { ParentTransactionId = transaction.Id, CreditAmount = 8 },
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
			CreditAmount = 6, // The right sum, but it's supposed to be 0.
		};
		this.Money.Insert(transaction);
		var splits = new Transaction[]
		{
			new() { ParentTransactionId = transaction.Id, CreditAmount = 2 },
			new() { ParentTransactionId = transaction.Id, CreditAmount = 4 },
		};
		this.Money.InsertAll(splits);

		IReadOnlyList<IntegrityChecks.Issue> issues = IntegrityChecks.CheckIntegrity(this.Money);
		Assert.Single(issues);
		var issue = Assert.IsType<IntegrityChecks.SplitTransactionTotalMismatch>(issues[0]);
		Assert.Equal(transaction.Id, issue.Transaction.Id);
	}
}
