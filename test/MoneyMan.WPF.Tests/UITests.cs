// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows.Data;
using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

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
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.Window.TransactionDataGrid.SelectedItem = CollectionView.NewItemPlaceholder;
	}

	[WpfFact]
	public void CreateInvestmentAccount()
	{
		this.DocumentViewModel.SelectedViewIndex = Nerdbank.MoneyManagement.ViewModels.DocumentViewModel.SelectableViews.Accounts;
		this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		this.DocumentViewModel.AccountsPanel.SelectedAccount!.Name = "Brokerage";
		this.DocumentViewModel.AccountsPanel.SelectedAccount!.Type = Account.AccountType.Investing;
		this.DocumentViewModel.SelectedViewIndex = Nerdbank.MoneyManagement.ViewModels.DocumentViewModel.SelectableViews.Banking;
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.DocumentViewModel.AccountsPanel.SelectedAccount;
	}
}
