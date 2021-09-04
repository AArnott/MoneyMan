﻿// Copyright (c) Andrew Arnott. All rights reserved.
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
	using Validation;

	public class CategoriesPanelViewModel : BindableBase
	{
		private readonly MoneyFile moneyFile;
		private CategoryViewModel? selectedCategory;

		public CategoriesPanelViewModel(MoneyFile moneyFile)
		{
			this.AddCommand = new AddCategoryCommand(this);
			this.DeleteCommand = new DeleteCategoryCommand(this);
			this.moneyFile = moneyFile;
		}

		public CommandBase AddCommand { get; }

		public CommandBase DeleteCommand { get; }

		public ObservableCollection<CategoryViewModel> Categories { get; } = new();

		public CategoryViewModel? SelectedCategory
		{
			get => this.selectedCategory;
			set => this.SetProperty(ref this.selectedCategory, value);
		}

		internal CategoryViewModel? AddingCategory { get; set; }

		private class AddCategoryCommand : CommandBase
		{
			private readonly CategoriesPanelViewModel viewModel;

			public AddCategoryCommand(CategoriesPanelViewModel viewModel)
			{
				this.viewModel = viewModel;
			}

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				if (this.viewModel.AddingCategory is object)
				{
					// We are already in the state of adding a category. Re-select it.
					this.viewModel.SelectedCategory = this.viewModel.AddingCategory;
				}
				else
				{
					CategoryViewModel newCategoryViewModel = new(null, this.viewModel.moneyFile);
					newCategoryViewModel.PropertyChanged += this.NewCategoryViewModel_PropertyChanged;
					newCategoryViewModel.Model = new Category();
					this.viewModel.Categories.Add(newCategoryViewModel);
					this.viewModel.SelectedCategory = newCategoryViewModel;
					this.viewModel.AddingCategory = newCategoryViewModel;
				}

				return Task.CompletedTask;
			}

			private void NewCategoryViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				CategoryViewModel newCategory = (CategoryViewModel)Requires.NotNull(sender!, nameof(sender));
				if (!string.IsNullOrEmpty(newCategory.Name))
				{
					newCategory.PropertyChanged -= this.NewCategoryViewModel_PropertyChanged;
				}

				if (this.viewModel.AddingCategory == newCategory)
				{
					this.viewModel.AddingCategory = null;
				}
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

			public override bool CanExecute(object? parameter) => base.CanExecute(parameter) && this.viewModel.SelectedCategory is object;

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				CategoryViewModel viewModel = this.viewModel.SelectedCategory ?? throw new InvalidOperationException("No category is selected.");
				this.viewModel.Categories.Remove(viewModel);
				this.viewModel.SelectedCategory = null;
				if (viewModel.Model is object)
				{
					this.viewModel.moneyFile.Delete(viewModel.Model);
				}

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