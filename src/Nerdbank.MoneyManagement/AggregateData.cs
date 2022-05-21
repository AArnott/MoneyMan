// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

public class AggregateData
{
	internal AggregateData(IReadOnlyDictionary<int, decimal> accountBalances, decimal netWorth, long dataVersion)
	{
		this.AccountBalances = accountBalances;
		this.NetWorth = netWorth;
		this.DataVersion = dataVersion;
	}

	public IReadOnlyDictionary<int, decimal> AccountBalances { get; }

	public decimal NetWorth { get; }

	internal long DataVersion { get; }
}
