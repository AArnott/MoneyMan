// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class PersistentAppSettingsTests : TestBase
{
	private readonly string settingsPath;
	private readonly TestSettings initialSettings;

	public PersistentAppSettingsTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.settingsPath = this.GenerateTemporaryFileName();
		this.initialSettings = new TestSettings(this.settingsPath);
	}

	[Fact]
	public async Task NoSaveWithoutChanges()
	{
		PersistentAppSettings<TestSettings> persistent = new(this.initialSettings);
		Assert.Same(this.initialSettings, persistent.Value);
		persistent.Value = persistent.Value with { Number = this.initialSettings.Number };
		await persistent.DisposeAsync();
		Assert.False(File.Exists(this.settingsPath));
	}

	[Fact]
	public async Task AutoSave()
	{
		PersistentAppSettings<TestSettings> persistent = new(this.initialSettings);
		persistent.Value = persistent.Value with { Number = 5 };
		while (!File.Exists(this.settingsPath))
		{
			await Task.Delay(10, this.TimeoutToken);
		}

		await persistent.DisposeAsync();
	}

	[Fact]
	public async Task AutoSaveWithManyChanges()
	{
		PersistentAppSettings<TestSettings> persistent = new(this.initialSettings);
		for (int i = 1; i <= 150; i++)
		{
			persistent.Value = persistent.Value with { Number = i };
		}

		await persistent.DisposeAsync();
		this.Logger.WriteLine($"Settings saved {persistent.Value.SaveCount} times.");
		Assert.NotEqual(150, persistent.Value.SaveCount); // We should *not* have saved it (synchronously) every time.

		persistent = new PersistentAppSettings<TestSettings>(this.initialSettings);
		await persistent.LoadAsync(this.TimeoutToken);
		Assert.Equal(150, persistent.Value.Number);
	}

	[Fact]
	public async Task SaveOnDisposalAndReload()
	{
		PersistentAppSettings<TestSettings> persistent = new(this.initialSettings);
		persistent.Value = persistent.Value with { Number = 5 };
		await persistent.DisposeAsync();
		Assert.True(File.Exists(this.settingsPath));

		persistent = new PersistentAppSettings<TestSettings>(this.initialSettings);
		Assert.NotEqual(5, persistent.Value.Number);
		await persistent.LoadAsync(this.TimeoutToken);
		Assert.Equal(5, persistent.Value.Number);
	}

	[Fact]
	public async Task NoAutoSaveWhenDisabled()
	{
		PersistentAppSettings<TestSettings> persistent = new(this.initialSettings, autoSave: false);
		persistent.Value = persistent.Value with { Number = 5 };
		await persistent.DisposeAsync();
		Assert.False(File.Exists(this.settingsPath));
	}
}
