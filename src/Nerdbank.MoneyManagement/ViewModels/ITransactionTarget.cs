// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	/// <summary>
	/// A <see cref="CategoryViewModel"/> or <see cref="AccountViewModel"/>,
	/// either of which may be the "target" of a transaction which represents
	/// the other side of a debit or credit.
	/// </summary>
	public interface ITransactionTarget
	{
		string? Name { get; }
	}
}
