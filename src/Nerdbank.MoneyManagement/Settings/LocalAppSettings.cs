// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Defines app settings that are per-user, per machine.
/// </summary>
public record LocalAppSettings : IAppSettings
{
	public string? LastOpenedFile { get; init; }

	public bool ReopenLastFile { get; init; } = true;
}
