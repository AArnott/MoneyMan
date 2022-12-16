// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public partial class AppSettingsTests : TestBase
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
		LocalAppSettings? deserialized = await new LocalAppSettings().LoadAsync<LocalAppSettings>(ms, this.TimeoutToken);
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

		TestSettings deserialized = await new TestSettings(settingsPath).LoadAsync<TestSettings>(this.TimeoutToken) ?? throw new Exception("Deserialize returned null.");
		Assert.Equal(settings.Number, deserialized.Number);
	}

	private void LogStreamText(Stream stream)
	{
		long position = stream.Position;
		StreamReader sr = new(stream);
		this.Logger.WriteLine(sr.ReadToEnd());
		stream.Position = position;
	}
}
