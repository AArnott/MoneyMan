// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class AccountsPanelViewModelTests : MoneyTestBase
{
	public AccountsPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	private AccountsPanelViewModel ViewModel => this.DocumentViewModel.AccountsPanel;

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.ViewModel.Accounts);
		Assert.Null(this.ViewModel.SelectedAccount);
	}

	[Fact]
	public void NewAccount()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		TestUtilities.AssertRaises(
			h => this.ViewModel.AddingNewAccount += h,
			h => this.ViewModel.AddingNewAccount -= h,
			() => this.ViewModel.NewBankingAccount());
		AccountViewModel newAccount = Assert.Single(this.ViewModel.Accounts);
		Assert.Same(newAccount, this.ViewModel.SelectedAccount);
		Assert.Equal(string.Empty, newAccount.Name);

		newAccount.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Accounts).Name);
	}

	[Fact]
	public async Task AddCommand()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		await TestUtilities.AssertRaisesAsync(
			h => this.ViewModel.AddingNewAccount += h,
			h => this.ViewModel.AddingNewAccount -= h,
			() => this.ViewModel.AddCommand.ExecuteAsync());
		AccountViewModel newAccount = Assert.Single(this.ViewModel.Accounts);
		Assert.Same(newAccount, this.ViewModel.SelectedAccount);
		Assert.Equal(string.Empty, newAccount.Name);

		newAccount.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Accounts).Name);
	}

	[Fact]
	public async Task AddCommand_Twice()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? newAccount = this.ViewModel.SelectedAccount;
		Assert.NotNull(newAccount);
		newAccount!.Name = "cat";

		await this.ViewModel.AddCommand.ExecuteAsync();
		newAccount = this.ViewModel.SelectedAccount;
		Assert.NotNull(newAccount);
		newAccount!.Name = "dog";

		Assert.Equal(2, this.Money.Accounts.Count());
	}

	[Fact]
	public async Task AddCommand_Undo()
	{
		const string name = "name";
		await this.ViewModel.AddCommand.ExecuteAsync();
		this.ViewModel.SelectedAccount!.Name = name;
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Banking;

		Assert.True(this.DocumentViewModel.UndoCommand.CanExecute());
		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.DoesNotContain(this.ViewModel.Accounts, acct => acct.Name == name);

		Assert.Equal(DocumentViewModel.SelectableViews.Accounts, this.DocumentViewModel.SelectedViewIndex);
		Assert.Null(this.DocumentViewModel.AccountsPanel.SelectedAccount);
	}

	[Fact]
	public async Task DeleteCommand_Undo()
	{
		const string name = "name";
		AccountViewModel account = this.ViewModel.NewBankingAccount(name);
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.DoesNotContain(this.ViewModel.Accounts, acct => acct.Name == name);
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Banking;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.Contains(this.ViewModel.Accounts, acct => acct.Name == name);

		Assert.Equal(DocumentViewModel.SelectableViews.Accounts, this.DocumentViewModel.SelectedViewIndex);
		Assert.Equal(account.Id, this.DocumentViewModel.AccountsPanel.SelectedAccount?.Id);
	}

	[Theory, PairwiseData]
	public async Task DeleteCommand(bool saveFirst)
	{
		AccountViewModel viewModel = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		if (saveFirst)
		{
			viewModel.Name = "cat";
		}

		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Empty(this.ViewModel.Accounts);
		Assert.Null(this.ViewModel.SelectedAccount);
		Assert.Empty(this.Money.Accounts);
	}

	[Fact]
	public async Task DeleteCommand_Multiple()
	{
		var cat1 = this.DocumentViewModel.AccountsPanel.NewBankingAccount("cat1");
		var cat2 = this.DocumentViewModel.AccountsPanel.NewBankingAccount("cat2");
		var cat3 = this.DocumentViewModel.AccountsPanel.NewBankingAccount("cat3");

		this.ViewModel.SelectedAccounts = new[] { cat1, cat3 };
		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();

		Assert.Equal("cat2", Assert.Single(this.ViewModel.Accounts).Name);
		Assert.Null(this.ViewModel.SelectedAccount);
		Assert.Equal("cat2", Assert.Single(this.Money.Accounts).Name);
	}

	[Fact]
	public async Task AddTwiceRedirectsToFirstIfNotCommitted()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? first = this.ViewModel.SelectedAccount;
		Assert.NotNull(first);

		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? second = this.ViewModel.SelectedAccount;
		Assert.Same(first, second);

		first!.Name = "Some account";
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		AccountViewModel? third = this.ViewModel.SelectedAccount;
		Assert.NotNull(third);
		Assert.NotSame(first, third);
		Assert.Equal(string.Empty, third!.Name);
	}

	[Fact]
	public async Task AddThenDelete()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Null(this.ViewModel.SelectedAccount);
		Assert.Empty(this.ViewModel.Accounts);
	}

	[Fact]
	public void AccountListIsSorted()
	{
		AccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		AccountViewModel savings = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Savings");
		Assert.Same(checking, this.DocumentViewModel.AccountsPanel.Accounts[0]);
		Assert.Same(savings, this.DocumentViewModel.AccountsPanel.Accounts[1]);

		// Insert one new account that should sort to the top.
		AccountViewModel anotherChecking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Another checking");
		Assert.Same(anotherChecking, this.DocumentViewModel.AccountsPanel.Accounts[0]);
		Assert.Same(checking, this.DocumentViewModel.AccountsPanel.Accounts[1]);
		Assert.Same(savings, this.DocumentViewModel.AccountsPanel.Accounts[2]);

		// Rename an account and confirm it is re-sorted.
		checking.Name = "The last checking";
		Assert.Same(anotherChecking, this.DocumentViewModel.AccountsPanel.Accounts[0]);
		Assert.Same(savings, this.DocumentViewModel.AccountsPanel.Accounts[1]);
		Assert.Same(checking, this.DocumentViewModel.AccountsPanel.Accounts[2]);
	}
}
