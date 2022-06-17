// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class DocumentViewModelTests : MoneyTestBase
{
	public DocumentViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.EnableSqlLogging();
	}

	[Fact]
	public void LoadFromFile()
	{
		this.Money.InsertAll(new ModelBase[]
		{
			new Account { Name = "Checking" },
			new Account { Name = "Cat1", Type = Account.AccountType.Category },
		});
		DocumentViewModel documentViewModel = new(this.Money);
		Assert.Contains(documentViewModel.BankingPanel?.Accounts, acct => acct.Name == "Checking");
		Assert.Contains(documentViewModel.CategoriesPanel?.Categories, cat => cat.Name == "Cat1");
	}

	[Fact]
	public void Undo_NewFile()
	{
		Assert.False(this.DocumentViewModel.UndoCommand.CanExecute());
	}

	[Fact]
	public async Task UndoCommandCaption_PropertyChangeEvent()
	{
		await TestUtilities.AssertPropertyChangedEventAsync(
			this.DocumentViewModel.UndoCommand,
			async delegate
			{
				await this.DocumentViewModel.AccountsPanel.AddCommand.ExecuteAsync();
				this.DocumentViewModel.AccountsPanel.SelectedAccount!.Name = "some new account";
			},
			nameof(this.DocumentViewModel.UndoCommand.Caption));
	}

	[Fact]
	public void NewFileGetsDefaultCategories()
	{
		DocumentViewModel documentViewModel = DocumentViewModel.CreateNew(MoneyFile.Load(":memory:"));
		Assert.Contains(documentViewModel.CategoriesPanel!.Categories, cat => cat.Name == "Groceries");
	}

	[Fact]
	public void NetWorth()
	{
		Account account = new() { Name = "Checking", CurrencyAssetId = this.Money.PreferredAssetId };
		this.Money.Insert(account);
		this.Money.Action.Deposit(account, 10);
		Assert.Equal(10, this.DocumentViewModel.NetWorth);

		TestUtilities.AssertPropertyChangedEvent(this.DocumentViewModel, () => this.Money.Action.Withdraw(account, 3), nameof(this.DocumentViewModel.NetWorth));
		Assert.Equal(7, this.DocumentViewModel.NetWorth);

		this.DocumentViewModel.AccountsPanel.Accounts.Single().IsClosed = true;
		Assert.Equal(0, this.DocumentViewModel.NetWorth);
	}

	[Fact]
	public void NewAccount()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);
	}

	[Theory]
	[InlineData(Account.AccountType.Banking)]
	[InlineData(Account.AccountType.Investing)]
	public void AddedAccountAddsToTransactionTargets(Account.AccountType type)
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount(type);
		accountViewModel.Name = "some new account";
		Account account = Assert.Single(this.Money.Accounts);
		Assert.Equal(accountViewModel.Name, account.Name);
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Theory]
	[InlineData(Account.AccountType.Banking)]
	[InlineData(Account.AccountType.Investing)]
	public void DeletedAccountRemovesFromTransactionTargets(Account.AccountType type)
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount(type);
		accountViewModel.Name = "some new account";
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.AccountsPanel.DeleteAccount(accountViewModel);
		Assert.DoesNotContain(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void AddedCategoryAddsToTransactionTargets()
	{
		CategoryAccountViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("some new category");
		Account category = Assert.Single(this.Money.Categories);
		Assert.Equal(categoryViewModel.Name, category.Name);
		Assert.Contains(categoryViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void DeletedCategoryRemovesFromTransactionTargets()
	{
		CategoryAccountViewModel categoryViewModel = this.DocumentViewModel.CategoriesPanel.NewCategory("some new category");
		Assert.Contains(categoryViewModel, this.DocumentViewModel.TransactionTargets);

		this.DocumentViewModel.CategoriesPanel.DeleteCategory(categoryViewModel);
		Assert.DoesNotContain(categoryViewModel, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void TransactionTargets_DoesNotIncludeVolatileAccounts()
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		Assert.DoesNotContain(accountViewModel, this.DocumentViewModel.TransactionTargets);
		accountViewModel.Name = "Checking";
		Assert.Contains(accountViewModel, this.DocumentViewModel.TransactionTargets);
	}

	/// <summary>
	/// Verifies that transaction targets includes closed accounts.
	/// This is important because editing old transactions must be able to show that it possibly transferred to/from an account that is now closed.
	/// </summary>
	[Fact]
	public void TransactionTargetsIncludesClosedAccounts()
	{
		AccountViewModel closed = this.DocumentViewModel.AccountsPanel.NewBankingAccount("ToBeClosed");
		Assert.Contains(closed, this.DocumentViewModel.TransactionTargets);
		closed.IsClosed = true;
		Assert.Contains(closed, this.DocumentViewModel.TransactionTargets);
		this.ReloadViewModel();
		Assert.Contains(this.DocumentViewModel.TransactionTargets, tt => tt?.Name == closed.Name);
	}

	[Fact]
	public void TransactionTargets_IncludesSplitSingleton()
	{
		Assert.Contains(this.DocumentViewModel.SplitCategory, this.DocumentViewModel.TransactionTargets);
	}

	[Fact]
	public void TransactionTargets_IsSorted()
	{
		AccountViewModel accountG = this.DocumentViewModel.AccountsPanel.NewBankingAccount("g");
		AccountViewModel accountA = this.DocumentViewModel.AccountsPanel.NewBankingAccount("a");
		CategoryAccountViewModel categoryA = this.DocumentViewModel.CategoriesPanel.NewCategory("a");
		CategoryAccountViewModel categoryG = this.DocumentViewModel.CategoriesPanel.NewCategory("g");
		Assert.Equal<AccountViewModel?>(
			new AccountViewModel?[] { null, categoryA, categoryG, this.DocumentViewModel.SplitCategory, accountA, accountG },
			this.DocumentViewModel.TransactionTargets);
	}

	[Theory, PairwiseData]
	public void GetAccount(bool closed)
	{
		AccountViewModel accountViewModel = this.DocumentViewModel.AccountsPanel.NewAccount(Account.AccountType.Banking, "account");
		if (closed)
		{
			accountViewModel.IsClosed = true;
		}

		Assert.Same(accountViewModel, this.DocumentViewModel.GetAccount(accountViewModel.Id));
		this.ReloadViewModel();
		Assert.Equal(accountViewModel.Id, this.DocumentViewModel.GetAccount(accountViewModel.Id).Id);
	}

	[Fact]
	public void Reset()
	{
		AccountViewModel account = this.DocumentViewModel.AccountsPanel.NewBankingAccount("checking");
		this.DocumentViewModel.Reset();
		Assert.Equal(3, this.DocumentViewModel.TransactionTargets.Count);
		Assert.Contains(this.DocumentViewModel.TransactionTargets, tt => tt?.Name == account.Name);
		Assert.Contains(this.DocumentViewModel.TransactionTargets, tt => tt?.Name == this.DocumentViewModel.SplitCategory.Name);
		Assert.Contains(null, this.DocumentViewModel.TransactionTargets);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.DocumentViewModel.Dispose();
		}

		base.Dispose(disposing);
	}
}
