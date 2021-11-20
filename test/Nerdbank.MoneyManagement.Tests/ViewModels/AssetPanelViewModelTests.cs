// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class AssetPanelViewModelTests : MoneyTestBase
{
	private const string SomeAssetName = "some asset";

	public AssetPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	private AssetsPanelViewModel ViewModel => this.DocumentViewModel.AssetsPanel;

	[Fact]
	public void InitialState()
	{
		Assert.Empty(this.ViewModel.Assets);
		Assert.Null(this.ViewModel.SelectedAsset);
	}

	[Fact]
	public void NewAsset_WithName()
	{
		AssetViewModel asset = this.ViewModel.NewAsset(SomeAssetName);
		Assert.Contains(asset, this.ViewModel.Assets);
		Assert.Equal(SomeAssetName, asset.Name);
		Assert.Contains(asset, this.ViewModel.Assets);
	}

	[Fact]
	public void NewAsset_WithoutName()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		TestUtilities.AssertRaises(
			h => this.ViewModel.AddingNewAsset += h,
			h => this.ViewModel.AddingNewAsset -= h,
			() => this.ViewModel.NewAsset());
		AssetViewModel newAsset = Assert.Single(this.ViewModel.Assets);
		Assert.Same(newAsset, this.ViewModel.SelectedAsset);
		Assert.Equal(string.Empty, newAsset.Name);

		newAsset.Name = "cat";
		Assert.Equal("cat", Assert.Single(this.Money.Assets).Name);
	}

	[Fact]
	public void AddCommand()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		TestUtilities.AssertRaises(
			h => this.ViewModel.AddingNewAsset += h,
			h => this.ViewModel.AddingNewAsset -= h,
			() => this.ViewModel.AddCommand.Execute(null));
		AssetViewModel newAsset = Assert.Single(this.ViewModel.Assets);
		Assert.Same(newAsset, this.ViewModel.SelectedAsset);
		Assert.Equal(string.Empty, newAsset.Name);

		newAsset.Name = "asset";
		Assert.Equal("asset", Assert.Single(this.Money.Assets).Name);
	}

	[Fact]
	public void AddCommand_Twice()
	{
		this.ViewModel.AddCommand.Execute(null);
		AssetViewModel? newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		newAsset!.Name = "cat";

		this.ViewModel.AddCommand.Execute(null);
		newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		newAsset!.Name = "dog";

		Assert.Equal(2, this.Money.Assets.Count());
	}

	[Fact]
	public void Persistence()
	{
		AssetViewModel asset1 = this.ViewModel.NewAsset(SomeAssetName);
		this.ReloadViewModel();
		Assert.Contains(this.ViewModel.Assets, a => a.Id == asset1.Id);
	}
}
