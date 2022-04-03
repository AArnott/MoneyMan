// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class MoneyTestBase : TestBase
{
	private readonly Lazy<MoneyFile> money;

	public MoneyTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.money = new Lazy<MoneyFile>(delegate
		{
			MoneyFile result = MoneyFile.Load(":memory:");
			result.Logger = new TestLoggerAdapter(this.Logger);
			return result;
		});
		this.UserNotification = new UserNotificationMock(logger);
		this.MainPageViewModel = new();
	}

	protected MoneyFile Money => this.money.Value;

	protected MainPageViewModelBase MainPageViewModel { get; }

	protected DocumentViewModel DocumentViewModel
	{
		get
		{
			if (this.MainPageViewModel.Document is null)
			{
				this.MainPageViewModel.Document = new DocumentViewModel(this.Money, ownsMoneyFile: false) { UserNotification = this.UserNotification };
			}

			return this.MainPageViewModel.Document;
		}
	}

	private protected UserNotificationMock UserNotification { get; }

	protected void LoadDocument() => _ = this.DocumentViewModel;

	protected virtual void ReloadViewModel()
	{
		this.MainPageViewModel.ReplaceViewModel(null);
	}

	protected void AssertNowAndAfterReload(Action assertions)
	{
		assertions();
		this.ReloadViewModel();
		assertions();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.MainPageViewModel.Document?.Dispose();
			if (this.money.IsValueCreated)
			{
				this.Money.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}
