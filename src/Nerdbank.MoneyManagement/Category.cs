// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using SQLite;

	/// <summary>
	/// A category that is assignable to a transaction.
	/// </summary>
	public class Category
	{
		/// <summary>
		/// The sentinel value to use for <see cref="Transaction.CategoryId"/> on split transactions.
		/// </summary>
		public const int Split = -1;

		/// <summary>
		/// Gets or sets the primary key for this entity.
		/// </summary>
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the name of this category.
		/// </summary>
		[NotNull]
		public string? Name { get; set; }

		/// <summary>
		/// Gets or sets the optional parent category for this category.
		/// </summary>
		public int? ParentCategoryId { get; set; }
	}
}
