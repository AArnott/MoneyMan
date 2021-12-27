// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class CategoryFacts : EntityTestBase
{
	public CategoryFacts(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		const string name = "Some name";
		const string childName = "child";

		var p = new Category
		{
			Name = name,
		};

		Assert.Equal(name, p.Name);
		Assert.Null(p.ParentCategoryId);

		Category? p2 = this.SaveAndReload(p);

		Assert.Equal(name, p2.Name);
		Assert.Null(p.ParentCategoryId);

		var pChild = new Category
		{
			Name = childName,
			ParentCategoryId = p.Id,
		};

		Category? pChild2 = this.SaveAndReload(pChild);
		Assert.Equal(p.Id, pChild2.ParentCategoryId);
	}
}
