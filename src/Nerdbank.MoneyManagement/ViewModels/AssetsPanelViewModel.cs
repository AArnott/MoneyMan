// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class AssetsPanelViewModel : BindableBase
{
	private readonly SortedObservableCollection<AssetViewModel> assets = new(AssetSort.Instance);
	private AssetViewModel? selectedAsset;

	public string Title => "Assets";

	public string NameLabel => "Name";

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
}
