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
		Assert.Equal("USD", asset.TickerSymbol);
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
		Assert.True(this.ViewModel.AddAssetCommand.CanExecute(null));

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
		await this.ViewModel.AddAssetCommand.ExecuteAsync();
		Assert.Equal(Asset.AssetType.Security, this.ViewModel.SelectedAsset?.Type);
	}

	[Fact]
	public async Task AddCommand()
	{
		Assert.True(this.ViewModel.AddAssetCommand.CanExecute(null));

		await TestUtilities.AssertRaisesAsync(
			h => this.ViewModel.AddingNewAsset += h,
			h => this.ViewModel.AddingNewAsset -= h,
			() => this.ViewModel.AddAssetCommand.ExecuteAsync());
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

		await this.ViewModel.AddAssetCommand.ExecuteAsync();
		AssetViewModel? newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		newAsset!.Name = "cat";

		await this.ViewModel.AddAssetCommand.ExecuteAsync();
		newAsset = this.ViewModel.SelectedAsset;
		Assert.NotNull(newAsset);
		newAsset!.Name = "dog";

		Assert.Equal(originalCount + 2, this.Money.Assets.Count());
	}

	[Fact]
	public async Task DeleteCommand()
	{
		Assert.False(this.ViewModel.DeleteAssetCommand.CanExecute());
		AssetViewModel asset = this.ViewModel.NewAsset(SomeAssetName);

		this.ViewModel.SelectedAsset = asset;
		await this.ViewModel.DeleteAssetCommand.ExecuteAsync();

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

		Assert.False(this.ViewModel.DeleteAssetCommand.CanExecute(null));
		TestUtilities.AssertCommandCanExecuteChanged(
			this.ViewModel.DeleteAssetCommand,
			() => this.ViewModel.SelectedAsset = asset);
		Assert.True(this.ViewModel.DeleteAssetCommand.CanExecute(null));
	}

	[Fact]
	public async Task DeleteAsset_Undo()
	{
		AssetViewModel asset = this.ViewModel.NewAsset(SomeAssetName);
		this.ViewModel.SelectedAsset = asset;
		await this.ViewModel.DeleteAssetCommand.ExecuteAsync();
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Accounts;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();

		Assert.Equal(DocumentViewModel.SelectableViews.Assets, this.DocumentViewModel.SelectedViewIndex);
		Assert.Equal(asset.Id, this.DocumentViewModel.AssetsPanel.SelectedAsset?.Id);
	}

	[Fact]
	public async Task AddAddThenDelete()
	{
		await this.ViewModel.AddAssetCommand.ExecuteAsync();
		Assert.NotNull(this.ViewModel.SelectedAsset);
		AssetViewModel asset = this.ViewModel.SelectedAsset!;
		int count = this.ViewModel.Assets.Count;

		await this.ViewModel.AddAssetCommand.ExecuteAsync();
		Assert.Same(asset, this.ViewModel.SelectedAsset);
		Assert.Equal(count, this.ViewModel.Assets.Count);

		await this.ViewModel.DeleteAssetCommand.ExecuteAsync();
		Assert.Null(this.ViewModel.SelectedAsset);

		await this.ViewModel.AddAssetCommand.ExecuteAsync();
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

	[Fact]
	public void IsPricesGridVisible()
	{
		AssetViewModel someAsset = this.ViewModel.NewAsset(SomeAssetName);
		this.ViewModel.SelectedAsset = someAsset;
		Assert.True(this.ViewModel.IsPricesGridVisible);

		this.ViewModel.SelectedAsset = this.DocumentViewModel.DefaultCurrency;
		Assert.False(this.ViewModel.IsPricesGridVisible);
		this.ViewModel.SelectedAsset = null;
		Assert.False(this.ViewModel.IsPricesGridVisible);
	}

	[Fact]
	public void SelectedAssetPrices()
	{
		AssetViewModel someAsset = this.ViewModel.NewAsset(SomeAssetName);
		Assert.Single(this.ViewModel.AssetPrices);
		this.Money.Insert(new AssetPrice
		{
			AssetId = someAsset.Id!.Value,
			When = DateTime.Now,
			ReferenceAssetId = this.Money.PreferredAssetId,
			PriceInReferenceAsset = 10,
		});
		this.ViewModel.SelectedAsset = null;
		this.ViewModel.SelectedAsset = someAsset;
		AssetPriceViewModel price = this.ViewModel.AssetPrices[1];
		Assert.Equal(10, price.Price);

		this.ViewModel.SelectedAsset = null;
		Assert.Empty(this.ViewModel.AssetPrices);
	}

	[Fact]
	public void PlaceholderPrice()
	{
		AssetViewModel someAsset = this.ViewModel.NewAsset(SomeAssetName);
		AssetPriceViewModel placeholder = this.ViewModel.AssetPrices[0];
		Assert.False(placeholder.IsPersisted);
		Assert.Same(someAsset, placeholder.Asset);
		Assert.Equal(DateTime.Today, placeholder.When);
		Assert.Same(this.DocumentViewModel.DefaultCurrency, placeholder.ReferenceAsset);
	}

	[Fact]
	public async Task AddAssetPrice()
	{
		await this.ViewModel.AddAssetCommand.ExecuteAsync();

		// Prices cannot exist nor be added before the selected asset has been persisted.
		Assert.False(this.ViewModel.SelectedAsset?.IsPersisted);
		Assert.Empty(this.ViewModel.AssetPrices);
		this.ViewModel.SelectedAsset!.Name = SomeAssetName;
		Assert.NotEmpty(this.ViewModel.AssetPrices);

		AssetPriceViewModel placeholder = this.ViewModel.AssetPrices[0];
		placeholder.Price = 10;
		Assert.Equal(2, this.ViewModel.AssetPrices.Count);
		Assert.Same(placeholder, this.ViewModel.AssetPrices[1]);
		Assert.False(this.ViewModel.AssetPrices[0].IsPersisted);
	}

	[Fact]
	public async Task DeletePrice()
	{
		this.ViewModel.NewAsset(SomeAssetName);
		this.ViewModel.SelectedAssetPrice = this.ViewModel.AssetPrices[0];
		this.ViewModel.SelectedAssetPrice.Price = 10;
		Assert.Equal(2, this.ViewModel.AssetPrices.Count);

		await this.ViewModel.DeletePriceCommand.ExecuteAsync();
		Assert.Equal(1, this.ViewModel.AssetPrices.Count);
		Assert.Null(this.ViewModel.SelectedAssetPrice);

		Assert.False(this.ViewModel.AssetPrices[0].IsPersisted);
		Assert.False(this.ViewModel.DeletePriceCommand.CanExecute());
	}

	[Fact]
	public async Task DeletePrice_ThenUndo()
	{
		this.ViewModel.NewAsset(SomeAssetName);
		this.ViewModel.SelectedAssetPrice = this.ViewModel.AssetPrices[0];
		this.ViewModel.SelectedAssetPrice.Price = 10;
		Assert.Equal(2, this.ViewModel.AssetPrices.Count);

		await this.ViewModel.DeletePriceCommand.ExecuteAsync();
		Assert.Equal(1, this.ViewModel.AssetPrices.Count);

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		Assert.Equal(2, this.ViewModel.AssetPrices.Count);
	}

	[Fact]
	public async Task DeletePrices()
	{
		List<AssetPriceViewModel> selected = new();
		this.ViewModel.SelectedAssetPrices = selected;
		this.ViewModel.NewAsset(SomeAssetName);

		selected.Add(this.ViewModel.AssetPrices[0]);
		this.ViewModel.SelectedAssetPrice = this.ViewModel.AssetPrices[0];
		this.ViewModel.SelectedAssetPrice.Price = 10;

		selected.Add(this.ViewModel.AssetPrices[0]);
		this.ViewModel.AssetPrices[0].When = DateTime.Today.AddDays(-1);
		this.ViewModel.AssetPrices[0].Price = 15;

		this.ViewModel.AssetPrices[0].When = DateTime.Today.AddDays(-2);
		this.ViewModel.AssetPrices[0].Price = 20;

		Assert.Equal(4, this.ViewModel.AssetPrices.Count);
		await this.ViewModel.DeletePriceCommand.ExecuteAsync();
		Assert.Null(this.ViewModel.SelectedAssetPrice);
		selected.Clear();

		this.AssertNowAndAfterReload(delegate
		{
			this.ViewModel.SelectedAsset = this.ViewModel.FindAsset(SomeAssetName);
			Assert.Equal(2, this.ViewModel.AssetPrices.Count);
			Assert.False(this.ViewModel.AssetPrices[0].IsPersisted);
			Assert.True(this.ViewModel.AssetPrices[1].IsPersisted);
			Assert.Equal(20, this.ViewModel.AssetPrices[1].Price);
			Assert.False(this.ViewModel.DeletePriceCommand.CanExecute());
		});
	}
}
