// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class SplitTransactionViewModelTests : MoneyTestBase
{
	private BankingAccountViewModel checkingAccount;
	private CategoryViewModel spendingCategory;
	private BankingTransactionViewModel transaction;
	private SplitTransactionViewModel viewModel;

	private decimal amount = 5.5m;

	private string memo = "Some memo";

	public SplitTransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.checkingAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.spendingCategory = this.DocumentViewModel.CategoriesPanel.NewCategory("Spending");
		this.transaction = this.checkingAccount.NewTransaction();
		this.viewModel = this.transaction.NewSplit();
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
			() => this.viewModel.CategoryOrTransfer = this.spendingCategory,
			nameof(this.viewModel.CategoryOrTransfer));
		Assert.Equal(this.spendingCategory, this.viewModel.CategoryOrTransfer);
	}

	[Fact]
	public void AvailableTransactionTargets()
	{
		Assert.DoesNotContain(this.viewModel.AvailableTransactionTargets, tt => tt == SplitCategoryPlaceholder.Singleton);
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
		this.viewModel.Amount = this.amount;
		this.viewModel.Memo = this.memo;
		this.viewModel.ApplyToModel();

		Assert.Equal(this.amount, this.viewModel.Model!.CreditAmount);
		Assert.Equal(this.memo, this.viewModel.Model.Memo);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>("model", () => this.viewModel.CopyFrom(null!));

		Transaction splitTransaction = new Transaction
		{
			CreditAmount = this.amount,
			Memo = this.memo,
			CategoryId = this.spendingCategory.Id,
			CreditAccountId = this.checkingAccount.Id,
		};

		this.viewModel.CopyFrom(splitTransaction);

		Assert.Equal(splitTransaction.CreditAmount, this.viewModel.Amount);
		Assert.Equal(splitTransaction.Memo, this.viewModel.Memo);
		Assert.Equal(this.spendingCategory.Id, ((CategoryViewModel?)this.viewModel.CategoryOrTransfer)?.Id);

		splitTransaction.CategoryId = null;
		this.viewModel.CopyFrom(splitTransaction);
		Assert.Null(this.viewModel.CategoryOrTransfer);
	}
}
