// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

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
		const int parentId = 5;

		var p = new Category
		{
			Name = name,
			ParentCategoryId = parentId,
		};

		Assert.Equal(name, p.Name);
		Assert.Equal(parentId, p.ParentCategoryId);

		Category? p2 = this.SaveAndReload(p);

		Assert.Equal(name, p2.Name);
		Assert.Equal(parentId, p2.ParentCategoryId);
	}
}
