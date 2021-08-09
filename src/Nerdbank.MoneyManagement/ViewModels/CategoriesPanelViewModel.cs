// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System.Collections.ObjectModel;
	using System.Windows.Input;
	using PCLCommandBase;

	public class CategoriesPanelViewModel : BindableBase
	{
		private ObservableCollection<CategoryViewModel> categories = new ObservableCollection<CategoryViewModel>();
		private CategoryViewModel? selectedCategory;

		public CategoriesPanelViewModel()
		{
			this.AddCommand = new AddCategoryCommand(this);
			this.DeleteCommand = new DeleteCategoryCommand(this);
		}

		public ICommand AddCommand { get; }

		public ICommand DeleteCommand { get; }

		public ObservableCollection<CategoryViewModel> Categories
		{
			get => this.categories;
			set => this.SetProperty(ref this.categories, value);
		}

		public CategoryViewModel? SelectedCategory
		{
			get => this.selectedCategory;
			set => this.SetProperty(ref this.selectedCategory, value);
		}
	}
}
