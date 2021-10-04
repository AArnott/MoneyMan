// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Runtime.InteropServices;
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
			using (UpdateManager mgr = CreateUpdateManager())
			{
				SquirrelAwareApp.HandleEvents(
				  onInitialInstall: v => CreateShortcuts(mgr),
				  onAppUpdate: v => CreateShortcuts(mgr),
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

			return new UpdateManager(
				  urlOrPath: $"https://moneymanreleases.blob.core.windows.net/releases/{channel}/{subchannel}/",
				  applicationName: "MoneyMan");
		}
	}
}
