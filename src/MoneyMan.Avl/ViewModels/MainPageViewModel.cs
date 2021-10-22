// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.Avl.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reactive;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using Avalonia.Controls;
	using Nerdbank.MoneyManagement.ViewModels;
	using ReactiveUI;

	public class MainPageViewModel : MainPageViewModelBase
	{
		public MainPageViewModel()
		{
			this.FileNewCommand = ReactiveCommand.CreateFromTask(this.FileNewAsync);
			this.FileOpenCommand = ReactiveCommand.CreateFromTask(this.FileOpenAsync);
		}

		public ReactiveCommand<Unit, Unit> FileNewCommand { get; }

		public ReactiveCommand<Unit, Unit> FileOpenCommand { get; }

		internal Window MainWindow { get; set; } = null!;

		public override void ReplaceViewModel(DocumentViewModel viewModel)
		{
			this.Document.CategoriesPanel.AddingNewCategory -= this.CategoriesPanel_AddingNewCategory;

			base.ReplaceViewModel(viewModel);

			this.Document.CategoriesPanel.AddingNewCategory += this.CategoriesPanel_AddingNewCategory;
			this.Document.CategoriesPanel.SelectedCategories = this.MainWindow.Find<ListBox>("CategoriesListView").SelectedItems;

			//this.Document.SelectedTransactions = this.TransactionDataGrid.SelectedItems;
		}

		private void CategoriesPanel_AddingNewCategory(object? sender, EventArgs e)
		{
			this.MainWindow.Find<TextBox>("CategoryName").Focus();
		}

		private async Task FileNewAsync()
		{
			SaveFileDialog dlg = new SaveFileDialog
			{
				Title = "Save MoneyMan file",
				Filters =
				{
					new FileDialogFilter { Name = "MoneyMan Files", Extensions = { "moneyman" } },
				},
			};
			string path = await dlg.ShowAsync(this.MainWindow);

			// Create the new file in a temporary location so we don't conflict with the currently open document.
			string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			DocumentViewModel.CreateNew(tempFile).Dispose();
			this.Document.Dispose();
			File.Move(tempFile, path, overwrite: true);
			this.ReplaceViewModel(DocumentViewModel.Open(path));
		}

		private async Task FileOpenAsync()
		{
			OpenFileDialog dlg = new OpenFileDialog
			{
				Title = "Open MoneyMan file",
				Filters =
				{
					new FileDialogFilter { Name = "MoneyMan Files", Extensions = { "moneyman" } },
				},
			};
			string[] result = await dlg.ShowAsync(this.MainWindow);
			if (result.Length == 1)
			{
				this.FileOpen(result[0]);
			}
		}

		private void FileOpen(string path)
		{
			this.ReplaceViewModel(DocumentViewModel.Open(path));
		}
	}
}
