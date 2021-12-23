// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class AssetPriceViewModelTests : MoneyTestBase
{
	private readonly AssetViewModel msft;

	public AssetPriceViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.msft = this.DocumentViewModel.AssetsPanel.NewAsset("MSFT");
	}

	[Fact]
	public void AssetPriceNotValidWithoutPrice()
	{
		AssetPriceViewModel viewModel = new AssetPriceViewModel(this.DocumentViewModel, new AssetPrice())
		{
			Asset = this.msft,
			When = DateTime.Now,
			ReferenceAsset = this.DocumentViewModel.GetAsset(this.Money.PreferredAssetId),
		};
		Assert.False(string.IsNullOrEmpty(viewModel.Error));
		viewModel.Price = 10;
		Assert.Equal(string.Empty, viewModel.Error);
	}

	[Fact]
	public void ApplyTo()
	{
		AssetPriceViewModel viewModel = new AssetPriceViewModel(this.DocumentViewModel, new AssetPrice())
		{
			Asset = this.msft,
			When = DateTime.Now,
			ReferenceAsset = this.DocumentViewModel.GetAsset(this.Money.PreferredAssetId),
			Price = 10,
		};

		this.ReloadViewModel();
		AssetPrice priceModel = this.Money.AssetPrices.First(ap => ap.Id == viewModel.Id);
		Assert.Equal(viewModel.Asset.Id, priceModel.AssetId);
		Assert.Equal(viewModel.When, priceModel.When);
		Assert.Equal(viewModel.ReferenceAsset.Id, priceModel.ReferenceAssetId);
		Assert.Equal(viewModel.Price, priceModel.PriceInReferenceAsset);
	}

	[Fact]
	public void CopyFrom()
	{
		var model = new AssetPrice
		{
			AssetId = this.msft.Id,
			When = DateTime.Now,
			ReferenceAssetId = this.Money.PreferredAssetId,
			PriceInReferenceAsset = 10,
		};

		AssetPriceViewModel viewModel = new(this.DocumentViewModel, model);
		Assert.Equal(model.AssetId, viewModel.Asset?.Id);
		Assert.Equal(model.When, viewModel.When);
		Assert.Equal(model.ReferenceAssetId, viewModel.ReferenceAsset?.Id);
		Assert.Equal(model.PriceInReferenceAsset, viewModel.Price);
	}
}
