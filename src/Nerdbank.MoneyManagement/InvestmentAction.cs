// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

/// <summary>
/// The kinds of investment transactions.
/// </summary>
public enum InvestmentAction
{
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

	/// <summary>
	/// Adjust share balance (usually to forcibly reconcile local records with those at a bank).
	/// </summary>
	AdjustShareBalance,

	/// <summary>
	/// Deposit cash.
	/// </summary>
	Deposit,

	/// <summary>
	/// Withdraw cash.
	/// </summary>
	Withdraw,
}
