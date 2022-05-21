// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class BankingPanelViewModelTests : MoneyTestBase
{
	public BankingPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.EnableSqlLogging();
	}

	private BankingPanelViewModel ViewModel => this.DocumentViewModel.BankingPanel;

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.ViewModel.Accounts);
		Assert.Null(this.ViewModel.SelectedAccount);
	}

	[Fact]
	public void Accounts_RemovesAccountWhenClosed()
	{
		AccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		Assert.Single(this.ViewModel.Accounts);
		checking.IsClosed = true;
		Assert.Empty(this.ViewModel.Accounts);
		checking.IsClosed = false;
		Assert.Single(this.ViewModel.Accounts);
	}

	[Fact]
	public void Accounts_ExcludesClosedAccounts()
	{
		this.Money.Insert(new Account { Name = "Checking", IsClosed = true });
		this.ReloadViewModel();
		Assert.Empty(this.ViewModel.Accounts);
		this.DocumentViewModel.AccountsPanel.Accounts.Single().IsClosed = false;
		Assert.Single(this.ViewModel.Accounts);
	}

	[Fact]
	public void Accounts_NewAccountAlreadyClosed()
	{
		AccountViewModel newAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		newAccount.IsClosed = true; // Set this before the name, since setting the name formally adds it to the db and the Banking panel.
		newAccount.Name = "Checking";
		Assert.Empty(this.ViewModel.Accounts);
	}

	[Fact]
	public void AccountListIsSorted()
	{
		AccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		AccountViewModel savings = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Savings");
		Assert.Same(checking, this.DocumentViewModel.BankingPanel.Accounts[0]);
		Assert.Same(savings, this.DocumentViewModel.BankingPanel.Accounts[1]);

		// Close and reopen the checking account to see if it is reinserted at the top.
		checking.IsClosed = true;
		Assert.Same(savings, this.DocumentViewModel.BankingPanel.Accounts.Single());
		checking.IsClosed = false;
		Assert.Same(checking, this.DocumentViewModel.BankingPanel.Accounts[0]);
		Assert.Same(savings, this.DocumentViewModel.BankingPanel.Accounts[1]);

		// Insert one new account that should sort to the top.
		AccountViewModel anotherChecking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Another checking");
		Assert.Same(anotherChecking, this.DocumentViewModel.BankingPanel.Accounts[0]);
		Assert.Same(checking, this.DocumentViewModel.BankingPanel.Accounts[1]);
		Assert.Same(savings, this.DocumentViewModel.BankingPanel.Accounts[2]);

		// Rename an account and confirm it is re-sorted.
		checking.Name = "The last checking";
		Assert.Same(anotherChecking, this.DocumentViewModel.BankingPanel.Accounts[0]);
		Assert.Same(savings, this.DocumentViewModel.BankingPanel.Accounts[1]);
		Assert.Same(checking, this.DocumentViewModel.BankingPanel.Accounts[2]);
	}
}
