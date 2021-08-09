// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using PCLCommandBase;

	public class CategoriesPanelViewModel : BindableBase
	{
		private readonly ObservableCollection<CategoryViewModel> categories = new ObservableCollection<CategoryViewModel>();
		private CategoryViewModel? selectedCategory;

		public CategoriesPanelViewModel()
		{
			this.AddCommand = new AddCategoryCommand(this);
			this.DeleteCommand = new DeleteCategoryCommand(this);
		}

		public ICommand AddCommand { get; }

		public ICommand DeleteCommand { get; }

		public ObservableCollection<CategoryViewModel> Categories => this.categories;

		public CategoryViewModel? SelectedCategory
		{
			get => this.selectedCategory;
			set => this.SetProperty(ref this.selectedCategory, value);
		}

		private class AddCategoryCommand : CommandBase
		{
			private readonly CategoriesPanelViewModel viewModel;

			public AddCategoryCommand(CategoriesPanelViewModel viewModel)
			{
				this.viewModel = viewModel;
			}

			protected override Task ExecuteCoreAsync(object parameter, CancellationToken cancellationToken)
			{
				CategoryViewModel newCategoryViewModel = new();
				this.viewModel.Categories.Add(newCategoryViewModel);
				this.viewModel.SelectedCategory = newCategoryViewModel;
				return Task.CompletedTask;
			}
		}

		private class DeleteCategoryCommand : CommandBase
		{
			private readonly CategoriesPanelViewModel viewModel;

			public DeleteCategoryCommand(CategoriesPanelViewModel viewModel)
			{
				this.viewModel = viewModel;
				viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
			}

			public override bool CanExecute(object parameter) => base.CanExecute(parameter) && this.viewModel.SelectedCategory is object;

			protected override Task ExecuteCoreAsync(object parameter, CancellationToken cancellationToken)
			{
				this.viewModel.Categories.Remove(this.viewModel.SelectedCategory ?? throw new InvalidOperationException("No category is selected."));
				this.viewModel.SelectedCategory = null;
				return Task.CompletedTask;
			}

			private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(CategoriesPanelViewModel.SelectedCategory))
				{
					this.OnCanExecuteChanged();
				}
			}
		}
	}
}
