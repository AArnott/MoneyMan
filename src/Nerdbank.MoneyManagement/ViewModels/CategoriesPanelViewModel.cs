// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System.Collections.ObjectModel;
	using PCLCommandBase;

	public class CategoriesPanelViewModel : BindableBase
	{
		private ObservableCollection<CategoryViewModel> categories = new ObservableCollection<CategoryViewModel>();

		public ObservableCollection<CategoryViewModel> Categories
		{
			get => this.categories;
			set => this.SetProperty(ref this.categories, value);
		}
	}
}
