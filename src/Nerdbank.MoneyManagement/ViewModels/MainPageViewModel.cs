// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using PCLCommandBase;

	public class MainPageViewModel : BindableBase
	{
		private AccountsPanelViewModel accountsPanel = new AccountsPanelViewModel();
		private CategoriesPanelViewModel categoriesPanel = new CategoriesPanelViewModel();

		public AccountsPanelViewModel AccountsPanel
		{
			get => this.accountsPanel;
			set => this.SetProperty(ref this.accountsPanel, value);
		}

		public CategoriesPanelViewModel CategoriesPanel
		{
			get => this.categoriesPanel;
			set => this.SetProperty(ref this.categoriesPanel, value);
		}
	}
}
