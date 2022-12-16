// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel;
using System.Threading.Tasks.Dataflow;
using System.Windows.Input;
using Microsoft;
using Microsoft.VisualStudio.Threading;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class MainPageViewModelBase : BindableBase, System.IAsyncDisposable
{
	private readonly bool persistSettings;
	private string statusMessage = "Ready";
	private bool updateAvailable;
	private string version = ThisAssembly.AssemblyInformationalVersion;
	private DocumentViewModel? document;
	private int downloadingUpdatePercentage;
	private string? updateChannel;
	private LocalAppSettings localAppSettings = new();
	private ActionBlock<Func<Task>> settingsSaver = new(a => a(), new() { BoundedCapacity = 2 });

	public MainPageViewModelBase(bool persistSettings)
	{
		this.persistSettings = persistSettings;

		this.FileSaveCommand = new SaveCommandImpl(this);
		this.FileCloseCommand = new FileCloseCommandImpl(this);

		this.RegisterDependentProperty(nameof(this.DownloadingUpdatePercentage), nameof(this.UpdateDownloading));
		this.RegisterDependentProperty(nameof(this.Document), nameof(this.UndoCommand));
		this.RegisterDependentProperty(nameof(this.Document), nameof(this.ImportFileCommand));
		this.RegisterDependentProperty(nameof(this.Document), nameof(this.IsFileOpen));
	}

	public event EventHandler? FileClosed;

	public LocalAppSettings LocalAppSettings
	{
		get => this.localAppSettings;
		set
		{
			if (this.localAppSettings != value)
			{
				this.localAppSettings = value;
				if (this.persistSettings)
				{
					this.settingsSaver.Post(() => this.localAppSettings.SaveAsync());
				}
			}
		}
	}

	public DocumentViewModel? Document
	{
		get => this.document;
		set => this.SetProperty(ref this.document, value);
	}

	public bool IsFileOpen => this.Document is object;

	public CommandBase FileSaveCommand { get; }

	public ICommand FileCloseCommand { get; }

	public UICommandBase UndoCommand => this.Document?.UndoCommand ?? OutOfContextCommand.Instance;

	public UICommandBase ImportFileCommand => this.Document?.ImportFileCommand ?? OutOfContextCommand.Instance;

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

	public string? UpdateChannel
	{
		get => this.updateChannel;
		set => this.SetProperty(ref this.updateChannel, value);
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
		this.OpenExistingFile(path);
	}

	public void OpenExistingFile(string path)
	{
		this.ReplaceViewModel(DocumentViewModel.Open(path));
		this.LocalAppSettings = this.LocalAppSettings with { LastOpenedFile = path };
	}

	public virtual void ReplaceViewModel(DocumentViewModel? documentViewModel)
	{
		this.Document?.Dispose();
		this.Document = documentViewModel;
	}

	public virtual async Task InitializeAsync(CancellationToken cancellationToken)
	{
		if (this.persistSettings)
		{
			// Set the field to avoid the property setter immediately scheduling a re-save.
			this.localAppSettings = await AppSettings.LoadAsync<LocalAppSettings>(cancellationToken);
		}

		LocalAppSettings settings = this.LocalAppSettings;
		if (settings.ReopenLastFile && !string.IsNullOrEmpty(settings.LastOpenedFile) && !this.IsFileOpen)
		{
			bool openSucceeded = false;
			try
			{
				if (File.Exists(settings.LastOpenedFile))
				{
					this.OpenExistingFile(settings.LastOpenedFile);
					openSucceeded = true;
				}
			}
			catch
			{
			}

			if (!openSucceeded)
			{
				// Do not try to reopen this file next time.
				this.LocalAppSettings = this.LocalAppSettings with { LastOpenedFile = null };
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		this.settingsSaver.Complete();
		await this.settingsSaver.Completion.NoThrowAwaitable();
	}

	protected virtual void OnFileClosed()
	{
		this.LocalAppSettings = this.LocalAppSettings with { LastOpenedFile = null };
		this.FileClosed?.Invoke(this, EventArgs.Empty);
	}

	private class SaveCommandImpl : UICommandBase
	{
		private readonly MainPageViewModelBase viewModel;
		private MoneyFile? subscribedMoneyFile;

		public SaveCommandImpl(MainPageViewModelBase viewModel)
		{
			this.viewModel = viewModel;
			viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		public override string Caption => "Save";

		public override string? InputGestureText => "Ctrl+S";

		public override bool Visible => this.viewModel.Document is not null;

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
				this.OnPropertyChanged(nameof(this.Visible));
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

	private class FileCloseCommandImpl : UICommandBase
	{
		private readonly MainPageViewModelBase viewModel;

		internal FileCloseCommandImpl(MainPageViewModelBase viewModel)
		{
			this.viewModel = viewModel;
			this.viewModel.PropertyChanged += this.MainPageViewModelBase_PropertyChanged;
		}

		public override string Caption => "Close";

		public override bool Visible => this.viewModel.IsFileOpen;

		public override bool CanExecute(object? parameter) => this.viewModel.IsFileOpen;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			this.viewModel.ReplaceViewModel(null);
			this.viewModel.OnFileClosed();
			return Task.CompletedTask;
		}

		private void MainPageViewModelBase_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.viewModel.IsFileOpen))
			{
				this.OnCanExecuteChanged();
				this.OnPropertyChanged(nameof(this.Visible));
			}
		}
	}
}
