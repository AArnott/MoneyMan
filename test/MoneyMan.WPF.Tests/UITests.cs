// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows.Data;
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
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.DocumentViewModel.AccountsPanel.NewAccount("Checking");
		this.Window.TransactionDataGrid.SelectedItem = CollectionView.NewItemPlaceholder;
	}
}
