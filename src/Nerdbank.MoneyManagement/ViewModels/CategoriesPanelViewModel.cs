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
	private readonly SortedObservableCollection<CategoryAccountViewModel> categories = new(CategorySort.Instance);
	private CategoryAccountViewModel? selectedCategory;
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

	public IReadOnlyList<CategoryAccountViewModel> Categories => this.categories;

	/// <summary>
	/// Gets or sets the selected category, or one of the selected categories.
	/// </summary>
	public CategoryAccountViewModel? SelectedCategory
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

	internal CategoryAccountViewModel? AddingCategory { get; set; }

	public CategoryAccountViewModel NewCategory(string name = "")
	{
		this.AddingNewCategory?.Invoke(this, EventArgs.Empty);
		if (this.AddingCategory is object)
		{
			this.SelectedCategory = this.AddingCategory;
			return this.AddingCategory;
		}

		CategoryAccountViewModel newCategoryAccountViewModel = new(null, this.documentViewModel);

		this.categories.Add(newCategoryAccountViewModel);
		this.SelectedCategory = newCategoryAccountViewModel;
		if (string.IsNullOrEmpty(name))
		{
			this.AddingCategory = newCategoryAccountViewModel;
			newCategoryAccountViewModel.NotifyWhenValid(s =>
			{
				if (this.AddingCategory == s)
				{
					this.AddingCategory = null;
				}
			});
		}
		else
		{
			newCategoryAccountViewModel.Name = name;
		}

		return newCategoryAccountViewModel;
	}

	public void DeleteCategory(CategoryAccountViewModel categoryViewModel)
	{
		this.categories.Remove(categoryViewModel);
		using IDisposable? transaction = this.documentViewModel.MoneyFile.UndoableTransaction($"Deleted category \"{categoryViewModel.Name}\"", categoryViewModel.Model);
		this.documentViewModel.MoneyFile.Delete(categoryViewModel.Model);

		if (this.SelectedCategory == categoryViewModel)
		{
			this.SelectedCategory = null;
		}

		if (this.AddingCategory == categoryViewModel)
		{
			this.AddingCategory = null;
		}
	}

	internal void AddCategory(CategoryAccountViewModel viewModel)
	{
		this.categories.Add(viewModel);
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

	internal CategoryAccountViewModel? FindCategory(int id) => this.Categories.FirstOrDefault(cat => cat.Id == id);

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

		private void NewCategoryAccountViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			CategoryAccountViewModel newCategory = (CategoryAccountViewModel)Requires.NotNull(sender!, nameof(sender));
			if (!string.IsNullOrEmpty(newCategory.Name))
			{
				newCategory.PropertyChanged -= this.NewCategoryAccountViewModel_PropertyChanged;
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
				return this.ExecuteCoreAsync(new CategoryAccountViewModel[] { this.viewModel.SelectedCategory }, cancellationToken);
			}

			return Task.CompletedTask;
		}

		private async Task ExecuteCoreAsync(IList categoryViewModels, CancellationToken cancellationToken)
		{
			using IDisposable? transaction = this.viewModel.documentViewModel.MoneyFile.UndoableTransaction($"Deleted {categoryViewModels.Count} categories.", categoryViewModels.OfType<CategoryAccountViewModel>().FirstOrDefault()?.Model);
			IEnumerable<CategoryAccountViewModel> categories = categoryViewModels.OfType<CategoryAccountViewModel>();
			List<CategoryAccountViewModel> inUse = new(), notInUse = new();
			foreach (CategoryAccountViewModel category in categories)
			{
				if (category.Id is int id && this.viewModel.documentViewModel.MoneyFile.IsAccountInUse(id) is true)
				{
					inUse.Add(category);
				}
				else
				{
					notInUse.Add(category);
				}
			}

			foreach (CategoryAccountViewModel category in notInUse)
			{
				this.viewModel.DeleteCategory(category);
			}

			if (inUse.Count > 0)
			{
				// Ask the user what they want to do about the categories that are in use.
				CategoryAccountViewModel? redirectedCategory = null;
				if (this.viewModel.documentViewModel.UserNotification is { } userNotification)
				{
					List<CategoryAccountViewModel> options = new(this.viewModel.Categories);
					options.RemoveAll(cat => inUse.Contains(cat));
					options.Insert(0, new CategoryAccountViewModel(new Account { Id = -1, Name = "(clear assigned category)" }, this.viewModel.documentViewModel));

					if (options.Count > 1)
					{
						var pickerViewModel = new PickerWindowViewModel("One or more of the categories selected are applied to transactions. How do you want to reassign those transactions?", options)
						{
							Title = "Category in use",
						};
						await userNotification.PresentAsync(pickerViewModel, cancellationToken);
						redirectedCategory = (CategoryAccountViewModel)pickerViewModel.GetSelectedOptionOrThrowCancelled();
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
				this.viewModel.documentViewModel.MoneyFile.ReassignCategory(inUse.Where(cat => cat.IsPersisted).Select(cat => cat.Id), redirectedCategory.Id);

				// Also update the live view models.
				foreach (BankingAccountViewModel account in this.viewModel.documentViewModel.BankingPanel.BankingAccounts)
				{
					account.NotifyReassignCategory(inUse, redirectedCategory.Id == 0 ? null : redirectedCategory);
				}

				// Now actually delete the categories.
				foreach (CategoryAccountViewModel category in inUse)
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

	private class CategorySort : IOptimizedComparer<CategoryAccountViewModel>
	{
		internal static readonly CategorySort Instance = new();

		private CategorySort()
		{
		}

		public int Compare(CategoryAccountViewModel? x, CategoryAccountViewModel? y)
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

		public bool IsPropertySignificant(string propertyName) => propertyName is nameof(CategoryAccountViewModel.Name);
	}
}
