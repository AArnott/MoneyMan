// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using Microsoft;

	public static class TemplateData
	{
		public static void InjectTemplateData(MoneyFile moneyFile)
		{
			Requires.NotNull(moneyFile, nameof(moneyFile));

			moneyFile.InsertAll(new object[]
			{
				new Category { Name = "Groceries" },
			});
		}
	}
}
