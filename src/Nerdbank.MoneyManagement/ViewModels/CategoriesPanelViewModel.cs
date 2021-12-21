// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using PCLCommandBase;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

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

	/// <summary>
	/// Occurs when <see cref="NewCategory(string)"/> is called or the <see cref="AddCommand" /> command is invoked.
	/// </summary>
	/// <remarks>
	/// Views are expected to set focus on the Name text field in response to this event.
	/// </remarks>
	public event EventHandler? AddingNewCategory;

	public string Title => "Categories";

	public CommandBase AddCommand { get; }

	public string AddCommandCaption => "_Add new";

	public string NameLabel => "_Name";

	public int NameMaxLength => 100;

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
		this.AddingNewCategory?.Invoke(this, EventArgs.Empty);
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
			using IDisposable? transaction = this.documentViewModel.MoneyFile?.UndoableTransaction($"Deleted category \"{categoryViewModel.Name}\"", categoryViewModel.Model);
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

	/// <summary>
	/// Clears the view model without deleting anything from the database.
	/// </summary>
	internal void ClearViewModel()
	{
		this.categories.Clear();
		this.selectedCategory = null;
		this.selectedCategories?.Clear();
	}

	internal CategoryViewModel? FindCategory(int id) => this.Categories.FirstOrDefault(cat => cat.Id == id);

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
				return this.ExecuteCoreAsync(this.viewModel.SelectedCategories, cancellationToken);
			}

			if (this.viewModel.SelectedCategory is object)
			{
				return this.ExecuteCoreAsync(new CategoryViewModel[] { this.viewModel.SelectedCategory }, cancellationToken);
			}

			return Task.CompletedTask;
		}

		private async Task ExecuteCoreAsync(IList categoryViewModels, CancellationToken cancellationToken)
		{
			using IDisposable? transaction = this.viewModel.documentViewModel.MoneyFile?.UndoableTransaction($"Deleted {categoryViewModels.Count} categories.", categoryViewModels.OfType<CategoryViewModel>().FirstOrDefault()?.Model);
			IEnumerable<CategoryViewModel> categories = categoryViewModels.OfType<CategoryViewModel>();
			List<CategoryViewModel> inUse = new(), notInUse = new();
			foreach (CategoryViewModel category in categories)
			{
				if (category.Id is int id && this.viewModel.documentViewModel.MoneyFile?.IsCategoryInUse(id) is true)
				{
					inUse.Add(category);
				}
				else
				{
					notInUse.Add(category);
				}
			}

			foreach (CategoryViewModel category in notInUse)
			{
				this.viewModel.DeleteCategory(category);
			}

			if (inUse.Count > 0)
			{
				// Ask the user what they want to do about the categories that are in use.
				CategoryViewModel? redirectedCategory = null;
				if (this.viewModel.documentViewModel.UserNotification is { } userNotification)
				{
					List<CategoryViewModel> options = new(this.viewModel.Categories);
					options.RemoveAll(cat => inUse.Contains(cat));
					options.Insert(0, new CategoryViewModel { Name = "(clear assigned category)" });

					if (options.Count > 1)
					{
						var pickerViewModel = new PickerWindowViewModel("One or more of the categories selected are applied to transactions. How do you want to reassign those transactions?", options)
						{
							Title = "Category in use",
						};
						await userNotification.PresentAsync(pickerViewModel, cancellationToken);
						redirectedCategory = (CategoryViewModel)pickerViewModel.GetSelectedOptionOrThrowCancelled();
					}
					else
					{
						// No need to ask the user what to do when there is only one option.
						redirectedCategory = options[0];
					}
				}
				else
				{
					throw new NotSupportedException("Some categories are used by transactions but no UI is attached to prompt the user for how to deal with it.");
				}

				// Update every transaction in the database, including those for which no view model has been created.
				if (this.viewModel.documentViewModel.MoneyFile is object)
				{
					this.viewModel.documentViewModel.MoneyFile.ReassignCategory(inUse.Where(cat => cat.Id.HasValue).Select(cat => cat.Id!.Value), redirectedCategory.Id);
				}

				// Also update the live view models.
				foreach (BankingAccountViewModel account in this.viewModel.documentViewModel.BankingPanel.BankingAccounts)
				{
					account.NotifyReassignCategory(inUse, redirectedCategory);
				}

				// Now actually delete the categories.
				foreach (CategoryViewModel category in inUse)
				{
					this.viewModel.DeleteCategory(category);
				}
			}
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
