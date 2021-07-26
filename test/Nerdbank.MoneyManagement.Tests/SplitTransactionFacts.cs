// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

public class SplitTransactionFacts : EntityTestBase
{
	public SplitTransactionFacts(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		DateTime when = DateTime.Now;
		const int transactionId = 5;
		const decimal amount = 5.2398345m;
		const int categoryId = 8;
		const string memo = "Some memo";

		var t = new SplitTransaction
		{
			TransactionId = transactionId,
			Amount = amount,
			CategoryId = categoryId,
			Memo = memo,
		};

		Assert.Equal(transactionId, t.TransactionId);
		Assert.Equal(amount, t.Amount);
		Assert.Equal(categoryId, t.CategoryId);
		Assert.Equal(memo, t.Memo);

		SplitTransaction? t2 = this.SaveAndReload(t);

		Assert.NotEqual(0, t.Id);
		Assert.Equal(t.Id, t2.Id);
		Assert.Equal(transactionId, t2.TransactionId);
		Assert.Equal(amount, t2.Amount);
		Assert.Equal(categoryId, t2.CategoryId);
		Assert.Equal(memo, t2.Memo);
	}
}
