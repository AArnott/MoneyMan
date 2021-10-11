// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	/// <summary>
	/// Sorts <see cref="AccountViewModel"/> objects by <see cref="AccountViewModel.Name"/>.
	/// </summary>
	internal class AccountSort : IOptimizedComparer<AccountViewModel>
	{
		internal static readonly AccountSort Instance = new AccountSort();

		private AccountSort()
		{
		}

		public int Compare(AccountViewModel? x, AccountViewModel? y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}
			else if (y is null)
			{
				return 1;
			}

			int order = x.Name.CompareTo(y.Name);
			if (order != 0)
			{
				return order;
			}

			order = x.Id is null
				? (y.Id is null ? 0 : -1)
				: (y.Id is null) ? 1 : 0;
			if (order != 0)
			{
				return order;
			}

			return 0;
		}

		public bool IsPropertySignificant(string propertyName) => propertyName is nameof(AccountViewModel.Name) or nameof(AccountViewModel.Id);
	}
}
