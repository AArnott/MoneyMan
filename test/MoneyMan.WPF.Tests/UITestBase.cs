// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using MoneyMan;
using Xunit;
using Xunit.Abstractions;

public abstract class UITestBase : MoneyTestBase
{
	public UITestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.TraceListener = new XunitTraceListener(logger);

		this.Window = new MainWindow
		{
			ReopenLastFile = false,
		};

		PresentationTraceSources.Refresh();
		PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
		PresentationTraceSources.DataBindingSource.Listeners.Add(this.TraceListener);

		this.Window.ViewModel.ReplaceViewModel(this.DocumentViewModel);
		if (VisibleWindow)
		{
			this.Window.Show();
		}
	}

	protected MainWindow Window { get; }

	protected XunitTraceListener TraceListener { get; }

	private static bool VisibleWindow => Debugger.IsAttached;

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.Logger.WriteLine("Data-binding trace switch: {0}, {1}", PresentationTraceSources.DataBindingSource.Switch.Level, PresentationTraceSources.DataBindingSource.Listeners.Count);
			this.Window.Close();
			PresentationTraceSources.DataBindingSource.Flush();
			PresentationTraceSources.DataBindingSource.Listeners.Remove(this.TraceListener);

			// Fail the test if any failures were traced.
			Assert.False(this.TraceListener.HasLoggedErrors, "Errors have been logged.");
		}

		base.Dispose(disposing);
	}
}
