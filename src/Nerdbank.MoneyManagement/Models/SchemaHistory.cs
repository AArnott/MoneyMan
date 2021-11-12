// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using SQLite;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Describes a version of the database schema that this database is or was at some past time.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
internal class SchemaHistory
{
	/// <summary>
	/// Gets or sets the schema version of the database as of the time represented by this row.
	/// </summary>
	[PrimaryKey]
	public int SchemaVersion { get; set; }

	/// <summary>
	/// Gets or sets the date the database was created or upgraded to this <see cref="SchemaVersion"/>.
	/// </summary>
	public DateTime AppliedDateUtc { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="AssemblyInformationalVersionAttribute">assembly informational version</see>
	/// of the running application when the database was created or upgraded to this <see cref="SchemaVersion"/>.
	/// </summary>
	public string? AppVersion { get; set; }

	private string DebuggerDisplay => $"{this.SchemaVersion}";
}
