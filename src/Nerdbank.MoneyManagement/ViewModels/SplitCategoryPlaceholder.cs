// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	public class SplitCategoryPlaceholder : ITransactionTarget
	{
		public static readonly SplitCategoryPlaceholder Singleton = new();

		private SplitCategoryPlaceholder()
		{
		}

		public string Name => "--split--";

		public string TransferTargetName => this.Name;
	}
}
