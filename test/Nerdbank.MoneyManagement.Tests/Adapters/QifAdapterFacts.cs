// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.Adapters;

public class QifAdapterFacts : AdapterTestBase
{
	private const string Simple1DataFileName = "Simple1.qif";
	private QifAdapter adapter;

	public QifAdapterFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.adapter = new(this.DocumentViewModel);
	}

	[Fact]
	public async Task ImportAsync_ValidatesArgs()
	{
		await Assert.ThrowsAsync<ArgumentNullException>("filePath", () => this.adapter.ImportAsync(null!, this.TimeoutToken));
		await Assert.ThrowsAsync<ArgumentException>("filePath", () => this.adapter.ImportAsync(string.Empty, this.TimeoutToken));
	}

	[Fact]
	public async Task ImportAsync()
	{
		int chooseAccountFuncInvocationCount = 0;
		this.UserNotification.ChooseAccountFunc = (string prompt, AccountViewModel? defaultAccount, CancellationToken cancellationToken) =>
		{
			chooseAccountFuncInvocationCount++;
			return Task.FromResult<AccountViewModel?>(this.Checking);
		};
		int result = await this.adapter.ImportAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);
		Assert.Equal(1, chooseAccountFuncInvocationCount);
		Assert.Equal(3, result);
		Assert.Equal(result, this.Checking.Transactions.Count(t => t.IsPersisted));

		BankingTransactionViewModel tx1 = this.Checking.Transactions[0];
		Assert.Equal(ExpectedDateTime(2022, 3, 24), tx1.When);
		Assert.Equal(-17.35m, tx1.Amount);
		Assert.Equal("Spotify", tx1.Payee);
		Assert.True(string.IsNullOrEmpty(tx1.Memo));
		Assert.Equal(ClearedState.Reconciled, tx1.Cleared);

		BankingTransactionViewModel tx2 = this.Checking.Transactions[1];
		Assert.Equal(ExpectedDateTime(2022, 3, 24), tx2.When);
		Assert.Equal(-671.00m, tx2.Amount);
		Assert.Equal("Xtreme Xperience Llc", tx2.Payee);
		Assert.Equal("gift for Fuzzy", tx2.Memo);
		Assert.Equal(ClearedState.Cleared, tx2.Cleared);

		BankingTransactionViewModel tx3 = this.Checking.Transactions[2];
		Assert.Equal(ExpectedDateTime(2022, 3, 25), tx3.When);
		Assert.Equal(-11.10m, tx3.Amount);
		Assert.Equal("Whole Foods", tx3.Payee);
		Assert.Equal("5146: WHOLEFDS RMD 10260 REDMOND WA 98052 US", tx3.Memo);
		Assert.Equal(ClearedState.None, tx3.Cleared);

		static DateTime ExpectedDateTime(int year, int month, int day) => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
	}

	protected override void RefetchViewModels()
	{
		base.RefetchViewModels();
		this.adapter = new(this.DocumentViewModel);
	}
}
