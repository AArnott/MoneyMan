// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class IntegrityChecksFacts : MoneyTestBase
{
	public IntegrityChecksFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.EnableSqlLogging();
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
		Account checking = new Account { Name = "Checking", CurrencyAssetId = this.Money.PreferredAssetId };
		this.Money.Insert(checking);
		this.Money.Action.Deposit(checking, 10);

		// Add a split transaction.
		Account cat1 = new Account { Name = "Category 1", Type = Account.AccountType.Category };
		Account cat2 = new Account { Name = "Category 2", Type = Account.AccountType.Category };
		this.Money.InsertAll(cat1, cat2);
		var transaction = new Transaction
		{
			When = DateTime.Today,
		};
		this.Money.Insert(transaction);
		var splits = new TransactionEntry[]
		{
			new() { AccountId = cat1.Id, TransactionId = transaction.Id, Amount = -8, AssetId = checking.CurrencyAssetId.Value },
			new() { AccountId = cat2.Id, TransactionId = transaction.Id, Amount = -2, AssetId = checking.CurrencyAssetId.Value },
		};
		this.Money.InsertAll(splits);

		Assert.Empty(IntegrityChecks.CheckIntegrity(this.Money));
	}
}
