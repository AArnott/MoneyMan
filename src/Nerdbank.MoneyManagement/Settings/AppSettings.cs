// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Text.Json;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Contains static methods for saving and loading settings classes.
/// </summary>
public static class AppSettings
{
	public const string AppFolderName = "MoneyMan";
	private static readonly JsonSerializerOptions JsonOptions = new() { AllowTrailingCommas = true, MaxDepth = 5, WriteIndented = true };

	/// <summary>
	/// Loads settings from disk.
	/// </summary>
	/// <typeparam name="T">The type of settings class to load.</typeparam>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The settings object. Deserialized from disk if possible, otherwise a fresh instance of it.</returns>
	public static async ValueTask<T> LoadAsync<T>(CancellationToken cancellationToken = default)
		where T : IAppSettings, new()
	{
		string settingsPath = GetSettingsPath<T>();
		if (File.Exists(settingsPath))
		{
			try
			{
				using Stream settingsStream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
				if (await LoadAsync<T>(settingsStream, cancellationToken).ConfigureAwait(false) is T settings)
				{
					return settings;
				}
			}
			catch
			{
			}
		}

		return new T();
	}

	/// <summary>
	/// Loads settings from a stream.
	/// </summary>
	/// <typeparam name="T">The type of settings class to load.</typeparam>
	/// <param name="stream">The stream to load settings from.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The settings object. Deserialized from disk if possible, otherwise a fresh instance of it.</returns>
	public static ValueTask<T?> LoadAsync<T>(Stream stream, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

	/// <summary>
	/// Saves a settings object to disk.
	/// </summary>
	/// <typeparam name="T">The type of settings class to be saved.</typeparam>
	/// <param name="settings">The settings to save.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks completion of the save operation.</returns>
	public static async Task SaveAsync<T>(this T settings, CancellationToken cancellationToken = default)
		where T : IAppSettings
	{
		string settingsPath = GetSettingsPath<T>();
		Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
		using Stream settingsStream = new FileStream(settingsPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
		await SaveAsync<T>(settings, settingsStream, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Saves a settings object to a stream.
	/// </summary>
	/// <typeparam name="T">The type of settings class to be saved.</typeparam>
	/// <param name="settings">The settings to save.</param>
	/// <param name="stream">The stream to serialize to.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks completion of the save operation.</returns>
	public static Task SaveAsync<T>(this T settings, Stream stream, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);

	private static string GetSettingsPath<T>()
		where T : IAppSettings
	{
		string basePath = Environment.GetFolderPath(T.Location);
		if (string.IsNullOrEmpty(basePath))
		{
			throw new InvalidOperationException($"No path has been set for {T.Location}.");
		}

		string settingsPath = Path.Combine(basePath, AppFolderName, T.FileName);
		return settingsPath;
	}
}
