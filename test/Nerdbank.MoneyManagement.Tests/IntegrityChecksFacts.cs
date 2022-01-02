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
		Account checking = new Account { Name = "Checking" };
		this.Money.Insert(checking);
		this.Money.Deposit(checking, 10);

		// Add a split transaction.
		Account cat1 = new Account { Name = "Category 1", Type = Account.AccountType.Category };
		Account cat2 = new Account { Name = "Category 2", Type = Account.AccountType.Category };
		var transaction = new Transaction
		{
			When = DateTime.Today,
		};
		this.Money.Insert(transaction);
		var splits = new TransactionEntry[]
		{
			new() { AccountId = checking.Id, TransactionId = transaction.Id, Amount = 10 },
			new() { AccountId = cat1.Id, TransactionId = transaction.Id, Amount = -8 },
			new() { AccountId = cat2.Id, TransactionId = transaction.Id, Amount = -2 },
		};
		this.Money.InsertAll(splits);

		Assert.Empty(IntegrityChecks.CheckIntegrity(this.Money));
	}
}
