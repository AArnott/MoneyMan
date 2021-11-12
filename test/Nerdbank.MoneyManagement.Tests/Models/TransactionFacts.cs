// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

public class TransactionFacts : EntityTestBase
{
	public TransactionFacts(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void AmountRejectsNegativeValue()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Transaction { Amount = -1 });
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		DateTime when = DateTime.Now;
		const int checkNo = 3;
		const decimal amount = 5.2398345m;
		const string payee = "Them";
		const int categoryId = 8;
		const string memo = "Some memo";
		const ClearedState cleared = ClearedState.Reconciled;

		var t = new Transaction
		{
			When = when,
			CheckNumber = checkNo,
			Amount = amount,
			Payee = payee,
			CategoryId = categoryId,
			Memo = memo,
			Cleared = cleared,
		};

		Assert.Equal(when, t.When);
		Assert.Equal(checkNo, t.CheckNumber);
		Assert.Equal(amount, t.Amount);
		Assert.Equal(payee, t.Payee);
		Assert.Equal(categoryId, t.CategoryId);
		Assert.Equal(memo, t.Memo);
		Assert.Equal(cleared, t.Cleared);

		Transaction? t2 = this.SaveAndReload(t);

		Assert.NotEqual(0, t.Id);
		Assert.Equal(t.Id, t2.Id);
		Assert.Equal(when, t2.When);
		Assert.Equal(checkNo, t2.CheckNumber);
		Assert.Equal(amount, t2.Amount);
		Assert.Equal(payee, t2.Payee);
		Assert.Equal(categoryId, t2.CategoryId);
		Assert.Equal(memo, t2.Memo);
		Assert.Equal(cleared, t2.Cleared);
	}
}
