﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Navigation;
	using Microsoft.Win32;
	using MoneyMan.ViewModel;
	using Nerdbank.MoneyManagement;
	using Nerdbank.MoneyManagement.ViewModels;
	using Squirrel;

	/// <summary>
	/// Interaction logic for MainWindow.xaml.
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly CancellationTokenSource closingTokenSource = new();

		public MainWindow()
		{
			this.InitializeComponent();
			this.DataContext = this.ViewModel;
			this.Loaded += this.MainWindow_Loaded;

			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, this.FileNew));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, this.FileOpen));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, this.FileClose, this.CanFileClose));
		}

		public bool ReopenLastFile { get; set; } = true;

		public MainPageViewModel ViewModel
		{
			get => (MainPageViewModel)this.Resources["viewModel"];
			set => this.Resources["viewModel"] = value;
		}

		internal void ReplaceViewModel(DocumentViewModel viewModel)
		{
			this.ViewModel.Document.Dispose();

			// BUGBUG: This doesn't trigger data-binding to reapply to the new view model.
			this.ViewModel.Document = viewModel;

			this.ViewModel.Document.CategoriesPanel.SelectedCategories = this.CategoriesListView.SelectedItems;
			this.ViewModel.Document.SelectedTransactions = this.TransactionDataGrid.SelectedItems;
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
				Title = "Create new MoneyMan file",
				OverwritePrompt = true,
			};
			this.InitializeFileDialog(dialog);
			if (dialog.ShowDialog() is true)
			{
				// Create the new file in a temporary location so we don't conflict with the currently open document.
				string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				DocumentViewModel.CreateNew(tempFile).Dispose();
				this.ViewModel.Document.Dispose();
				File.Move(tempFile, dialog.FileName, overwrite: true);
				this.ReplaceViewModel(DocumentViewModel.Open(dialog.FileName));
			}
		}

		private void FileOpen(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog dialog = new()
			{
				CheckFileExists = true,
				Title = "Open MoneyMan file",
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
				this.ReplaceViewModel(DocumentViewModel.Open(path));
				AppSettings.Default.LastOpenedFile = path;
			}
			catch (Exception ex)
			{
				MessageBox.Show($@"Failed to open ""{path}"". {ex.Message}");
			}
		}

		private void FileClose(object sender, ExecutedRoutedEventArgs e)
		{
			this.ReplaceViewModel(new DocumentViewModel(null));
			AppSettings.Default.LastOpenedFile = null;
		}

		private void CanFileClose(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = this.ViewModel.Document.IsFileOpen;
		}

		private void InitializeFileDialog(FileDialog dialog)
		{
			dialog.AddExtension = true;
			dialog.DefaultExt = ".moneyman";
			dialog.CheckPathExists = true;
			dialog.Filter = "MoneyMan files (*.moneyman)|*.moneyman|All files|*.*";
			dialog.FilterIndex = 0;
		}

		private void TransactionGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
		{
			e.NewItem = this.ViewModel.Document.BankingPanel.SelectedAccount?.NewTransaction(volatileOnly: true) ?? throw new InvalidOperationException("No selected account.");
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

		private void TransactionDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
		{
			WpfHelpers.GetFirstChildByType<Control>(e.EditingElement)?.Focus();
		}
	}
}
