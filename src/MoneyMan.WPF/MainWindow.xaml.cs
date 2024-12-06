// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft;
using Microsoft.Win32;
using MoneyMan.ViewModel;

namespace MoneyMan;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
	private static readonly PropertyInfo? ContentPresenterTemplateProperty = typeof(ContentPresenter).GetProperty("Template", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

	private readonly CancellationTokenSource closingTokenSource = new();

	public MainWindow()
	{
		this.InitializeComponent();
		this.ViewModel.MainWindow = this;
		this.DataContext = this.ViewModel;
		this.Loaded += this.MainWindow_Loaded;

		this.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, this.FileNew));
		this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, this.FileOpen));
		this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, this.FileSave));
	}

	public MainPageViewModel ViewModel
	{
		get => (MainPageViewModel)this.Resources["viewModel"];
		set => this.Resources["viewModel"] = value;
	}

	protected override void OnClosed(EventArgs e)
	{
		this.closingTokenSource.Cancel();
		this.ViewModel.Document?.Dispose();
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
			try
			{
				this.ViewModel.OpenExistingFile(dialog.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show($@"Failed to open ""{dialog.FileName}"". {ex.Message}");
			}
		}
	}

	private void FileSave(object sender, ExecutedRoutedEventArgs e)
	{
		Verify.Operation(this.ViewModel.Document is not null, "No file to be saved.");
		this.ViewModel.Document.Save();
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
		await this.ViewModel.InitializeAsync(this.closingTokenSource.Token);

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
		using UpdateManager updateManager = App.CreateUpdateManager(out string channel);
		this.ViewModel.UpdateChannel = channel;
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

	private void BankingPanelAccountList_SelectionChanged(object sender, SelectionChangedEventArgs e) => this.UpdateSelectedTransactions();

	private void BankingSelectedAccountPresenter_TemplateApplied(object sender, EventArgs e) => this.UpdateSelectedTransactions();

	private void UpdateSelectedTransactions()
	{
		if (this.ViewModel.Document is null)
		{
			return;
		}

		this.ViewModel.Document.SelectedTransactions = null;
		if (this.BankingAccountsListView.SelectedItem?.GetType() is Type accountType)
		{
			DataTemplateKey key = new(accountType);
			var accountTemplate = (DataTemplate?)this.BankingLayoutGrid.FindResource(key);
			Assumes.NotNull(accountTemplate);
			if (VisualTreeHelper.GetChildrenCount(this.BankingSelectedAccountPresenter) > 0)
			{
				ContentPresenter? presenter = VisualTreeHelper.GetChild(this.BankingSelectedAccountPresenter, 0) as ContentPresenter;
				Assumes.NotNull(presenter);
				presenter.ApplyTemplate();

				// This next step may throw InvalidOperationException, but we can't avoid it through conventional checks.
				// See https://github.com/dotnet/wpf/issues/6343 for a request to address this.
				// Since this exception is thrown on the startup path which is a royal pain when debugging, we go to lengths by using Reflection to avoid the exception.
				if (ContentPresenterTemplateProperty is null || ContentPresenterTemplateProperty.GetValue(presenter) == accountTemplate)
				{
					try
					{
						var transactionDataGrid = (DataGrid?)accountTemplate.FindName("TransactionDataGrid", presenter);
						this.ViewModel.Document.SelectedTransactions = transactionDataGrid?.SelectedItems;
					}
					catch (InvalidOperationException) when (ContentPresenterTemplateProperty is null)
					{
						// Sometimes the template hasn't been applied yet, and this throws (if our Reflection approach didn't work).
					}
				}
			}
		}
	}

	private void TextBox_GotKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e)
	{
		if (sender is TextBox tb)
		{
			// This gives us the desired effect of selecting all text in a textbox when the user selects it.
			_ = tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()));
		}
	}
}
