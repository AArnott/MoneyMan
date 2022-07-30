// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class ConfigurationPanelViewModelTests : MoneyTestBase
{
	public ConfigurationPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.EnableSqlLogging();
	}

	protected ConfigurationPanelViewModel ViewModel => this.DocumentViewModel.ConfigurationPanel;

	[Fact]
	public void PreferredCurrency()
	{
		Assert.NotNull(this.ViewModel.PreferredAsset);

		this.ViewModel.PreferredAsset = this.DocumentViewModel.AssetsPanel.NewAsset("new");
		this.ReloadViewModel();
		Assert.Equal("new", this.ViewModel.PreferredAsset.Name);
	}

	[Fact]
	public void CommissionAccount()
	{
		Assert.NotNull(this.ViewModel.CommissionCategory);

		this.ViewModel.CommissionCategory = this.DocumentViewModel.CategoriesPanel.NewCategory("My commission");
		this.ReloadViewModel();
		Assert.Equal("My commission", this.ViewModel.CommissionCategory?.Name);
	}
}
