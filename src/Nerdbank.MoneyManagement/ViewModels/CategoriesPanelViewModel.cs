// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System.Collections;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using PCLCommandBase;
	using Validation;

	public class CategoriesPanelViewModel : BindableBase
	{
		private readonly DocumentViewModel documentViewModel;
		private readonly SortedObservableCollection<CategoryViewModel> categories = new(CategorySort.Instance);
		private CategoryViewModel? selectedCategory;
		private IList? selectedCategories;

		public CategoriesPanelViewModel(DocumentViewModel documentViewModel)
		{
			this.AddCommand = new AddCategoryCommand(this);
			this.DeleteCommand = new DeleteCategoryCommand(this);
			this.documentViewModel = documentViewModel;
		}

		public string Title => "Categories";

		public CommandBase AddCommand { get; }

		public string AddCommandCaption => "_Add new";

		/// <summary>
		/// Gets a command that deletes all categories in the <see cref="SelectedCategories"/> collection, if that property is set;
		/// otherwise the <see cref="SelectedCategory"/> is deleted.
		/// </summary>
		public CommandBase DeleteCommand { get; }

		public SortedObservableCollection<CategoryViewModel> Categories => this.categories;

		/// <summary>
		/// Gets or sets the selected category, or one of the selected categories.
		/// </summary>
		public CategoryViewModel? SelectedCategory
		{
			get => this.selectedCategory;
			set => this.SetProperty(ref this.selectedCategory, value);
		}

		/// <summary>
		/// Gets or sets a collection of selected categories.
		/// </summary>
		/// <remarks>
		/// This is optional. When set, the <see cref="DeleteCommand"/> will use this collection as the set of categories to delete.
		/// When not set, the <see cref="SelectedCategory"/> will be used by the <see cref="DeleteCommand"/>.
		/// </remarks>
		public IList? SelectedCategories
		{
			get => this.selectedCategories;
			set => this.SetProperty(ref this.selectedCategories, value);
		}

		internal CategoryViewModel? AddingCategory { get; set; }

		public CategoryViewModel NewCategory(string name = "")
		{
			if (this.AddingCategory is object)
			{
				this.SelectedCategory = this.AddingCategory;
				return this.AddingCategory;
			}

			CategoryViewModel newCategoryViewModel = new(null, this.documentViewModel.MoneyFile)
			{
				Model = new(),
			};

			this.categories.Add(newCategoryViewModel);
			this.SelectedCategory = newCategoryViewModel;
			if (string.IsNullOrEmpty(name))
			{
				this.AddingCategory = newCategoryViewModel;
				newCategoryViewModel.NotifyWhenValid(s =>
				{
					if (this.AddingCategory == s)
					{
						this.AddingCategory = null;
					}
				});
			}
			else
			{
				newCategoryViewModel.Name = name;
			}

			return newCategoryViewModel;
		}

		public void DeleteCategory(CategoryViewModel categoryViewModel)
		{
			this.categories.Remove(categoryViewModel);
			if (categoryViewModel.Model is object)
			{
				this.documentViewModel.MoneyFile?.Delete(categoryViewModel.Model);
			}

			if (this.SelectedCategory == categoryViewModel)
			{
				this.SelectedCategory = null;
			}

			if (this.AddingCategory == categoryViewModel)
			{
				this.AddingCategory = null;
			}
		}

		private class AddCategoryCommand : CommandBase
		{
			private readonly CategoriesPanelViewModel viewModel;

			public AddCategoryCommand(CategoriesPanelViewModel viewModel)
			{
				this.viewModel = viewModel;
			}

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				this.viewModel.NewCategory();
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
			private INotifyCollectionChanged? subscribedSelectedCategories;

			public DeleteCategoryCommand(CategoriesPanelViewModel viewModel)
			{
				this.viewModel = viewModel;
				this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
				this.SubscribeToSelectionChanged();
			}

			public string Caption => "_Delete";

			public override bool CanExecute(object? parameter) => base.CanExecute(parameter) && (this.viewModel.SelectedCategories?.Count > 0 || this.viewModel.SelectedCategory is object);

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				if (this.viewModel.SelectedCategories is object)
				{
					foreach (CategoryViewModel category in this.viewModel.SelectedCategories.OfType<CategoryViewModel>().ToList())
					{
						this.viewModel.DeleteCategory(category);
					}
				}
				else if (this.viewModel.SelectedCategory is object)
				{
					this.viewModel.DeleteCategory(this.viewModel.SelectedCategory);
				}

				return Task.CompletedTask;
			}

			private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName is nameof(this.viewModel.SelectedCategories))
				{
					this.SubscribeToSelectionChanged();
				}
				else if (e.PropertyName is nameof(this.viewModel.SelectedCategory))
				{
					this.OnCanExecuteChanged();
				}
			}

			private void SelectedCategories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.OnCanExecuteChanged();

			private void SubscribeToSelectionChanged()
			{
				if (this.subscribedSelectedCategories is object)
				{
					this.subscribedSelectedCategories.CollectionChanged -= this.SelectedCategories_CollectionChanged;
				}

				this.subscribedSelectedCategories = this.viewModel.SelectedCategories as INotifyCollectionChanged;

				if (this.subscribedSelectedCategories is object)
				{
					this.subscribedSelectedCategories.CollectionChanged += this.SelectedCategories_CollectionChanged;
				}
			}
		}

		private class CategorySort : IOptimizedComparer<CategoryViewModel>
		{
			internal static readonly CategorySort Instance = new();

			private CategorySort()
			{
			}

			public int Compare(CategoryViewModel? x, CategoryViewModel? y)
			{
				if (x is null)
				{
					return y is null ? 0 : -1;
				}
				else if (y is null)
				{
					return 1;
				}

				int order = x.Name.CompareTo(y.Name);
				if (order != 0)
				{
					return order;
				}

				return 0;
			}

			public bool IsPropertySignificant(string propertyName) => propertyName is nameof(CategoryViewModel.Name);
		}
	}
}
