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
	/// <remarks>
	/// Used for transactions that do not conform to any of the other kinds.
	/// </remarks>
	Unspecified = 0,

	/// <summary>
	/// Deposit novel assets or cash into the database.
	/// </summary>
	/// <remarks>
	/// Used in a banking transaction that has one entry that adds currency to an account, and zero or more entries that withdraw currency from categories.
	/// </remarks>
	Deposit,

	/// <summary>
	/// Withdraw assets or cash from tracking in the database.
	/// </summary>
	/// <remarks>
	/// Used in a banking transaction that has one entry that withdraws currency from an account, and zero or more entries that add currency to categories.
	/// </remarks>
	Withdraw,

	/// <summary>
	/// Transfers cash or assets across accounts.
	/// </summary>
	/// <remarks>
	/// Two or more entries that are all assigned to non-category accounts, involve at least two unique accounts, and the same asset is associated with all entries.
	/// </remarks>
	Transfer,

	/// <summary>
	/// Shares bought with cash.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that withdraws currency from an account, and another entry that adds a security to an account.
	/// A third entry that assigns a portion of the withdrawn currency to a category (for a commission) is also allowed.
	/// </remarks>
	Buy,

	/// <summary>
	/// Shares sold for cash.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that withdraws a security from an account, and another entry that adds currency to an account.
	/// A third entry that assigns some of the currency to a category (for a commission) is also allowed.
	/// </remarks>
	Sell,

	/// <summary>
	/// Shares of one security traded directly for shares of another security.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that withdraws a security from an account, and another entry that adds a security to an account.
	/// Other entries may be also be present.
	/// </remarks>
	Exchange,

	/// <summary>
	/// In-kind dividend (non-cash), or yield from staking cryptocurrency.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that adds currency to an account and references a security via <see cref="Transaction.RelatedAssetId"/>.
	/// </remarks>
	Dividend,

	/// <summary>
	/// Income from interest on a cash balance.
	/// </summary>
	/// <remarks>
	/// Used in a banking transaction that has one entry that adds currency to an account.
	/// TODO: consider removing this as it is not significantly different from a special case of <see cref="Deposit"/> where the negative amount entry is to a category named "Interest" or similar.
	///       But first we'll have to support setting categories for investment transactions in the view model.
	/// </remarks>
	Interest,

	/// <summary>
	/// Shares added.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that adds a security to an account.
	/// </remarks>
	Add,

	/// <summary>
	/// Shares removed.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that withdraws a security from an account.
	/// </remarks>
	Remove,

	/// <summary>
	/// Sale of borrowed securities.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that adds currency to an account, and another entry that adds a special negative position security to an account.
	/// </remarks>
	ShortSale,

	/// <summary>
	/// Covers a prior short sale.
	/// </summary>
	/// <remarks>
	/// Used in an investment transaction that has one entry that withdraws currency from an account, and another entry that removes a special negative position security from an account.
	/// </remarks>
	CoverShort,
}
