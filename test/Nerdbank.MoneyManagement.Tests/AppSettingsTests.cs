// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class AppSettingsTests : TestBase
{
	private static readonly LocalAppSettings NonDefaultSettings = new()
	{
		LastOpenedFile = "some.txt",
		ReopenLastFile = !new LocalAppSettings().ReopenLastFile,
	};

	public AppSettingsTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public async Task LocalAppSettings_RoundtripInMemory()
	{
		MemoryStream ms = new();
		await NonDefaultSettings.SaveAsync(ms, this.TimeoutToken);
		ms.Position = 0;
		this.LogStreamText(ms);
		LocalAppSettings? deserialized = await new LocalAppSettings().LoadAsync(ms, this.TimeoutToken);
		Assert.Equal(NonDefaultSettings, deserialized);
	}

	[Fact]
	public async Task TestSettings_RoundtripOnDisk()
	{
		string settingsPath = this.GenerateTemporaryFileName();
		TestSettings settings = new TestSettings(settingsPath) { Number = 5 };
		await settings.SaveAsync(this.TimeoutToken);

		Assert.True(File.Exists(settingsPath));
		using Stream settingsFileStream = File.OpenRead(settingsPath);
		this.LogStreamText(settingsFileStream);

		TestSettings deserialized = await new TestSettings(settingsPath).LoadAsync(this.TimeoutToken);
		Assert.Equal(settings.Number, deserialized.Number);
	}

	private void LogStreamText(Stream stream)
	{
		long position = stream.Position;
		StreamReader sr = new(stream);
		this.Logger.WriteLine(sr.ReadToEnd());
		stream.Position = position;
	}

	private record TestSettings : AppSettings
	{
		private string? settingsPath;

		internal TestSettings(string settingsPath)
		{
			this.settingsPath = settingsPath;
		}

		public TestSettings()
		{
		}

		protected override string FileName => "testsettings.json";

		protected override string SettingsPath => this.settingsPath ?? throw new InvalidOperationException("A deserialized instance doesn't know where to serialize again.");

		public int Number { get; init; }

		/// <inheritdoc cref="AppSettings.LoadAsync{T}(CancellationToken)"/>
		public async ValueTask<TestSettings> LoadAsync(CancellationToken cancellationToken) => await this.LoadAsync<TestSettings>(cancellationToken) ?? this;

		/// <inheritdoc cref="AppSettings.LoadAsync{T}(Stream, CancellationToken)"/>
		public async ValueTask<TestSettings> LoadAsync(Stream stream, CancellationToken cancellationToken) => await this.LoadAsync<TestSettings>(stream, cancellationToken) ?? this;
	}
}
