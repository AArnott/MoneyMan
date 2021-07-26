// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using Xunit.Abstractions;

public abstract class TestBase
{
	public TestBase(ITestOutputHelper logger)
	{
		this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected ITestOutputHelper Logger { get; }
}
