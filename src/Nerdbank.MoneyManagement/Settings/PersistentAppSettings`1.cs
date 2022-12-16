// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Threading;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// A container for <see cref="AppSettings"/> that can load or automatically save those settings.
/// </summary>
/// <typeparam name="T">The type of settings being tracked.</typeparam>
public class PersistentAppSettings<T> : System.IAsyncDisposable
	where T : AppSettings, new()
{
	private readonly ActionBlock<bool>? saveBlock;
	private T settings = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="PersistentAppSettings{T}"/> class.
	/// </summary>
	/// <param name="initialSettings">The settings to start with. May be <see langword="null" /> to use the default constructor for the initial settings.</param>
	/// <param name="autoSave"><see langword="true" /> to schedule async save operations after each change to the settings.</param>
	public PersistentAppSettings(T? initialSettings = null, bool autoSave = true)
	{
		if (autoSave)
		{
			this.saveBlock = new ActionBlock<bool>(
				_ => this.settings.SaveAsync(),
				new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });
		}

		this.settings = initialSettings ?? new();
	}

	public T Value
	{
		get => this.settings;
		set
		{
			if (this.settings != value)
			{
				this.settings = value;
				this.saveBlock?.Post(true);
			}
		}
	}

	/// <summary>
	/// Attempts to load the app settings from disk and replaces the current <see cref="Value"/>
	/// with the loaded settings if successful.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that completes when the attempt is over.</returns>
	public async Task LoadAsync(CancellationToken cancellationToken)
	{
		if (await this.settings.LoadAsync<T>(cancellationToken) is T newSettings)
		{
			// Set the field to avoid the property setter immediately scheduling a re-save.
			this.settings = newSettings;
		}
	}

	/// <summary>
	/// Finishes any pending save operations and prevents any further saving.
	/// </summary>
	/// <returns>A task that completes when saving has completed.</returns>
	/// <remarks>
	/// This method never throws exceptions.
	/// </remarks>
	public async ValueTask DisposeAsync()
	{
		if (this.saveBlock is not null)
		{
			this.saveBlock.Complete();
			await this.saveBlock.Completion.NoThrowAwaitable();
		}
	}
}
