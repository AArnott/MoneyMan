// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.IO;
using Nerdbank.MoneyManagement;
using Xunit.Abstractions;

public class EntityTestBase : MoneyTestBase
{
	public EntityTestBase(ITestOutputHelper logger)
		: base(logger)
	{
	}

	public T SaveAndReload<T>(T obj)
		where T : notnull, new()
	{
		int pk = this.Money.Insert(obj);
		return this.Money.Get<T>(pk);
	}
}
