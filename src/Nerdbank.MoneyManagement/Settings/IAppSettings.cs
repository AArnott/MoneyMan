// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

/// <summary>
/// An interface implemented by classes that contain user, app, or machine settings.
/// </summary>
public interface IAppSettings
{
	/// <summary>
	/// Gets the special location for storage of the settings.
	/// </summary>
	/// <value>The default value is <see cref="Environment.SpecialFolder.LocalApplicationData"/>.</value>
	static virtual Environment.SpecialFolder Location => Environment.SpecialFolder.LocalApplicationData;

	/// <summary>
	/// Gets the leaf filename to use for the settings file.
	/// </summary>
	static virtual string FileName => "settings.json";
}
