// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Windows;
	using System.Windows.Controls;
	using Squirrel;

	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		[STAThread]
		public static void Main()
		{
			// Note, in most of these scenarios, the app exits after this method completes!
			using (UpdateManager mgr = CreateUpdateManager())
			{
				SquirrelAwareApp.HandleEvents(
				  onInitialInstall: v => mgr.CreateShortcutForThisExe(),
				  onAppUpdate: v => mgr.CreateShortcutForThisExe(),
				  onAppUninstall: v => mgr.RemoveShortcutForThisExe());
			}

			App app = new();
			app.InitializeComponent();
			app.Run();
		}

		internal static UpdateManager CreateUpdateManager()
		{
			string channel = ThisAssembly.IsPrerelease ? "prerelease" : "release";
			string subchannel = RuntimeInformation.ProcessArchitecture switch
			{
				Architecture.Arm64 => "win-arm64",
				Architecture.X64 => "win-x64",
				Architecture.X86 => "win-x86",
				_ => throw new NotSupportedException("Unrecognized process architecture."),
			};

			// The application name must exactly match the package ID.
			string applicationName = $"Nerdbank_MoneyMan_{subchannel}";
			return new UpdateManager(
				  urlOrPath: $"https://moneymanreleases.blob.core.windows.net/releases/{channel}/{subchannel}/",
				  applicationName);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			EventManager.RegisterClassHandler(
				typeof(DataGrid),
				DataGrid.PreviewMouseLeftButtonDownEvent,
				new RoutedEventHandler(WpfHelpers.DataGridPreviewMouseLeftButtonDownEvent));
		}
	}
}
