// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	/// <summary>
	/// Enumerates the states a transaction may be in.
	/// </summary>
	public enum ClearedState
	{
		/// <summary>
		/// The transaction has not cleared the bank.
		/// </summary>
		None,

		/// <summary>
		/// The transaction has cleared the bank.
		/// </summary>
		Cleared,

		/// <summary>
		/// The transaction has cleared the bank and has been shown to contribute to a local balance that equals the online balance.
		/// </summary>
		Reconciled,
	}
}
