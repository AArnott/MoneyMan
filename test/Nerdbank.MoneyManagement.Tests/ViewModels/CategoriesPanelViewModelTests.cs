// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class CategoriesPanelViewModelTests : TestBase
{
	private CategoriesPanelViewModel viewModel = new CategoriesPanelViewModel();

	public CategoriesPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.viewModel.Categories);
		Assert.Null(this.viewModel.SelectedCategory);
	}

	[Fact]
	public void AddCommand()
	{
		Assert.True(this.viewModel.AddCommand.CanExecute(null));

		this.viewModel.AddCommand.Execute(null);
		CategoryViewModel newCategory = Assert.Single(this.viewModel.Categories);
		Assert.Same(newCategory, this.viewModel.SelectedCategory);
		Assert.Equal(string.Empty, newCategory.Name);
	}

	[Fact]
	public void DeleteCommand()
	{
		Assert.False(this.viewModel.DeleteCommand.CanExecute(null));
		this.viewModel.Categories.Add(new CategoryViewModel());
		Assert.False(this.viewModel.DeleteCommand.CanExecute(null));
		this.viewModel.SelectedCategory = this.viewModel.Categories[0];
		Assert.True(this.viewModel.DeleteCommand.CanExecute(null));

		this.viewModel.DeleteCommand.Execute(null);
		Assert.Empty(this.viewModel.Categories);
		Assert.Null(this.viewModel.SelectedCategory);
	}

	[Fact]
	public void AddTwiceRedirectsToFirstIfNotCommitted()
	{
		Assert.True(this.viewModel.AddCommand.CanExecute(null));
		this.viewModel.AddCommand.Execute(null);
		CategoryViewModel? first = this.viewModel.SelectedCategory;
		Assert.NotNull(first);

		Assert.True(this.viewModel.AddCommand.CanExecute(null));
		this.viewModel.AddCommand.Execute(null);
		CategoryViewModel? second = this.viewModel.SelectedCategory;
		Assert.Same(first, second);

		first!.Name = "Some category";
		Assert.True(this.viewModel.AddCommand.CanExecute(null));
		this.viewModel.AddCommand.Execute(null);
		CategoryViewModel? third = this.viewModel.SelectedCategory;
		Assert.NotNull(third);
		Assert.NotSame(first, third);
		Assert.Equal(string.Empty, third!.Name);
	}

	[Fact]
	public void AddThenDelete()
	{
		this.viewModel.AddCommand.Execute(null);
		Assert.True(this.viewModel.DeleteCommand.CanExecute(null));
		this.viewModel.DeleteCommand.Execute(null);
		Assert.Null(this.viewModel.SelectedCategory);
		Assert.Empty(this.viewModel.Categories);
	}
}
