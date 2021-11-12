// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// A category that is assignable to a transaction.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Category : ModelBase
{
	/// <summary>
	/// The sentinel value to use for <see cref="Transaction.CategoryId"/> on split transactions.
	/// </summary>
	public const int Split = -1;

	/// <summary>
	/// Gets or sets the name of this category.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the optional parent category for this category.
	/// </summary>
	public int? ParentCategoryId { get; set; }

	private string? DebuggerDisplay => this.Name;
}
