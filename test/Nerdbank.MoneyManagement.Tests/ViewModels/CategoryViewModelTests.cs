// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class CategoryViewModelTests : TestBase
{
	private CategoryViewModel viewModel = new CategoryViewModel();

	public CategoryViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void Name()
	{
		Assert.Equal(string.Empty, this.viewModel.Name);
		this.viewModel.Name = "changed";
		Assert.Equal("changed", this.viewModel.Name);

		Assert.Throws<ArgumentNullException>(() => this.viewModel.Name = null!);
	}

	[Fact]
	public void Name_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Name = "foo",
			nameof(this.viewModel.Name));
	}

	[Fact]
	public void ApplyTo()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));

		var category = new Category();

		this.viewModel.Name = "some name";

		this.viewModel.ApplyTo(category);
		Assert.Equal(this.viewModel.Name, category.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, category.Name);
	}

	[Fact]
	public void ApplyToThrowsOnEntityMismatch()
	{
		this.viewModel.CopyFrom(new Category { Id = 2, Name = "Groceries" });
		Assert.Throws<ArgumentException>(() => this.viewModel.ApplyTo(new Category { Id = 4 }));
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));

		var category = new Category
		{
			Id = 5,
			Name = "some name",
		};

		this.viewModel.CopyFrom(category);

		Assert.Equal(category.Id, this.viewModel.Id);
		Assert.Equal(category.Name, this.viewModel.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, category.Name);
	}

	[Fact]
	public void Ctor_From_Entity()
	{
		var category = new Category
		{
			Id = 5,
			Name = "some name",
		};

		this.viewModel = new CategoryViewModel(category);

		Assert.Equal(category.Id, this.viewModel.Id);
		Assert.Equal(category.Name, this.viewModel.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, category.Name);
	}
}
