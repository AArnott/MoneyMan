// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.ViewModel
{
	using System;
	using Nerdbank.MoneyManagement.ViewModels;

	public class MainPageViewModel : MainPageViewModelBase
	{
		internal MainWindow MainWindow { get; set; } = null!;

		public override void ReplaceViewModel(DocumentViewModel documentViewModel)
		{
			this.Document.AccountsPanel.AddingNewAccount -= this.AccountsPanel_AddingNewAccount;
			this.Document.CategoriesPanel.AddingNewCategory -= this.CategoriesPanel_AddingNewCategory;

			base.ReplaceViewModel(documentViewModel);

			this.Document.UserNotification = new UserNotification(this.MainWindow);
			this.Document.AccountsPanel.AddingNewAccount += this.AccountsPanel_AddingNewAccount;
			this.Document.CategoriesPanel.AddingNewCategory += this.CategoriesPanel_AddingNewCategory;
			this.Document.CategoriesPanel.SelectedCategories = this.MainWindow.CategoriesListView.SelectedItems;
			this.Document.SelectedTransactions = this.MainWindow.TransactionDataGrid.SelectedItems;
		}

		private void AccountsPanel_AddingNewAccount(object? sender, EventArgs e)
		{
			this.MainWindow.AccountName.Focus();
		}

		private void CategoriesPanel_AddingNewCategory(object? sender, System.EventArgs e)
		{
			this.MainWindow.CategoryName.Focus();
		}
	}
}
