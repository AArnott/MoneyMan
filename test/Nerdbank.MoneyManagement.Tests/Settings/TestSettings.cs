// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

internal record TestSettings : AppSettings
{
	private string? settingsPath;

	public TestSettings()
	{
	}

	internal TestSettings(string settingsPath)
	{
		this.settingsPath = settingsPath;
	}

	public int Number { get; init; }

	internal int SaveCount { get; private set; }

	protected override string FileName => "testsettings.json";

	protected override string SettingsPath => this.settingsPath ?? throw new InvalidOperationException("A deserialized instance doesn't know where to serialize again.");

	/// <inheritdoc cref="AppSettings.LoadAsync{T}(CancellationToken)"/>
	public override async ValueTask<T?> LoadAsync<T>(Stream stream, CancellationToken cancellationToken)
		where T : class
	{
		TestSettings? deserialized = (TestSettings?)(object?)await base.LoadAsync<T>(stream, cancellationToken);
		if (deserialized is not null)
		{
			deserialized.settingsPath = this.settingsPath;
			return (T?)(object?)deserialized;
		}

		return null;
	}

	public override Task SaveAsync(Stream stream, CancellationToken cancellationToken)
	{
		this.SaveCount++;
		return base.SaveAsync(stream, cancellationToken);
	}
}
