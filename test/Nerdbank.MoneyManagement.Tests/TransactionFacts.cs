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
		decimal amount = 5.2398345m;
		int payeeId = 5;
		int categoryId = 8;

		var t = new Transaction
		{
			When = when,
			Amount = amount,
			PayeeId = payeeId,
			CategoryId = categoryId,
		};

		Assert.Equal(when, t.When);
		Assert.Equal(amount, t.Amount);
		Assert.Equal(payeeId, t.PayeeId);
		Assert.Equal(categoryId, t.CategoryId);

		Transaction? t2 = this.SaveAndReload(t);

		Assert.NotEqual(0, t.Id);
		Assert.Equal(t.Id, t2.Id);
		Assert.Equal(when, t2.When);
		Assert.Equal(amount, t2.Amount);
		Assert.Equal(payeeId, t2.PayeeId);
		Assert.Equal(categoryId, t2.CategoryId);
	}
}
