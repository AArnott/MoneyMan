// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.Models;
using Xunit;
using Xunit.Abstractions;

public class AssetFacts : EntityTestBase
{
	public AssetFacts(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		const string name = "Some name";

		var p = new Asset
		{
			Name = name,
		};

		Assert.Equal(name, p.Name);

		Asset? p2 = this.SaveAndReload(p);

		Assert.Equal(name, p2.Name);
	}
}
