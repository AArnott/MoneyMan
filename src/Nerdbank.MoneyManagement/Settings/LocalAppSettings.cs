﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Defines app settings that are per-user, per machine.
/// </summary>
public record LocalAppSettings : AppSettings
{
	/// <summary>
	/// Gets the path to the last file opened in the application.
	/// </summary>
	public string? LastOpenedFile { get; init; }

	/// <summary>
	/// Gets a value indicating whether the last file should be reopened when the application launches next time.
	/// </summary>
	public bool ReopenLastFile { get; init; } = true;
}
