// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class CategoriesPanelViewModelTests : MoneyTestBase
{
	public CategoriesPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	private CategoriesPanelViewModel ViewModel => this.DocumentViewModel.CategoriesPanel;

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.ViewModel.Categories);
		Assert.Null(this.ViewModel.SelectedCategory);
	}

	[Fact]
	public void NewCategory()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		TestUtilities.AssertRaises(
			h => this.ViewModel.AddingNewCategory += h,
			h => this.ViewModel.AddingNewCategory -= h,
			() => this.ViewModel.NewCategory());
		CategoryViewModel newCategory = Assert.Single(this.ViewModel.Categories);
		Assert.Same(newCategory, this.ViewModel.SelectedCategory);
		Assert.Equal(string.Empty, newCategory.Name);

		newCategory.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Categories).Name);
	}

	[Fact]
	public async Task AddCommand()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		await TestUtilities.AssertRaisesAsync(
			h => this.ViewModel.AddingNewCategory += h,
			h => this.ViewModel.AddingNewCategory -= h,
			() => this.ViewModel.AddCommand.ExecuteAsync());
		CategoryViewModel newCategory = Assert.Single(this.ViewModel.Categories);
		Assert.Same(newCategory, this.ViewModel.SelectedCategory);
		Assert.Equal(string.Empty, newCategory.Name);

		newCategory.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Categories).Name);
	}

	[Fact]
	public async Task AddCommand_Twice()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? newCategory = this.ViewModel.SelectedCategory;
		Assert.NotNull(newCategory);
		newCategory!.Name = "cat";

		await this.ViewModel.AddCommand.ExecuteAsync();
		newCategory = this.ViewModel.SelectedCategory;
		Assert.NotNull(newCategory);
		newCategory!.Name = "dog";

		Assert.Equal(2, this.Money.Categories.Count());
	}

	[Theory, PairwiseData]
	public async Task DeleteCommand(bool saveFirst)
	{
		CategoryViewModel viewModel = this.DocumentViewModel.CategoriesPanel.NewCategory();
		if (saveFirst)
		{
			viewModel.Name = "cat";
		}

		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Empty(this.ViewModel.Categories);
		Assert.Null(this.ViewModel.SelectedCategory);
		Assert.Empty(this.Money.Categories);
	}

	[Fact]
	public async Task DeleteCommand_Multiple()
	{
		var cat1 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat1");
		var cat2 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat2");
		var cat3 = this.DocumentViewModel.CategoriesPanel.NewCategory("cat3");

		this.ViewModel.SelectedCategories = new[] { cat1, cat3 };
		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();

		Assert.Equal("cat2", Assert.Single(this.ViewModel.Categories).Name);
		Assert.Null(this.ViewModel.SelectedCategory);
		Assert.Equal("cat2", Assert.Single(this.Money.Categories).Name);
	}

	[Fact]
	public async Task AddTwiceRedirectsToFirstIfNotCommitted()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? first = this.ViewModel.SelectedCategory;
		Assert.NotNull(first);

		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? second = this.ViewModel.SelectedCategory;
		Assert.Same(first, second);

		first!.Name = "Some category";
		Assert.True(this.ViewModel.AddCommand.CanExecute());
		await this.ViewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? third = this.ViewModel.SelectedCategory;
		Assert.NotNull(third);
		Assert.NotSame(first, third);
		Assert.Equal(string.Empty, third!.Name);
	}

	[Fact]
	public async Task AddThenDelete()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		Assert.True(this.ViewModel.DeleteCommand.CanExecute());
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Null(this.ViewModel.SelectedCategory);
		Assert.Empty(this.ViewModel.Categories);
	}
}
