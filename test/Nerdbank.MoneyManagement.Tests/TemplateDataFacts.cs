// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class TemplateDataFacts : MoneyTestBase
{
	public TemplateDataFacts(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void Inject_AddsCategories()
	{
		TemplateData.InjectTemplateData(this.Money);
		Assert.Contains(this.Money.Categories, cat => cat.Name == "Groceries");
	}
}
