// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.ViewModels;

namespace MoneyMan;

internal class InvestingTransactionEditingSelector : DataTemplateSelector
{
	/// <summary>
	/// Gets or sets a template with selection for asset, amount, and price.
	/// </summary>
	public DataTemplate? BuySellShares { get; set; }

	/// <summary>
	/// Gets or sets a template with selection for asset, amount, and acquisition date.
	/// </summary>
	public DataTemplate? AddShares { get; set; }

	/// <summary>
	/// Gets or sets a template with selection for asset and amount.
	/// </summary>
	public DataTemplate? RemoveShares { get; set; }

	/// <summary>
	/// Gets or sets a template with selection for amount of a currency.
	/// </summary>
	public DataTemplate? CashOnly { get; set; }

	/// <summary>
	/// Gets or sets a template with selection for asset and amount of a currency.
	/// </summary>
	public DataTemplate? Dividend { get; set; }

	/// <summary>
	/// Gets or sets a template with selection for removed asset and amount as well as added asset and amount.
	/// </summary>
	public DataTemplate? Exchange { get; set; }

	/// <summary>
	/// Gets or sets a template with selection for asset, amount, and other account.
	/// </summary>
	public DataTemplate? Transfer { get; set; }

	public override DataTemplate? SelectTemplate(object item, DependencyObject container)
	{
		DataGridCell cell = (DataGridCell)((ContentPresenter)container).Parent;
		InvestingTransactionViewModel? viewModel = (InvestingTransactionViewModel)cell.DataContext;

		return viewModel?.Action switch
		{
			TransactionAction.Buy or TransactionAction.Sell => this.BuySellShares,
			TransactionAction.Interest or TransactionAction.Deposit or TransactionAction.Withdraw => this.CashOnly,
			TransactionAction.Add => this.AddShares,
			TransactionAction.Remove => this.RemoveShares,
			TransactionAction.Dividend => this.Dividend,
			TransactionAction.Exchange => this.Exchange,
			TransactionAction.Transfer => this.Transfer,
			_ => null,
		};
	}
}
