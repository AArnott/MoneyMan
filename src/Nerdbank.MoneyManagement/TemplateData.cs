// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MoneyManagement;

public static class TemplateData
{
	public static void InjectTemplateData(MoneyFile moneyFile)
	{
		Requires.NotNull(moneyFile, nameof(moneyFile));

		moneyFile.InsertAll(new ModelBase[]
		{
			new Category { Name = "Groceries" },
			new Category { Name = "Entertainment" },
			new Category { Name = "Salary" },
			new Account { Name = "Checking", CurrencyAssetId = moneyFile.PreferredAssetId },
			new Account { Name = "Savings", CurrencyAssetId = moneyFile.PreferredAssetId },
		});
	}
}
