// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class AssetFacts : EntityTestBase
{
	public AssetFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.EnableSqlLogging();
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		const string name = "Some name";

		var p = new Asset
		{
			Name = name,
			Type = Asset.AssetType.Security,
			CurrencySymbol = "ⓩ",
			CurrencyDecimalDigits = 8,
		};

		Assert.Equal(name, p.Name);
		Assert.Equal(Asset.AssetType.Security, p.Type);
		Assert.Equal("ⓩ", p.CurrencySymbol);
		Assert.Equal(8, p.CurrencyDecimalDigits);

		Asset? p2 = this.SaveAndReload(p);

		Assert.Equal(name, p2.Name);
		Assert.Equal(p.Type, p2.Type);
		Assert.Equal(p.CurrencySymbol, p2.CurrencySymbol);
		Assert.Equal(p.CurrencyDecimalDigits, p2.CurrencyDecimalDigits);
	}
}
