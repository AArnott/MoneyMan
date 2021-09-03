// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class CategoriesPanelViewModelTests : MoneyTestBase
{
	private CategoriesPanelViewModel viewModel;

	public CategoriesPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.viewModel = new CategoriesPanelViewModel(this.Money);
	}

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.viewModel.Categories);
		Assert.Null(this.viewModel.SelectedCategory);
	}

	[Fact]
	public async Task AddCommand()
	{
		Assert.True(this.viewModel.AddCommand.CanExecute(null));

		await this.viewModel.AddCommand.ExecuteAsync();
		CategoryViewModel newCategory = Assert.Single(this.viewModel.Categories);
		Assert.Same(newCategory, this.viewModel.SelectedCategory);
		Assert.Equal(string.Empty, newCategory.Name);

		newCategory.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Categories).Name);
	}

	[Fact]
	public async Task AddCommand_Twice()
	{
		await this.viewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? newCategory = this.viewModel.SelectedCategory;
		Assert.NotNull(newCategory);
		newCategory!.Name = "cat";

		await this.viewModel.AddCommand.ExecuteAsync();
		newCategory = this.viewModel.SelectedCategory;
		Assert.NotNull(newCategory);
		newCategory!.Name = "dog";

		Assert.Equal(2, this.Money.Categories.Count());
	}

	[Fact]
	public async Task DeleteCommand_Volatile()
	{
		Assert.False(this.viewModel.DeleteCommand.CanExecute(null));
		this.viewModel.Categories.Add(new CategoryViewModel());
		Assert.False(this.viewModel.DeleteCommand.CanExecute(null));
		this.viewModel.SelectedCategory = this.viewModel.Categories[0];
		Assert.True(this.viewModel.DeleteCommand.CanExecute(null));

		await this.viewModel.DeleteCommand.ExecuteAsync();
		Assert.Empty(this.viewModel.Categories);
		Assert.Null(this.viewModel.SelectedCategory);
	}

	[Fact]
	public async Task DeleteCommand()
	{
		var category = new Category { Name = "cat" };
		this.Money.Insert(category);
		var viewModel = new CategoryViewModel(category, this.Money);
		Assert.False(this.viewModel.DeleteCommand.CanExecute(null));
		this.viewModel.Categories.Add(viewModel);
		Assert.False(this.viewModel.DeleteCommand.CanExecute(null));
		this.viewModel.SelectedCategory = this.viewModel.Categories[0];
		Assert.True(this.viewModel.DeleteCommand.CanExecute(null));

		await this.viewModel.DeleteCommand.ExecuteAsync();
		Assert.Empty(this.viewModel.Categories);
		Assert.Null(this.viewModel.SelectedCategory);
		Assert.Empty(this.Money.Categories);
	}

	[Fact]
	public async Task AddTwiceRedirectsToFirstIfNotCommitted()
	{
		Assert.True(this.viewModel.AddCommand.CanExecute(null));
		await this.viewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? first = this.viewModel.SelectedCategory;
		Assert.NotNull(first);

		Assert.True(this.viewModel.AddCommand.CanExecute(null));
		await this.viewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? second = this.viewModel.SelectedCategory;
		Assert.Same(first, second);

		first!.Name = "Some category";
		Assert.True(this.viewModel.AddCommand.CanExecute(null));
		await this.viewModel.AddCommand.ExecuteAsync();
		CategoryViewModel? third = this.viewModel.SelectedCategory;
		Assert.NotNull(third);
		Assert.NotSame(first, third);
		Assert.Equal(string.Empty, third!.Name);
	}

	[Fact]
	public async Task AddThenDelete()
	{
		await this.viewModel.AddCommand.ExecuteAsync();
		Assert.True(this.viewModel.DeleteCommand.CanExecute(null));
		await this.viewModel.DeleteCommand.ExecuteAsync();
		Assert.Null(this.viewModel.SelectedCategory);
		Assert.Empty(this.viewModel.Categories);
	}
}
