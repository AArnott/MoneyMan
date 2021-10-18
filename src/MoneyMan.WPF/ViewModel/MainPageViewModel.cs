// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.ViewModel
{
	using Nerdbank.MoneyManagement.ViewModels;

	public class MainPageViewModel : MainPageViewModelBase
	{
		internal MainWindow MainWindow { get; set; } = null!;

		public override void ReplaceViewModel(DocumentViewModel documentViewModel)
		{
			base.ReplaceViewModel(documentViewModel);

			this.Document.CategoriesPanel.SelectedCategories = this.MainWindow.CategoriesListView.SelectedItems;
			this.Document.SelectedTransactions = this.MainWindow.TransactionDataGrid.SelectedItems;
		}
	}
}
