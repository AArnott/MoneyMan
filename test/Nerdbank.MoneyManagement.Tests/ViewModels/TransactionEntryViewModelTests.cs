// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class TransactionEntryViewModelTests : MoneyTestBase
{
	private BankingAccountViewModel checkingAccount;
	private CategoryAccountViewModel spendingCategory;
	private BankingTransactionViewModel transaction;
	private TransactionEntryViewModel viewModel;

	private decimal amount = 5.5m;
	private string ofxFitId = "someFitId";
	private string memo = "Some memo";

	public TransactionEntryViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.checkingAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.spendingCategory = this.DocumentViewModel.CategoriesPanel.NewCategory("Spending");
		this.transaction = this.checkingAccount.NewTransaction();
		this.viewModel = this.transaction.NewSplit();
		this.EnableSqlLogging();
	}

	[Fact]
	public void OfxFitId()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.OfxFitId = this.ofxFitId,
			nameof(this.viewModel.OfxFitId));
		Assert.Equal(this.ofxFitId, this.viewModel.OfxFitId);
	}

	[Fact]
	public void Amount()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Amount = this.amount,
			nameof(this.viewModel.Amount));
		Assert.Equal(this.amount, this.viewModel.Amount);
	}

	[Fact]
	public void Category()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Account = this.spendingCategory,
			nameof(this.viewModel.Account));
		Assert.Equal(this.spendingCategory, this.viewModel.Account);
	}

	[Fact]
	public void AvailableTransactionTargets()
	{
		Assert.DoesNotContain(this.viewModel.AvailableTransactionTargets, tt => tt == this.DocumentViewModel.SplitCategory);
		Assert.DoesNotContain(this.viewModel.AvailableTransactionTargets, tt => tt == this.viewModel.ThisAccount);
		Assert.NotEmpty(this.viewModel.AvailableTransactionTargets);
	}

	[Fact]
	public void Memo()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Memo = this.memo,
			nameof(this.viewModel.Memo));
		Assert.Equal(this.memo, this.viewModel.Memo);
	}

	[Fact]
	public void ApplyTo()
	{
		this.viewModel.Account = this.spendingCategory;
		this.viewModel.Amount = this.amount;
		this.viewModel.Asset = this.DocumentViewModel.DefaultCurrency;
		this.viewModel.Memo = this.memo;
		this.viewModel.Cleared = ClearedState.Cleared;
		this.viewModel.OfxFitId = this.ofxFitId;
		this.viewModel.ApplyToModel();

		Assert.Equal(-this.amount, this.viewModel.Model.Amount);
		Assert.Equal(this.memo, this.viewModel.Model.Memo);
		Assert.Equal(ClearedState.Cleared, this.viewModel.Model.Cleared);
		Assert.Equal(this.ofxFitId, this.viewModel.Model.OfxFitId);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>("model", () => this.viewModel.CopyFrom(null!));

		TransactionEntry splitTransaction = new()
		{
			Amount = this.amount,
			AssetId = this.checkingAccount.CurrencyAsset!.Id,
			Memo = this.memo,
			AccountId = this.spendingCategory.Id,
			OfxFitId = this.ofxFitId,
			Cleared = ClearedState.Cleared,
		};

		this.viewModel.CopyFrom(splitTransaction);

		Assert.Equal(-splitTransaction.Amount, this.viewModel.Amount);
		Assert.Equal(splitTransaction.Memo, this.viewModel.Memo);
		Assert.Equal(this.spendingCategory.Id, this.viewModel.Account?.Id);
		Assert.Equal(this.ofxFitId, this.viewModel.OfxFitId);
		Assert.Equal(ClearedState.Cleared, this.viewModel.Cleared);

		splitTransaction.AccountId = 0;
		this.viewModel.CopyFrom(splitTransaction);
		Assert.Null(this.viewModel.Account);
	}
}
