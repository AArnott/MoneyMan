// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MoneyMan.ViewModel;
using Squirrel;

namespace MoneyMan;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
	private readonly CancellationTokenSource closingTokenSource = new();

	public MainWindow()
	{
		this.InitializeComponent();
		this.ViewModel.MainWindow = this;
		this.DataContext = this.ViewModel;
		this.Loaded += this.MainWindow_Loaded;
		this.ViewModel.FileClosed += this.ViewModel_FileClosed;

		this.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, this.FileNew));
		this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, this.FileOpen));
	}

	public bool ReopenLastFile { get; set; } = true;

	public MainPageViewModel ViewModel
	{
		get => (MainPageViewModel)this.Resources["viewModel"];
		set => this.Resources["viewModel"] = value;
	}

	protected override void OnClosed(EventArgs e)
	{
		this.closingTokenSource.Cancel();
		AppSettings.Default.Save();
		base.OnClosed(e);
	}

	private void FileNew(object sender, ExecutedRoutedEventArgs e)
	{
		SaveFileDialog dialog = new()
		{
			Title = this.ViewModel.FileNewDialogTitle,
			OverwritePrompt = true,
		};
		this.InitializeFileDialog(dialog);
		if (dialog.ShowDialog() is true)
		{
			try
			{
				this.ViewModel.OpenNewFile(dialog.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "Unable to create this database: " + ex.Message, "MoneyMan", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	private void FileOpen(object sender, ExecutedRoutedEventArgs e)
	{
		OpenFileDialog dialog = new()
		{
			CheckFileExists = true,
			Title = this.ViewModel.FileOpenDialogTitle,
		};
		this.InitializeFileDialog(dialog);
		if (dialog.ShowDialog() is true)
		{
			this.FileOpen(dialog.FileName);
		}
	}

	private void FileOpen(string path)
	{
		try
		{
			this.ViewModel.OpenExistingFile(path);
			AppSettings.Default.LastOpenedFile = path;
		}
		catch (Exception ex)
		{
			MessageBox.Show($@"Failed to open ""{path}"". {ex.Message}");
		}
	}

	private void ViewModel_FileClosed(object? sender, EventArgs e)
	{
		AppSettings.Default.LastOpenedFile = null;
	}

	private void InitializeFileDialog(FileDialog dialog)
	{
		dialog.AddExtension = true;
		dialog.DefaultExt = ".moneyman";
		dialog.CheckPathExists = true;
		dialog.Filter = "MoneyMan files (*.moneyman)|*.moneyman|All files|*.*";
		dialog.FilterIndex = 0;
	}

#pragma warning disable VSTHRD100 // Avoid async void methods
	private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
	{
		if (this.ReopenLastFile && !string.IsNullOrEmpty(AppSettings.Default.LastOpenedFile))
		{
			if (File.Exists(AppSettings.Default.LastOpenedFile))
			{
				this.FileOpen(AppSettings.Default.LastOpenedFile);
			}
			else
			{
				AppSettings.Default.LastOpenedFile = null;
			}
		}

		try
		{
			await this.UpdateApplicationAsync();
		}
		catch (Exception)
		{
			// Notify the user.
		}
	}

	private async Task UpdateApplicationAsync()
	{
		using UpdateManager updateManager = App.CreateUpdateManager();
		NuGet.SemanticVersion? currentVersion = updateManager.CurrentlyInstalledVersion();
		if (currentVersion is null)
		{
			// This isn't a squirrel installation.
			return;
		}

		ReleaseEntry? result = await updateManager.UpdateApp(p => this.ViewModel.DownloadingUpdatePercentage = p);
		this.ViewModel.DownloadingUpdatePercentage = 100; // Hide the progress bar, since the UpdateManager has a tendency to quit at 99%.
		if (result is null || result.Version == currentVersion)
		{
			// This is the latest version.
			this.ViewModel.UpdateAvailable = false;
		}
		else if (result.Version > currentVersion)
		{
			// An update was brought down. Restarting the app will launch the new version.
			this.ViewModel.UpdateAvailable = true;
			this.ViewModel.StatusMessage = $"Restart to upgrade to {result.Version}";
		}
		else
		{
			// This is newer than the latest stable version.
		}
	}

	private void DataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
	{
		WpfHelpers.GetFirstChildByType<Control>(e.EditingElement)?.Focus();
	}
}
