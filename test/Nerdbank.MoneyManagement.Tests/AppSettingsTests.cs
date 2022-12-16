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
	public async Task SaveAndLoadRoundtrip()
	{
		MemoryStream ms = new();
		await NonDefaultSettings.SaveAsync(ms, this.TimeoutToken);
		ms.Position = 0;
		this.LogStreamText(ms);
		LocalAppSettings? deserialized = await AppSettings.LoadAsync<LocalAppSettings>(ms, this.TimeoutToken);
		Assert.Equal(NonDefaultSettings, deserialized);
	}

	[Fact]
	public async Task SaveAndLoadToAlternateLocation()
	{
		string expectedPath = Path.Join(Environment.GetFolderPath(TestSettings.Location), AppSettings.AppFolderName, TestSettings.FileName);
		if (File.Exists(expectedPath))
		{
			File.Delete(expectedPath);
		}

		TestSettings settings = new() { Number = 5 };
		await settings.SaveAsync(this.TimeoutToken);

		Assert.True(File.Exists(expectedPath));

		try
		{
			TestSettings deserialized = await AppSettings.LoadAsync<TestSettings>(this.TimeoutToken);
			Assert.Equal(settings, deserialized);
		}
		finally
		{
			File.Delete(expectedPath);
		}
	}

	private void LogStreamText(Stream stream)
	{
		long position = stream.Position;
		StreamReader sr = new(stream);
		this.Logger.WriteLine(sr.ReadToEnd());
		stream.Position = position;
	}

	private record TestSettings : IAppSettings
	{
		public static string FileName => "testsettings.json";

		public static Environment.SpecialFolder Location => Environment.SpecialFolder.LocalApplicationData;

		public int Number { get; init; }
	}
}
