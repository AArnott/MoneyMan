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
		this.documentViewModel = documentViewModel;
	}

	/// <summary>
	/// Occurs when a new asset is being interactively created.
	/// </summary>
	public event EventHandler? AddingNewAsset;

	public string Title => "Assets";

	public ICommand AddCommand { get; }

	public string AddCommandCaption => "Add new";

	public string NameLabel => "_Name";

	public int NameMaxLength => 50;

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

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			this.viewModel.NewAsset();
			return Task.CompletedTask;
		}
	}
}
