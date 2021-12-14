// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class ConfigurationPanelViewModelTests : MoneyTestBase
{
	private ConfigurationPanelViewModel viewModel;

	public ConfigurationPanelViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.viewModel = this.DocumentViewModel.ConfigurationPanel;
	}

	[Fact]
	public void PreferredCurrency()
	{
		Assert.NotNull(this.viewModel.PreferredAsset);
	}
}
