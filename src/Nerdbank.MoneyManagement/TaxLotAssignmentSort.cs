// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.ViewModels;

namespace Nerdbank.MoneyManagement;

internal class TaxLotAssignmentSort : IOptimizedComparer<TaxLotAssignmentViewModel>
{
	internal static readonly TaxLotAssignmentSort Instance = new();

	private TaxLotAssignmentSort()
	{
	}

	public int Compare(TaxLotAssignmentViewModel? x, TaxLotAssignmentViewModel? y)
	{
		if (x is null)
		{
			return y is null ? 0 : -1;
		}
		else if (y is null)
		{
			return 1;
		}

		// Keep these rules in sync with the linq orderby in TaxLotBookKeeping.IncreaseTaxLotAssignments.
		// Sort first by acquisition date, ascending.
		int order = x.AcquisitionDate.CompareTo(y.AcquisitionDate);
		if (order != 0)
		{
			return order;
		}

		// Then by remaining lot size, ascending. (asc to encourage eliminating small lots rather than shrink larger ones).
		order = x.Available.CompareTo(y.Available);
		if (order != 0)
		{
			return order;
		}

		return 0;
	}

	public bool IsPropertySignificant(string propertyName)
	{
		return propertyName is nameof(TaxLotAssignmentViewModel.AcquisitionDate);
	}
}
