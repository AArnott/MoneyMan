// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	public void BasicPropertiesSerialization()
	{
		DateTime when = DateTime.Now;
		const int checkNo = 3;
		const decimal amount = 5.2398345m;
		const int payeeId = 5;
		const int categoryId = 8;
		const string memo = "Some memo";

		var t = new Transaction
		{
			When = when,
			CheckNumber = checkNo,
			Amount = amount,
			PayeeId = payeeId,
			CategoryId = categoryId,
			Memo = memo,
		};

		Assert.Equal(when, t.When);
		Assert.Equal(checkNo, t.CheckNumber);
		Assert.Equal(amount, t.Amount);
		Assert.Equal(payeeId, t.PayeeId);
		Assert.Equal(categoryId, t.CategoryId);
		Assert.Equal(memo, t.Memo);

		Transaction? t2 = this.SaveAndReload(t);

		Assert.NotEqual(0, t.Id);
		Assert.Equal(t.Id, t2.Id);
		Assert.Equal(when, t2.When);
		Assert.Equal(checkNo, t2.CheckNumber);
		Assert.Equal(amount, t2.Amount);
		Assert.Equal(payeeId, t2.PayeeId);
		Assert.Equal(categoryId, t2.CategoryId);
		Assert.Equal(memo, t2.Memo);
	}
}
