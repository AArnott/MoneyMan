// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class AssetViewModelTests : MoneyTestBase
{
	private AssetViewModel viewModel;

	public AssetViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.viewModel = this.DocumentViewModel.AssetsPanel.NewAsset();
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
	public void Type()
	{
		Assert.Equal(Asset.AssetType.Security, this.viewModel.Type);
		this.viewModel.Type = Asset.AssetType.Currency;
		Assert.Equal(Asset.AssetType.Currency, this.viewModel.Type);
	}

	[Fact]
	public void Type_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Type = Asset.AssetType.Currency,
			nameof(this.viewModel.Type));
	}

	[Theory, PairwiseData]
	public void TypeIsReadOnly_TrueAfterBankingAccountBasedOnIt(bool delete)
	{
		this.viewModel.Name = "some asset";
		Assert.False(this.viewModel.TypeIsReadOnly);
		this.viewModel.Type = Asset.AssetType.Currency;
		Assert.False(this.viewModel.TypeIsReadOnly);

		BankingAccountViewModel testAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount("testAccount");
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => testAccount.CurrencyAsset = this.viewModel,
			nameof(this.viewModel.TypeIsReadOnly));

		// Now that an account is based on this currency, it should not be permitted to change type.
		Assert.True(this.viewModel.TypeIsReadOnly);
		Assert.Throws<InvalidOperationException>(() => this.viewModel.Type = Asset.AssetType.Security);
		Assert.Equal(Asset.AssetType.Currency, this.viewModel.Type);

		// Removing the account's use of this asset should enable changing the asset type again.
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			delegate
			{
				if (delete)
				{
					this.DocumentViewModel.AccountsPanel.DeleteAccount(testAccount);
				}
				else
				{
					testAccount!.CurrencyAsset = null;
				}
			},
			nameof(this.viewModel.TypeIsReadOnly));
		Assert.False(this.viewModel.TypeIsReadOnly);
		this.viewModel.Type = Asset.AssetType.Security;
		Assert.Equal(Asset.AssetType.Security, this.viewModel.Type);
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

		Asset? asset = this.viewModel.Model!;

		this.viewModel.Name = "some name";
		this.viewModel.Type = Asset.AssetType.Security;

		this.viewModel.ApplyTo(asset);
		Assert.Equal(this.viewModel.Name, asset.Name);
		Assert.Equal(this.viewModel.Type, asset.Type);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, asset.Name);

		this.viewModel.Type = Asset.AssetType.Security;
		Assert.Equal(this.viewModel.Type, asset.Type);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.CopyFrom(null!));

		var asset = new Asset
		{
			Id = 5,
			Name = "some name",
			Type = Asset.AssetType.Security,
		};

		this.viewModel.CopyFrom(asset);

		Assert.Equal(asset.Id, this.viewModel.Id);
		Assert.Equal(asset.Name, this.viewModel.Name);
		Assert.Equal(asset.Type, this.viewModel.Type);
	}
}
