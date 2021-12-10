// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows.Input;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class AssetsPanelViewModel : BindableBase
{
	private readonly SortedObservableCollection<AssetViewModel> assets = new(AssetSort.Instance);
	private readonly DocumentViewModel documentViewModel;
	private AssetViewModel? selectedAsset;

	public AssetsPanelViewModel(DocumentViewModel documentViewModel)
	{
		this.AddCommand = new AddCommandImpl(this);
		this.DeleteCommand = new DeleteCommandImpl(this);
		this.documentViewModel = documentViewModel;
	}

	/// <summary>
	/// Occurs when a new asset is being interactively created.
	/// </summary>
	public event EventHandler? AddingNewAsset;

	public string Title => "Assets";

	public CommandBase AddCommand { get; }

	public CommandBase DeleteCommand { get; }

	public string NameLabel => "_Name";

	public int NameMaxLength => 50;

	public string TypeLabel => "_Type";

	/// <summary>
	/// Gets or sets the selected asset.
	/// </summary>
	public AssetViewModel? SelectedAsset
	{
		get => this.selectedAsset;
		set => this.SetProperty(ref this.selectedAsset, value);
	}

	public IReadOnlyList<AssetViewModel> Assets => this.assets;

	internal AssetViewModel? AddingAsset { get; set; }

	public AssetViewModel NewAsset(string name = "")
	{
		this.AddingNewAsset?.Invoke(this, EventArgs.Empty);
		if (this.AddingAsset is object)
		{
			this.SelectedAsset = this.AddingAsset;
			return this.AddingAsset;
		}

		AssetViewModel newAssetViewModel = new(null, this.documentViewModel.MoneyFile)
		{
			Model = new(),
			Type = Asset.AssetType.Security,
		};

		this.assets.Add(newAssetViewModel);
		this.SelectedAsset = newAssetViewModel;
		if (string.IsNullOrEmpty(name))
		{
			this.AddingAsset = newAssetViewModel;
			newAssetViewModel.NotifyWhenValid(s =>
			{
				if (this.AddingAsset == s)
				{
					this.AddingAsset = null;
				}
			});
		}
		else
		{
			newAssetViewModel.Name = name;
		}

		return newAssetViewModel;
	}

	public void DeleteAsset(AssetViewModel asset)
	{
		this.assets.Remove(asset);

		if (asset.Model is object)
		{
			using IDisposable? transaction = this.documentViewModel.MoneyFile?.UndoableTransaction($"Deleted asset \"{asset.Name}\".", asset.Model);
			this.documentViewModel.MoneyFile?.Delete(asset.Model);
		}

		if (this.SelectedAsset == asset)
		{
			this.SelectedAsset = null;
		}

		if (this.AddingAsset == asset)
		{
			this.AddingAsset = null;
		}
	}

	public AssetViewModel? FindAsset(int id) => this.assets?.FirstOrDefault(a => a.Id == id);

	public AssetViewModel? FindAsset(string name) => this.assets?.FirstOrDefault(a => a.Name == name);

	internal void Add(AssetViewModel asset)
	{
		this.assets.Add(asset);
	}

	internal void ClearViewModel()
	{
		this.assets.Clear();
	}

	private class AssetSort : IComparer<AssetViewModel>
	{
		internal static readonly AssetSort Instance = new AssetSort();

		private AssetSort()
		{
		}

		public int Compare(AssetViewModel? x, AssetViewModel? y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}
			else if (y is null)
			{
				return 1;
			}

			int order = Utilities.CompareNullOrZeroComesLast(x.Id, y.Id);
			if (order != 0)
			{
				return order;
			}

			return StringComparer.CurrentCultureIgnoreCase.Compare(x.Name, y.Name);
		}
	}

	private class AddCommandImpl : CommandBase
	{
		private readonly AssetsPanelViewModel viewModel;

		internal AddCommandImpl(AssetsPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
		}

		public string Caption => "Add new";

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			this.viewModel.NewAsset();
			return Task.CompletedTask;
		}
	}

	private class DeleteCommandImpl : CommandBase
	{
		private readonly AssetsPanelViewModel viewModel;

		internal DeleteCommandImpl(AssetsPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
			viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		public string Caption => "Delete";

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && this.viewModel.SelectedAsset is object;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			AssetViewModel asset = this.viewModel.SelectedAsset ?? throw new InvalidOperationException("Select an asset first.");

			this.viewModel.DeleteAsset(asset);
			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.viewModel.SelectedAsset))
			{
				this.OnCanExecuteChanged();
			}
		}
	}
}
