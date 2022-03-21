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
			Category("Groceries"),
			Category("Entertainment"),
			Category("Salary"),
			Banking("Checking"),
			Banking("Savings"),
		});

		Account Category(string name) => new() { Name = name, Type = Account.AccountType.Category };
		Account Banking(string name) => new() { Name = name, CurrencyAssetId = moneyFile.PreferredAssetId };
	}
}
