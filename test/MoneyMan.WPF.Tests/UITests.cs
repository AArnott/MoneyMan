// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows.Threading;

[Trait("UI", "")]
public class UITests : UITestBase
{
	public UITests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[WpfFact]
	public void SelectNewRowInAccountGrid()
	{
		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.DocumentViewModel.BankingPanel.SelectedAccount = checking;
		this.DocumentViewModel.SelectedTransaction = checking.Transactions[^1];
	}

	[WpfFact]
	public async Task Split()
	{
		AccountViewModel groceries = this.DocumentViewModel.CategoriesPanel.NewCategory("Groceries");
		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.DocumentViewModel.BankingPanel.SelectedAccount = checking;
		var tx = checking.Transactions[^1];
		this.DocumentViewModel.SelectedTransaction = tx;
		tx.Amount = 10;
		tx.OtherAccount = groceries;
		await Dispatcher.Yield(DispatcherPriority.ContextIdle);
		await tx.SplitCommand.ExecuteAsync();
	}

	[WpfFact]
	public void CreateInvestmentAccount()
	{
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Accounts;
		this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		this.DocumentViewModel.AccountsPanel.SelectedAccount!.Name = "Brokerage";
		this.DocumentViewModel.AccountsPanel.SelectedAccount!.Type = Account.AccountType.Investing;
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Banking;
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.DocumentViewModel.AccountsPanel.SelectedAccount;
	}

	[WpfFact]
	public async Task Undo()
	{
		this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		Assert.NotNull(this.DocumentViewModel.ConfigurationPanel.PreferredAsset);
		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.NotNull(this.DocumentViewModel.ConfigurationPanel.PreferredAsset);
	}
}
