// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// An asset, which may be a share of a company, a car, a cryptocurrency, etc.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Asset : ModelBase
{
	/// <summary>
	/// Gets or sets the name of this asset.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	private string? DebuggerDisplay => this.Name;
}
