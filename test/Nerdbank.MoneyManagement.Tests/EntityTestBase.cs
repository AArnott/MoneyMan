// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class EntityTestBase : MoneyTestBase
{
	public EntityTestBase(ITestOutputHelper logger)
		: base(logger)
	{
	}

	public T SaveAndReload<T>(T model)
		where T : notnull, ModelBase, new()
	{
		this.Money.Insert(model);
		return this.Money.Get<T>(model.Id);
	}
}
