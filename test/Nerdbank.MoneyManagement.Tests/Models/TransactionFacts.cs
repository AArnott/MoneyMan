// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class TransactionFacts : EntityTestBase
{
	public TransactionFacts(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void AmountRejectsNegativeValue()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Transaction { CreditAmount = -1 });
		Assert.Throws<ArgumentOutOfRangeException>(() => new Transaction { DebitAmount = -1 });
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		CategoryViewModel cat = this.DocumentViewModel.CategoriesPanel.NewCategory("cat");

		DateTime when = DateTime.Now;
		const int checkNo = 3;
		const decimal creditAmount = 5.2398345m;
		const decimal debitAmount = 5.2498345m;
		const string payee = "Them";
		int categoryId = cat.Id;
		const string memo = "Some memo";
		const ClearedState creditCleared = ClearedState.Reconciled;
		const ClearedState debitCleared = ClearedState.Cleared;

		var t = new Transaction
		{
			When = when,
			CheckNumber = checkNo,
			CreditAmount = creditAmount,
			DebitAmount = debitAmount,
			Payee = payee,
			CategoryId = categoryId,
			Memo = memo,
			CreditCleared = creditCleared,
			DebitCleared = debitCleared,
		};

		Assert.Equal(when, t.When);
		Assert.Equal(checkNo, t.CheckNumber);
		Assert.Equal(creditAmount, t.CreditAmount);
		Assert.Equal(debitAmount, t.DebitAmount);
		Assert.Equal(payee, t.Payee);
		Assert.Equal(categoryId, t.CategoryId);
		Assert.Equal(memo, t.Memo);
		Assert.Equal(creditCleared, t.CreditCleared);
		Assert.Equal(debitCleared, t.DebitCleared);

		Transaction? t2 = this.SaveAndReload(t);

		Assert.NotEqual(0, t.Id);
		Assert.Equal(t.Id, t2.Id);
		Assert.Equal(when, t2.When);
		Assert.Equal(checkNo, t2.CheckNumber);
		Assert.Equal(creditAmount, t2.CreditAmount);
		Assert.Equal(debitAmount, t2.DebitAmount);
		Assert.Equal(payee, t2.Payee);
		Assert.Equal(categoryId, t2.CategoryId);
		Assert.Equal(memo, t2.Memo);
		Assert.Equal(creditCleared, t2.CreditCleared);
		Assert.Equal(debitCleared, t2.DebitCleared);
	}
}
