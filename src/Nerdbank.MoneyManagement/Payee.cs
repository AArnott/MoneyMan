// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using System.Diagnostics;
	using SQLite;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class Payee
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		[NotNull]
		public string? Name { get; set; }

		private string? DebuggerDisplay => this.Name;
	}
}
