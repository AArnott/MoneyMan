// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class AssetViewModelTests : MoneyTestBase
{
	private AssetViewModel viewModel = new AssetViewModel();

	public AssetViewModelTests(ITestOutputHelper logger)
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
	public void ApplyTo()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));

		var asset = new Asset();

		this.viewModel.Name = "some name";

		this.viewModel.ApplyTo(asset);
		Assert.Equal(this.viewModel.Name, asset.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, asset.Name);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));

		var asset = new Asset
		{
			Id = 5,
			Name = "some name",
		};

		this.viewModel.CopyFrom(asset);

		Assert.Equal(asset.Id, this.viewModel.Id);
		Assert.Equal(asset.Name, this.viewModel.Name);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, asset.Name);
	}
}
