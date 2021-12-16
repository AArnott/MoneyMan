// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

/// <summary>
/// The kinds of investment transactions.
/// </summary>
public enum TransactionAction
{
	/// <summary>
	/// Unspecified.
	/// </summary>
	Unspecified = 0,

	/// <summary>
	/// Deposit novel assets or cash into the database.
	/// </summary>
	Deposit,

	/// <summary>
	/// Withdraw assets or cash from tracking in the database.
	/// </summary>
	Withdraw,

	/// <summary>
	/// Transfers cash or assets across accounts.
	/// </summary>
	Transfer,

	/// <summary>
	/// Shares bought with cash.
	/// </summary>
	Buy,

	/// <summary>
	/// Shares sold for cash.
	/// </summary>
	Sell,

	/// <summary>
	/// Shares of one security traded directly for shares of another security.
	/// </summary>
	Exchange,

	/// <summary>
	/// In-kind dividend (non-cash), or yield from staking cryptocurrency.
	/// </summary>
	Dividend,

	/// <summary>
	/// Income from interest on a cash balance.
	/// </summary>
	Interest,

	/// <summary>
	/// Shares added.
	/// </summary>
	Add,

	/// <summary>
	/// Shares removed.
	/// </summary>
	Remove,
}
