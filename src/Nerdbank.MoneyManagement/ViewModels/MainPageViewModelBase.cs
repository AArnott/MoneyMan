// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Input;
using Microsoft;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class MainPageViewModelBase : BindableBase
{
	private string statusMessage = "Ready";
	private bool updateAvailable;
	private string version = ThisAssembly.AssemblyInformationalVersion;
	private DocumentViewModel? document;
	private int downloadingUpdatePercentage;

	public MainPageViewModelBase()
	{
		this.FileSaveCommand = new SaveCommandImpl(this);
		this.FileCloseCommand = new FileCloseCommandImpl(this);

		this.RegisterDependentProperty(nameof(this.DownloadingUpdatePercentage), nameof(this.UpdateDownloading));
	}

	public event EventHandler? FileClosed;

	public DocumentViewModel? Document
	{
		get => this.document;
		set => this.SetProperty(ref this.document, value);
	}

	public bool IsFileOpen => this.Document is object;

	public CommandBase FileSaveCommand { get; }

	public ICommand FileCloseCommand { get; }

	public string StatusMessage
	{
		get => this.statusMessage;
		set => this.SetProperty(ref this.statusMessage, value);
	}

	public bool UpdateAvailable
	{
		get => this.updateAvailable;
		set => this.SetProperty(ref this.updateAvailable, value);
	}

	public bool UpdateDownloading => this.DownloadingUpdatePercentage is > 0 and < 100;

	public int DownloadingUpdatePercentage
	{
		get => this.downloadingUpdatePercentage;
		set => this.SetProperty(ref this.downloadingUpdatePercentage, value);
	}

	public string Version
	{
		get => this.version;
		set => this.SetProperty(ref this.version, value);
	}

	public string FileOpenDialogTitle => "Open MoneyMan file";

	public string FileNewDialogTitle => "Create new MoneyMan file";

	public void OpenNewFile(string path)
	{
		// Create the new file in a temporary location so we don't conflict with the currently open document.
		string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		DocumentViewModel.CreateNew(tempFile).Dispose();
		this.Document?.Dispose();
		File.Move(tempFile, path, overwrite: true);
		this.ReplaceViewModel(DocumentViewModel.Open(path));
	}

	public void OpenExistingFile(string path)
	{
		this.ReplaceViewModel(DocumentViewModel.Open(path));
	}

	public virtual void ReplaceViewModel(DocumentViewModel? documentViewModel)
	{
		this.Document?.Dispose();
		this.Document = documentViewModel;
		this.OnPropertyChanged(nameof(this.IsFileOpen));
	}

	private class SaveCommandImpl : CommandBase
	{
		private readonly MainPageViewModelBase viewModel;
		private MoneyFile? subscribedMoneyFile;

		public SaveCommandImpl(MainPageViewModelBase viewModel)
		{
			this.viewModel = viewModel;
			viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && this.viewModel.Document?.MoneyFile.UndoStack.Any() is true;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			Verify.Operation(this.viewModel.Document is not null, "No file to save.");
			this.viewModel.Document.MoneyFile.Save();
			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(this.viewModel.Document))
			{
				if (this.subscribedMoneyFile is object)
				{
					this.subscribedMoneyFile.PropertyChanged -= this.DocumentViewModel_PropertyChanged;
				}

				this.subscribedMoneyFile = this.viewModel.Document?.MoneyFile;
				if (this.subscribedMoneyFile is object)
				{
					this.subscribedMoneyFile.PropertyChanged += this.DocumentViewModel_PropertyChanged;
				}

				this.OnCanExecuteChanged();
			}
		}

		private void DocumentViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.subscribedMoneyFile.UndoStack))
			{
				this.OnCanExecuteChanged();
			}
		}
	}

	private class FileCloseCommandImpl : ICommand
	{
		private readonly MainPageViewModelBase viewModel;

		internal FileCloseCommandImpl(MainPageViewModelBase viewModel)
		{
			this.viewModel = viewModel;
			this.viewModel.PropertyChanged += this.MainPageViewModelBase_PropertyChanged;
		}

		public event EventHandler? CanExecuteChanged;

		public bool CanExecute(object? parameter) => this.viewModel.IsFileOpen is true;

		public void Execute(object? parameter)
		{
			this.viewModel.ReplaceViewModel(null);
			this.viewModel.FileClosed?.Invoke(this.viewModel, EventArgs.Empty);
		}

		private void MainPageViewModelBase_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.viewModel.IsFileOpen))
			{
				this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
