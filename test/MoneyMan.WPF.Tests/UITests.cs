// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using MoneyMan;
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
