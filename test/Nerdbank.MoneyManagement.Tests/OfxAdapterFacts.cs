// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

public class OfxAdapterFacts : MoneyTestBase
{
	private const string Simple1DataFileName = "Simple1.ofx";
	private OfxAdapter adapter;
	private BankingAccountViewModel checking;

	public OfxAdapterFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.adapter = new(this.DocumentViewModel);
		this.checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
	}

	[Fact]
	public void CtorValidatesArgs()
	{
		Assert.Throws<ArgumentNullException>("documentViewModel", () => new OfxAdapter(null!));
	}

	[Fact]
	public async Task ImportOfxAsync_ValidatesArgs()
	{
		OfxAdapter adapter = new(this.DocumentViewModel);
		await Assert.ThrowsAsync<ArgumentNullException>("ofxFilePath", () => adapter.ImportOfxAsync(null!, this.TimeoutToken));
		await Assert.ThrowsAsync<ArgumentException>("ofxFilePath", () => adapter.ImportOfxAsync(string.Empty, this.TimeoutToken));
	}

	[Fact]
	public async Task ImportOfxAsync_NoMatch()
	{
		int result = await this.adapter.ImportOfxAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);
		Assert.Equal(0, result);
	}

	[Fact]
	public async Task ImportOfxAsync_SelectedAccountRetained()
	{
		int chooseAccountFuncInvocationCount = 0;
		this.UserNotification.ChooseAccountFunc = (string prompt, AccountViewModel? defaultAccount, CancellationToken cancellationToken) =>
		{
			chooseAccountFuncInvocationCount++;
			return Task.FromResult<AccountViewModel?>(this.checking);
		};
		int result = await this.adapter.ImportOfxAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);
		Assert.Equal(1, chooseAccountFuncInvocationCount);
		Assert.Equal(1, result);

		Assert.Equal("1234", this.checking.OfxAcctId);
		Assert.Equal("031176110", this.checking.OfxBankId);

		// Verify that the second time we import a file for this account, we don't need to ask the user which account to use.
		result = await this.adapter.ImportOfxAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);
		Assert.Equal(1, chooseAccountFuncInvocationCount);
		Assert.Equal(1, result);
	}

	[Fact]
	public async Task ImportOfxAsync_BringsInTransactions()
	{
		this.UserNotification.ChooseAccountResult = this.checking;
		int result = await this.adapter.ImportOfxAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);
		Assert.Equal(1, result);

		Assert.Equal(3, this.checking.Transactions.Count(t => t.IsPersisted));

		BankingTransactionViewModel tx1 = this.checking.Transactions[0];
		Assert.Equal(-86, tx1.Amount);
		Assert.Equal(ExpectedDate(new DateTime(2022, 03, 23, 4, 0, 0, DateTimeKind.Utc)), tx1.When);
		Assert.Equal("202203234238", tx1.Entries[0].OfxFitId);
		Assert.Equal(ClearedState.Cleared, tx1.Entries[0].Cleared);
		Assert.Equal("Memo 3", tx1.Memo);

		BankingTransactionViewModel tx2 = this.checking.Transactions[1];
		Assert.Equal(-13.94m, tx2.Amount);
		Assert.Equal(ExpectedDate(new DateTime(2022, 3, 24, 4, 0, 0, DateTimeKind.Utc)), tx2.When);
		Assert.Equal("202203244239", tx2.Entries[0].OfxFitId);
		Assert.Equal(ClearedState.Cleared, tx2.Entries[0].Cleared);
		Assert.Equal("Memo 2", tx2.Memo);

		BankingTransactionViewModel tx3 = this.checking.Transactions[2];
		Assert.Equal(48, tx3.Amount);
		Assert.Equal(ExpectedDate(new DateTime(2022, 3, 26, 4, 0, 0, DateTimeKind.Utc)), tx3.When);
		Assert.Equal("202203264240", tx3.Entries[0].OfxFitId);
		Assert.Equal(ClearedState.Cleared, tx3.Entries[0].Cleared);
		Assert.Equal("Memo 1", tx3.Memo);

		static DateTime ExpectedDate(DateTime value)
		{
			Requires.Argument(value.Kind == DateTimeKind.Utc, "value", "The test must be written in UTC time.");
			return value.ToLocalTime().Date;
		}
	}

	[Theory, PairwiseData]
	public async Task ImportOfxAsync_DeDupesTransactions(bool acrossSessions)
	{
		this.UserNotification.ChooseAccountResult = this.checking;
		await this.adapter.ImportOfxAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);
		Assert.Equal(3, this.checking.Transactions.Count(t => t.IsPersisted));

		if (acrossSessions)
		{
			this.ReloadViewModel();
			this.UserNotification.ChooseAccountResult = this.checking;
		}

		this.checking.DeleteTransaction(this.checking.Transactions[2]);
		await this.adapter.ImportOfxAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);
		Assert.Equal(3, this.checking.Transactions.Count(t => t.IsPersisted));
	}

	[Fact]
	public async Task ImportOfx_UndoEntireBatch()
	{
		this.UserNotification.ChooseAccountResult = this.checking;
		int result = await this.adapter.ImportOfxAsync(this.GetTestDataFile(Simple1DataFileName), this.TimeoutToken);

		Assert.Equal(3, this.checking.Transactions.Count(t => t.IsPersisted));
		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		this.RefetchViewModels();
		Assert.Empty(this.checking.Transactions.Where(t => t.IsPersisted));
		Assert.Equal(DocumentViewModel.SelectableViews.Banking, this.DocumentViewModel.SelectedViewIndex);
		Assert.Same(this.checking, this.DocumentViewModel.BankingPanel.SelectedAccount);
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.RefetchViewModels();
	}

	private void RefetchViewModels()
	{
		this.adapter = new(this.DocumentViewModel);
		this.checking = (BankingAccountViewModel)this.DocumentViewModel.GetAccount(this.checking.Id);
	}
}
