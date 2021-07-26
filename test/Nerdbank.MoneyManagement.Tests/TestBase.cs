// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using Xunit.Abstractions;

public abstract class TestBase : IDisposable
{
	private bool disposed;

	public TestBase(ITestOutputHelper logger)
	{
		this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected ITestOutputHelper Logger { get; }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!this.disposed)
		{
			if (disposing)
			{
			}

			this.disposed = true;
		}
	}
}
