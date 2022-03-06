// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public abstract class TestBase : IDisposable, IAsyncLifetime
{
	protected static readonly TimeSpan ExpectedTimeout = TimeSpan.FromMilliseconds(250);

	protected static readonly TimeSpan UnexpectedTimeout = Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(20);

	private bool disposed;
	private List<string> filesToDelete = new();

	public TestBase(ITestOutputHelper logger)
	{
		this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected ITestOutputHelper Logger { get; }

	protected CancellationToken TimeoutToken { get; } = new CancellationTokenSource(UnexpectedTimeout).Token;

	public virtual Task InitializeAsync() => Task.CompletedTask;

	public virtual Task DisposeAsync() => Task.CompletedTask;

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected string GenerateTemporaryFileName()
	{
		string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		this.filesToDelete.Add(path);
		return path;
	}

	protected void DeleteTemporaryFileOnDispose(string path) => this.filesToDelete.Add(path);

	protected virtual void Dispose(bool disposing)
	{
		if (!this.disposed)
		{
			if (disposing)
			{
				foreach (string file in this.filesToDelete)
				{
					File.Delete(file);
				}
			}

			this.disposed = true;
		}
	}
}
