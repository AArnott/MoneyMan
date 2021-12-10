// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

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
		AssetViewModel asset = Assert.Single(this.ViewModel.Assets);
		Assert.Equal("USD", asset.Name);
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
		AssetViewModel? newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		Assert.Equal(string.Empty, newAsset!.Name);

		newAsset.Name = "cat";
		Assert.Contains(this.Money.Assets, a => a.Name == newAsset.Name);
	}

	[Fact]
	public async Task NewAsset_TypeDefault()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		Assert.Equal(Asset.AssetType.Security, this.ViewModel.SelectedAsset?.Type);
	}

	[Fact]
	public async Task AddCommand()
	{
		Assert.True(this.ViewModel.AddCommand.CanExecute(null));

		await TestUtilities.AssertRaisesAsync(
			h => this.ViewModel.AddingNewAsset += h,
			h => this.ViewModel.AddingNewAsset -= h,
			() => this.ViewModel.AddCommand.ExecuteAsync());
		AssetViewModel? newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		Assert.Equal(string.Empty, newAsset!.Name);

		newAsset.Name = "asset";
		Assert.Contains(this.Money.Assets, a => a.Name == newAsset.Name);
	}

	[Fact]
	public async Task AddCommand_Twice()
	{
		int originalCount = this.Money.Assets.Count();

		await this.ViewModel.AddCommand.ExecuteAsync();
		AssetViewModel? newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		newAsset!.Name = "cat";

		await this.ViewModel.AddCommand.ExecuteAsync();
		newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		newAsset!.Name = "dog";

		Assert.Equal(originalCount + 2, this.Money.Assets.Count());
	}

	[Fact]
	public async Task DeleteCommand()
	{
		Assert.False(this.ViewModel.DeleteCommand.CanExecute());
		AssetViewModel asset = this.ViewModel.NewAsset(SomeAssetName);

		this.ViewModel.SelectedAsset = asset;
		await this.ViewModel.DeleteCommand.ExecuteAsync();

		Assert.Null(this.ViewModel.SelectedAsset);
		Assert.DoesNotContain(asset, this.ViewModel.Assets);

		this.ReloadViewModel();
		Assert.DoesNotContain(this.ViewModel.Assets, a => a.Id == asset.Id);
	}

	[Fact]
	public void DeleteCommand_CanExecuteOnSelectionChanged()
	{
		AssetViewModel asset = this.ViewModel.NewAsset(SomeAssetName);
		this.ViewModel.SelectedAsset = null;

		Assert.False(this.ViewModel.DeleteCommand.CanExecute(null));
		TestUtilities.AssertCommandCanExecuteChanged(
			this.ViewModel.DeleteCommand,
			() => this.ViewModel.SelectedAsset = asset);
		Assert.True(this.ViewModel.DeleteCommand.CanExecute(null));
	}

	[Fact]
	public async Task DeleteAsset_Undo()
	{
		AssetViewModel asset = this.ViewModel.NewAsset(SomeAssetName);
		this.ViewModel.SelectedAsset = asset;
		await this.ViewModel.DeleteCommand.ExecuteAsync();
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Accounts;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();

		Assert.Equal(DocumentViewModel.SelectableViews.Assets, this.DocumentViewModel.SelectedViewIndex);
		Assert.Equal(asset.Id, this.DocumentViewModel.AssetsPanel.SelectedAsset?.Id);
	}

	[Fact]
	public async Task AddAddThenDelete()
	{
		await this.ViewModel.AddCommand.ExecuteAsync();
		Assert.NotNull(this.ViewModel.SelectedAsset);
		AssetViewModel asset = this.ViewModel.SelectedAsset!;
		int count = this.ViewModel.Assets.Count;

		await this.ViewModel.AddCommand.ExecuteAsync();
		Assert.Same(asset, this.ViewModel.SelectedAsset);
		Assert.Equal(count, this.ViewModel.Assets.Count);

		await this.ViewModel.DeleteCommand.ExecuteAsync();
		Assert.Null(this.ViewModel.SelectedAsset);

		await this.ViewModel.AddCommand.ExecuteAsync();
		Assert.NotSame(asset, this.ViewModel.SelectedAsset);
		Assert.Equal(count, this.ViewModel.Assets.Count);
	}

	[Fact]
	public void Persistence()
	{
		AssetViewModel asset1 = this.ViewModel.NewAsset(SomeAssetName);
		this.ReloadViewModel();
		Assert.Contains(this.ViewModel.Assets, a => a.Id == asset1.Id);
	}
}
