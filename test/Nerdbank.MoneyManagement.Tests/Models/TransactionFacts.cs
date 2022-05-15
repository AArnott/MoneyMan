// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class TransactionFacts : EntityTestBase
{
	public TransactionFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.EnableSqlLogging();
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		CategoryAccountViewModel cat = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");

		DateTime when = DateTime.Now;
		const int checkNo = 3;
		const string payee = "Them";
		const string memo = "Some memo";

		var t = new Transaction
		{
			When = when,
			CheckNumber = checkNo,
			Payee = payee,
			Memo = memo,
		};

		Assert.Equal(when, t.When);
		Assert.Equal(checkNo, t.CheckNumber);
		Assert.Equal(payee, t.Payee);
		Assert.Equal(memo, t.Memo);

		Transaction? t2 = this.SaveAndReload(t);

		Assert.NotEqual(0, t.Id);
		Assert.Equal(t.Id, t2.Id);
		Assert.Equal(when, t2.When);
		Assert.Equal(checkNo, t2.CheckNumber);
		Assert.Equal(payee, t2.Payee);
		Assert.Equal(memo, t2.Memo);
	}
}
