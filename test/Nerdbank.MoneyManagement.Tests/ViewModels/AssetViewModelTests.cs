// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;

public class AssetViewModelTests : MoneyTestBase
{
	private AssetViewModel viewModel;

	public AssetViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.viewModel = this.DocumentViewModel.AssetsPanel.NewAsset();
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
	public void TickerSymbol()
	{
		Assert.Equal(string.Empty, this.viewModel.TickerSymbol);
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.TickerSymbol = "foo",
			nameof(this.viewModel.TickerSymbol));
		Assert.Equal("foo", this.viewModel.TickerSymbol);
	}

	[Fact]
	public void Type()
	{
		Assert.Equal(Asset.AssetType.Security, this.viewModel.Type);
		this.viewModel.Type = Asset.AssetType.Currency;
		Assert.Equal(Asset.AssetType.Currency, this.viewModel.Type);
	}

	[Fact]
	public void CurrencySymbol()
	{
		Assert.Null(this.viewModel.CurrencySymbol);
		this.viewModel.CurrencySymbol = "ⓩ";
		Assert.Equal("ⓩ", this.viewModel.CurrencySymbol);
	}

	[Fact]
	public void CurrencyDecimalDigits()
	{
		Assert.Equal(3, this.viewModel.DecimalDigits);
		this.viewModel.DecimalDigits = 8;
		Assert.Equal(8, this.viewModel.DecimalDigits);
		this.viewModel.DecimalDigits = null;
		Assert.Null(this.viewModel.DecimalDigits);
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
	public void Format()
	{
		this.viewModel.Type = Asset.AssetType.Currency;
		var controlled = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		controlled.NumberFormat.CurrencyDecimalDigits = 2;
		controlled.NumberFormat.CurrencySymbol = "$";
		CultureInfo previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = controlled;
			this.viewModel.DecimalDigits = null;
			Assert.Equal("$2,345", this.viewModel.Format(2345.123m));
			this.viewModel.DecimalDigits = 0;
			Assert.Equal("$2,345", this.viewModel.Format(2345.123m));
			this.viewModel.DecimalDigits = 4;
			this.viewModel.CurrencySymbol = "ⓩ";
			Assert.Equal("ⓩ2,345.1230", this.viewModel.Format(2345.123m));
			Assert.Equal("ⓩ2,345.1237", this.viewModel.Format(2345.12369m));

			this.viewModel.Type = Asset.AssetType.Security;
			Assert.Equal("2,345.123", this.viewModel.Format(2345.123m));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void ApplyTo()
	{
		Asset? asset = this.viewModel.Model!;

		this.viewModel.Name = "some name";
		this.viewModel.TickerSymbol = "ticker";
		this.viewModel.Type = Asset.AssetType.Security;
		this.viewModel.CurrencySymbol = "ⓩ";
		this.viewModel.DecimalDigits = 8;

		this.viewModel.ApplyToModel();
		Assert.Equal(this.viewModel.Name, asset.Name);
		Assert.Equal(this.viewModel.TickerSymbol, asset.TickerSymbol);
		Assert.Equal(this.viewModel.Type, asset.Type);
		Assert.Equal(this.viewModel.CurrencySymbol, asset.CurrencySymbol);
		Assert.Equal(this.viewModel.DecimalDigits, asset.DecimalDigits);

		this.viewModel.TickerSymbol = " ";
		this.viewModel.ApplyToModel();
		Assert.Null(asset.TickerSymbol);

		// Test auto-save behavior.
		this.viewModel.Name = "another name";
		Assert.Equal(this.viewModel.Name, asset.Name);

		this.viewModel.Type = Asset.AssetType.Security;
		Assert.Equal(this.viewModel.Type, asset.Type);
	}

	[Fact]
	public void CopyFrom()
	{
		var asset = new Asset
		{
			Id = 5,
			Name = "some name",
			TickerSymbol = "ticker",
			Type = Asset.AssetType.Security,
			CurrencySymbol = "ⓩ",
			DecimalDigits = 8,
		};

		this.viewModel = new(asset, this.DocumentViewModel);

		Assert.Equal(asset.Id, this.viewModel.Id);
		Assert.Equal(asset.Name, this.viewModel.Name);
		Assert.Equal(asset.TickerSymbol, this.viewModel.TickerSymbol);
		Assert.Equal(asset.Type, this.viewModel.Type);
		Assert.Equal(asset.CurrencySymbol, this.viewModel.CurrencySymbol);
		Assert.Equal(asset.DecimalDigits, this.viewModel.DecimalDigits);
	}
}
