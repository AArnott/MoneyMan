// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Windows;
	using Squirrel;

	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		[STAThread]
		public static void Main()
		{
			using (var mgr = new UpdateManager(null))
			{
				static void CreateShortcuts(UpdateManager mgr)
				{
					mgr.CreateShortcutsForExecutable(
						Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location) + ".exe",
						ShortcutLocation.StartMenu | ShortcutLocation.Desktop,
						updateOnly: !Environment.CommandLine.Contains("squirrel-install"),
						programArguments: null,
						icon: null);
				}

				// Note, in most of these scenarios, the app exits after this method completes!
				SquirrelAwareApp.HandleEvents(
				  onInitialInstall: v => CreateShortcuts(mgr),
				  onAppUpdate: v => CreateShortcuts(mgr),
				  onAppUninstall: v => mgr.RemoveShortcutForThisExe());
			}

			App app = new();
			app.InitializeComponent();
			app.Run();
		}
	}
}
