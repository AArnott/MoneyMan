// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MoneyMan;

public static class WpfHelpers
{
	internal static void DataGridPreviewMouseLeftButtonDownEvent(object sender, RoutedEventArgs e)
	{
		// The original source for this was inspired by https://softwaremechanik.wordpress.com/2013/10/02/how-to-make-all-wpf-datagrid-cells-have-a-single-click-to-edit/
		DataGridCell? cell = e is MouseButtonEventArgs { OriginalSource: UIElement clickTarget } ? FindVisualParent<DataGridCell>(clickTarget) : null;
		if (cell is { IsEditing: false, IsReadOnly: false })
		{
			if (!cell.IsFocused)
			{
				cell.Focus();
			}

			if (FindVisualParent<DataGrid>(cell) is DataGrid dataGrid)
			{
				if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
				{
					if (!cell.IsSelected)
					{
						cell.IsSelected = true;
					}
				}
				else
				{
					if (FindVisualParent<DataGridRow>(cell) is DataGridRow { IsSelected: false } row)
					{
						row.IsSelected = true;
					}
				}
			}
		}
	}

	internal static T? GetFirstChildByType<T>(DependencyObject prop)
		where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(prop);
		for (int i = 0; i < count; i++)
		{
			if (VisualTreeHelper.GetChild(prop, i) is DependencyObject child)
			{
				T? typedChild = child as T ?? GetFirstChildByType<T>(child);
				if (typedChild is object)
				{
					return typedChild;
				}
			}
		}

		return null;
	}

	private static T? FindVisualParent<T>(UIElement element)
		where T : UIElement
	{
		UIElement? parent = element;
		while (parent is object)
		{
			if (parent is T correctlyTyped)
			{
				return correctlyTyped;
			}

			parent = VisualTreeHelper.GetParent(parent) as UIElement;
		}

		return null;
	}
}
