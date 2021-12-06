// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.ViewModels;
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
		BankingAccountViewModel checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.DocumentViewModel.BankingPanel.SelectedAccount = checking;
		this.DocumentViewModel.SelectedTransaction = checking.Transactions[^1];
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
