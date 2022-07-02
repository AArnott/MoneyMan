// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class CategoryViewModelTests : MoneyTestBase
{
	private CategoryAccountViewModel viewModel;

	public CategoryViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.viewModel = new CategoryAccountViewModel(null, this.DocumentViewModel);
		this.EnableSqlLogging();
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
	public void Name_Validation()
	{
		// Assert the assumed default value.
		Assert.Equal(string.Empty, this.viewModel.Name);

		this.Logger.WriteLine(this.viewModel.Error);
		Assert.NotEqual(string.Empty, this.viewModel[nameof(this.viewModel.Name)]);
		Assert.Equal(this.viewModel[nameof(this.viewModel.Name)], this.viewModel.Error);

		this.viewModel.Name = "a";
		Assert.Equal(string.Empty, this.viewModel[nameof(this.viewModel.Name)]);
	}

	[Fact]
	public void AutoSaveDoesNotFunctionOnInvalidViewModel()
	{
		this.viewModel = this.DocumentViewModel.CategoriesPanel.NewCategory();
		this.viewModel.Name = "Hi";
		Assert.Equal("Hi", this.viewModel.Model.Name);
		this.viewModel.Name = string.Empty;

		// Assert that the model does *not* immediately pick up on the invalid state of the view model.
		Assert.Equal("Hi", this.viewModel.Model.Name);
	}

	[Fact]
	public void ApplyTo_ThrowsFromInvalidViewModel()
	{
		this.viewModel = this.DocumentViewModel.CategoriesPanel.NewCategory();
		Assert.Throws<InvalidOperationException>(() => this.viewModel.ApplyToModel());
		this.viewModel.Name = "some name";
		this.viewModel.ApplyToModel();
		Assert.Equal(this.viewModel.Name, this.viewModel.Model.Name);
	}

	[Fact]
	public void TransferTargetName()
	{
		this.viewModel.Name = "tt-test";
		Assert.Equal(this.viewModel.Name, this.viewModel.TransferTargetName);
	}

	[Fact]
	public void TransferTargetName_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(this.viewModel, () => this.viewModel.Name = "other", nameof(this.viewModel.Name), nameof(this.viewModel.TransferTargetName));
	}

	[Fact]
	public void ApplyTo()
	{
		this.viewModel = new CategoryAccountViewModel(null, this.DocumentViewModel);

		this.viewModel.Name = "some name";

		this.viewModel.ApplyToModel();
		Assert.Equal(this.viewModel.Name, this.viewModel.Model.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, this.viewModel.Model.Name);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));

		Account category = new()
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
	public void Ctor_From_Volatile_Entity()
	{
		Account category = new()
		{
			Name = "some name",
			Type = Account.AccountType.Category,
		};

		this.viewModel = new CategoryAccountViewModel(category, this.DocumentViewModel);

		Assert.Equal(category.Id, this.viewModel.Id);
		Assert.Equal(category.Name, this.viewModel.Name);

		// Test auto-save behavior.
		Assert.Equal(0, this.viewModel.Id);
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, this.viewModel.Name);
		Assert.Equal(category.Id, this.viewModel.Id);
		Assert.NotEqual(0, category.Id);

		Account fromDb = this.Money.Categories.First(cat => cat.Id == category.Id);
		Assert.Equal(category.Name, fromDb.Name);
		Assert.Single(this.Money.Categories, cat => cat.Name != DefaultCommissionCategoryName);
	}

	[Fact]
	public void Ctor_From_Db_Entity()
	{
		Account category = new()
		{
			Name = "some name",
			Type = Account.AccountType.Category,
		};
		this.Money.Insert(category);

		this.viewModel = new CategoryAccountViewModel(category, this.DocumentViewModel);

		Assert.Equal(category.Id, this.viewModel.Id);
		Assert.Equal(category.Name, this.viewModel.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, category.Name);

		Account fromDb = this.Money.Categories.First(cat => cat.Id == category.Id);
		Assert.Equal(category.Name, fromDb.Name);
		Assert.Single(this.Money.Categories, cat => cat.Name != DefaultCommissionCategoryName);
	}
}
