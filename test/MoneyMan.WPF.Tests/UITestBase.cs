// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows.Threading;

public abstract class UITestBase : MoneyTestBase
{
	public UITestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.TraceListener = new XunitTraceListener(logger);

		this.Window = new MainWindow();

		PresentationTraceSources.Refresh();
		PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
		PresentationTraceSources.DataBindingSource.Listeners.Add(this.TraceListener);

		this.Window.ViewModel.ReplaceViewModel(this.DocumentViewModel);

		// We have to make the window "visible" in order for data binding to occur so we can log errors
		// from data binding, which is a major purpose of these UI tests.
		// But we don't want to flash the window for each test, so position it so it's guaranteed to be off screen.
		if (!VisibleWindow)
		{
			this.Window.Top = -1000;
		}

		this.Window.Show();
	}

	protected MainWindow Window { get; }

	protected XunitTraceListener TraceListener { get; }

	private static bool VisibleWindow => Debugger.IsAttached;

	public override async Task DisposeAsync()
	{
		// We must wait for the Render step in WPF to ensure that data binding errors are reported.
		await Dispatcher.Yield(DispatcherPriority.Render);

		this.Logger.WriteLine("Data-binding trace switch: {0}, {1}", PresentationTraceSources.DataBindingSource.Switch.Level, PresentationTraceSources.DataBindingSource.Listeners.Count);
		this.Window.Close();
		PresentationTraceSources.DataBindingSource.Flush();
		PresentationTraceSources.DataBindingSource.Listeners.Remove(this.TraceListener);

		// Fail the test if any failures were traced.
		Assert.False(this.TraceListener.HasLoggedErrors, "Errors have been logged.");

		await base.DisposeAsync();
	}
}
