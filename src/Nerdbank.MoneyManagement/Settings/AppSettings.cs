// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// A base class for application settings.
/// </summary>
/// <remarks>
/// Derived types should define settings as properties with init-only accessors.
/// </remarks>
public abstract record AppSettings
{
	private static readonly JsonSerializerOptions JsonOptions = new() { AllowTrailingCommas = true, MaxDepth = 5, WriteIndented = true };

	/// <summary>
	/// Gets the leaf name of the folder that should contain settings.
	/// </summary>
	/// <remarks>
	/// This property should only be used from the <see cref="SettingsPath"/> property.
	/// An override of that property may choose to ignore this one.
	/// </remarks>
	[JsonIgnore]
	public virtual string AppFolderName => "MoneyMan";

	/// <summary>
	/// Gets the special location for storage of the settings.
	/// </summary>
	/// <value>The default value is <see cref="Environment.SpecialFolder.LocalApplicationData"/>.</value>
	/// <remarks>
	/// This property should only be used from the <see cref="SettingsPath"/> property.
	/// An override of that property may choose to ignore this one.
	/// </remarks>
	protected virtual Environment.SpecialFolder Location => Environment.SpecialFolder.LocalApplicationData;

	/// <summary>
	/// Gets the leaf filename to use for the settings file.
	/// </summary>
	/// <remarks>
	/// This property should only be used from the <see cref="SettingsPath"/> property.
	/// An override of that property may choose to ignore this one.
	/// </remarks>
	protected virtual string FileName => "settings.json";

	/// <summary>
	/// Gets the path to the settings file that will be used to persist or load this object.
	/// </summary>
	/// <value>A rooted file system path.</value>
	/// <exception cref="InvalidOperationException">Thrown when the base path cannot be determined.</exception>
	protected virtual string SettingsPath
	{
		get
		{
			string basePath = Environment.GetFolderPath(this.Location);
			if (string.IsNullOrEmpty(basePath))
			{
				throw new InvalidOperationException($"No path has been set for {this.Location}.");
			}

			string settingsPath = Path.Combine(basePath, this.AppFolderName, this.FileName);
			return settingsPath;
		}
	}

	/// <summary>
	/// Loads settings from disk.
	/// </summary>
	/// <typeparam name="T">The type of settings class to load.</typeparam>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The settings object. Deserialized from disk if possible, otherwise a fresh instance of it.</returns>
	protected async ValueTask<T?> LoadAsync<T>(CancellationToken cancellationToken = default)
		where T : AppSettings
	{
		string settingsPath = this.SettingsPath;
		if (File.Exists(settingsPath))
		{
			try
			{
				using Stream settingsStream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
				return await this.LoadAsync<T>(settingsStream, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
			}
		}

		return null;
	}

	/// <summary>
	/// Loads settings from a stream.
	/// </summary>
	/// <typeparam name="T">The type of settings class to load.</typeparam>
	/// <param name="stream">The stream to load settings from.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The settings object. Deserialized from disk if possible, otherwise a fresh instance of it.</returns>
	protected ValueTask<T?> LoadAsync<T>(Stream stream, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

	/// <summary>
	/// Saves a settings object to disk.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks completion of the save operation.</returns>
	public async Task SaveAsync(CancellationToken cancellationToken = default)
	{
		string settingsPath = this.SettingsPath;
		Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
		using Stream settingsStream = new FileStream(settingsPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
		await this.SaveAsync(settingsStream, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Saves a settings object to a stream.
	/// </summary>
	/// <param name="stream">The stream to serialize to.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks completion of the save operation.</returns>
	public Task SaveAsync(Stream stream, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, this, this.GetType(), JsonOptions, cancellationToken);
}
