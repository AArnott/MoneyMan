// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;
using Nerdbank.MoneyManagement.Adapters;

public class OfxAdapterFacts : AdapterTestBase<OfxAdapter>
{
	private const string Simple1DataFileName = "Simple1.ofx";
	private const string CapitalOneRealMemosDataFileName = "CapitalOneRealMemos.ofx";
	private OfxAdapter adapter;

	public OfxAdapterFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.adapter = new(this.DocumentViewModel);
		this.EnableSqlLogging();
	}

	protected override OfxAdapter Adapter => this.adapter;

	[Fact]
	public void CtorValidatesArgs()
	{
		Assert.Throws<ArgumentNullException>("documentViewModel", () => new OfxAdapter(null!));
	}

	[Fact]
	public async Task ImportAsync_ValidatesArgs()
	{
		await Assert.ThrowsAsync<ArgumentNullException>("filePath", () => this.adapter.ImportAsync(null!, this.TimeoutToken));
		await Assert.ThrowsAsync<ArgumentException>("filePath", () => this.adapter.ImportAsync(string.Empty, this.TimeoutToken));
	}

	[Fact]
	public async Task ImportAsync_NoMatch()
	{
		int result = await this.ImportAsync(Simple1DataFileName);
		Assert.Equal(0, result);
	}

	[Fact]
	public async Task ImportAsync_SelectedAccountRetained()
	{
		int chooseAccountFuncInvocationCount = 0;
		this.UserNotification.ChooseAccountFunc = (string prompt, AccountViewModel? defaultAccount, CancellationToken cancellationToken) =>
		{
			chooseAccountFuncInvocationCount++;
			return Task.FromResult<AccountViewModel?>(this.Checking);
		};
		int result = await this.ImportAsync(Simple1DataFileName);
		Assert.Equal(1, chooseAccountFuncInvocationCount);
		Assert.Equal(3, result);

		Assert.Equal("1234", this.Checking.OfxAcctId);
		Assert.Equal("031176110", this.Checking.OfxBankId);

		// Verify that the second time we import a file for this account, we don't need to ask the user which account to use.
		result = await this.ImportAsync(Simple1DataFileName);
		Assert.Equal(1, chooseAccountFuncInvocationCount);
		Assert.Equal(0, result);
	}

	[Fact]
	public async Task ImportAsync_BringsInTransactions()
	{
		this.UserNotification.ChooseAccountResult = this.Checking;
		int result = await this.ImportAsync(Simple1DataFileName);
		Assert.Equal(3, result);

		Assert.Equal(3, this.Checking.Transactions.Count(t => t.IsPersisted));

		BankingTransactionViewModel tx1 = this.Checking.Transactions[0];
		Assert.Equal(-86, tx1.Amount);
		Assert.Equal(ExpectedDate(new DateTime(2022, 03, 23, 4, 0, 0, DateTimeKind.Utc)), tx1.When);
		Assert.Equal("202203234238", tx1.Entries[0].OfxFitId);
		Assert.Equal(ClearedState.Cleared, tx1.Entries[0].Cleared);
		Assert.Equal("Memo 3", tx1.Memo);

		BankingTransactionViewModel tx2 = this.Checking.Transactions[1];
		Assert.Equal(-13.94m, tx2.Amount);
		Assert.Equal(ExpectedDate(new DateTime(2022, 3, 24, 4, 0, 0, DateTimeKind.Utc)), tx2.When);
		Assert.Equal("202203244239", tx2.Entries[0].OfxFitId);
		Assert.Equal(ClearedState.Cleared, tx2.Entries[0].Cleared);
		Assert.Equal("Memo 2", tx2.Memo);

		BankingTransactionViewModel tx3 = this.Checking.Transactions[2];
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
	public async Task ImportAsync_DeDupesTransactions(bool acrossSessions)
	{
		this.UserNotification.ChooseAccountResult = this.Checking;
		await this.ImportAsync(Simple1DataFileName);
		Assert.Equal(3, this.Checking.Transactions.Count(t => t.IsPersisted));

		if (acrossSessions)
		{
			this.ReloadViewModel();
			this.UserNotification.ChooseAccountResult = this.Checking;
		}

		this.Checking.DeleteTransaction(this.Checking.Transactions[2]);
		await this.ImportAsync(Simple1DataFileName);
		Assert.Equal(3, this.Checking.Transactions.Count(t => t.IsPersisted));
	}

	[Fact]
	public async Task ImportOfx_UndoEntireBatch()
	{
		this.UserNotification.ChooseAccountResult = this.Checking;
		int result = await this.ImportAsync(Simple1DataFileName);

		Assert.Equal(3, this.Checking.Transactions.Count(t => t.IsPersisted));
		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		this.RefetchViewModels();
		Assert.Empty(this.Checking.Transactions.Where(t => t.IsPersisted));
		Assert.Equal(DocumentViewModel.SelectableViews.Banking, this.DocumentViewModel.SelectedViewIndex);
		Assert.Same(this.Checking, this.DocumentViewModel.BankingPanel.SelectedAccount);
	}

	/// <summary>
	/// Verifies that memos get appropriately modified based on bank tendencies to add fluff the user doesn't want to see.
	/// </summary>
	[Fact]
	public async Task MemoProcessing()
	{
		this.UserNotification.ChooseAccountResult = this.Checking;
		await this.ImportAsync(CapitalOneRealMemosDataFileName);

		BankingTransactionViewModel greenlightWithdrawal = this.Checking.Transactions.Single(tx => tx.Amount == -13.94m);
		Assert.Equal("GREENLIGHT APP", greenlightWithdrawal.Payee);
		Assert.True(string.IsNullOrEmpty(greenlightWithdrawal.Memo));

		BankingTransactionViewModel checkCashed = this.Checking.Transactions.Single(tx => tx.Amount == -35);
		Assert.Equal(1030, checkCashed.CheckNumber);
		Assert.True(string.IsNullOrEmpty(checkCashed.Payee));
		Assert.True(string.IsNullOrEmpty(checkCashed.Memo));

		BankingTransactionViewModel githubCredit = this.Checking.Transactions.Single(tx => tx.Amount == 86);
		Assert.Equal("GitHub Sponsors GitHub Spo", githubCredit.Payee);
		Assert.True(string.IsNullOrEmpty(githubCredit.Memo));

		BankingTransactionViewModel mobileDeposit = this.Checking.Transactions.Single(tx => tx.Amount == 48m);
		Assert.True(string.IsNullOrEmpty(mobileDeposit.Payee));
		Assert.Equal("user memo - Check Deposit (Mobile)", mobileDeposit.Memo);

		BankingTransactionViewModel zelleMoneyReturned = this.Checking.Transactions.Single(tx => tx.Amount == 10);
		Assert.Equal("some@email.com", zelleMoneyReturned.Payee);
		Assert.Equal("Zelle money returned", zelleMoneyReturned.Memo);
	}

	protected override void RefetchViewModels()
	{
		base.RefetchViewModels();
		this.adapter = new(this.DocumentViewModel);
	}
}
