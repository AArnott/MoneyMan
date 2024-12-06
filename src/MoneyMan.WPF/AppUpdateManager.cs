// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Threading;
using MoneyMan.ViewModels;
using Velopack;

namespace MoneyMan;

/// <summary>
/// Manages the app's self-updating functionality.
/// </summary>
public class AppUpdateManager
{
	private readonly App app;
	private readonly UpdateManager? updateManager;
	private readonly ObservableBox<bool> isUpdateReady = new(false);
	private readonly ObservableBox<UpdateInfo?> updateInfo = new(null);
	private readonly AsyncSemaphore downloadSemaphore = new(1);

	public AppUpdateManager(App app, string? velopackUpdateUrl)
	{
		this.app = app;
		if (velopackUpdateUrl is not null)
		{
			this.updateManager = new(velopackUpdateUrl);
		}
	}

	public bool IsInstalled => this.updateManager?.IsInstalled is true;

	public IObservable<bool> IsUpdateReady => this.isUpdateReady;

	public IObservable<UpdateInfo?> UpdateInfo => this.updateInfo;

	public SelfUpdateProgressData UpdateDownloading { get; } = new();

	/// <summary>
	/// Checks for and downloads an update if one is available.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>
	/// <see langword="true" /> if an update is downloaded and ready to apply;
	/// <see langword="false" /> if no update is available;
	/// <see langword="null" /> if the app is not running in a self-updatable (installed) configuration.
	/// </returns>
	/// <remarks>
	/// This method is safe to call multiple times concurrently, but only one download will be in progress at a time.
	/// </remarks>
	public async Task<bool?> DownloadUpdateAsync(CancellationToken cancellationToken)
	{
		if (!this.IsInstalled || this.updateManager is null)
		{
			return null;
		}

		using (await this.downloadSemaphore.EnterAsync(cancellationToken))
		{
			this.isUpdateReady.Value = this.updateManager.IsUpdatePendingRestart;

			UpdateInfo? updateInfo = await this.updateManager.CheckForUpdatesAsync();
			this.updateInfo.Value = updateInfo;

			// If an update is available, download it (but don't install it immediately).
			if (updateInfo is not null)
			{
				this.UpdateDownloading.NotifyDownloadingUpdate(updateInfo.TargetFullRelease.Version.ToString());

				await this.updateManager.DownloadUpdatesAsync(
					updateInfo,
					percent => this.UpdateDownloading.Current = (ulong)percent,
					cancelToken: cancellationToken);

				this.UpdateDownloading.Complete();
				this.isUpdateReady.Value = true;
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Periodically calls <see cref="DownloadUpdateAsync(CancellationToken)"/>,
	/// beginning immediately.
	/// </summary>
	/// <param name="cancellationToken">
	/// A cancellation token that terminates the periodic polling and any currently downloading update.
	/// </param>
	/// <returns>A task that represents the polling operation.</returns>
	public async Task PeriodicallyCheckForUpdatesAsync(CancellationToken cancellationToken)
	{
		if (!this.IsInstalled || this.updateManager is null)
		{
			return;
		}

		while (!cancellationToken.IsCancellationRequested)
		{
			await this.DownloadUpdateAsync(cancellationToken);

			// This app has a tendency to be left open for days at a time.
			// So rather than only check for updates on startup, we'll check once a day.
			await Task.Delay(TimeSpan.FromDays(1), cancellationToken);
		}
	}

	/// <summary>
	/// Closes the running application, applies the pre-downloaded update, and restarts the app.
	/// </summary>
	/// <returns>A task that you probably shouldn't expect to complete, because the process will shutdown before it's done.</returns>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="IsUpdateReady"/> is <see langword="false"/>.</exception>
	public async Task RestartUpdatedVersionAsync()
	{
		Verify.Operation(this.updateManager is not null, "Channel URL must be specified on construction.");
		if (this.updateInfo.Value is UpdateInfo updateInfo)
		{
			// Conduct a graceful exit of the app.
			await this.app.DisposeAsync();

			// This must be done last, as it exits the application.
			this.updateManager.ApplyUpdatesAndRestart(updateInfo);
		}
		else
		{
			throw new InvalidOperationException("No update available.");
		}
	}

	internal async Task MockUpdateAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(2000, cancellationToken);
		this.updateInfo.Value = new(new VelopackAsset { Version = new(1, 2, 3) }, false);
		this.UpdateDownloading.NotifyDownloadingUpdate("Mock");
		for (int i = 0; i <= 10; i++)
		{
			this.UpdateDownloading.Current = (ulong)(i * 10);
			await Task.Delay(1000, cancellationToken);
		}

		this.UpdateDownloading.Complete();
		this.isUpdateReady.Value = true;
	}
}
