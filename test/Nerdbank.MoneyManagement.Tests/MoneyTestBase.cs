// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class MoneyTestBase : TestBase
{
	public const int DefaultCategoryCount = 1;

	public const string DefaultCommissionCategoryName = "Commission";

	private readonly Lazy<MoneyFile> money;

	public MoneyTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.MoneyFileTraceListener = new MoneyFileTraceListener(this.Logger);
		this.money = new Lazy<MoneyFile>(delegate
		{
			MoneyFile result = MoneyFile.Load(":memory:");
			result.TraceSource.Listeners.Add(this.MoneyFileTraceListener);
			return result;
		});
		this.UserNotification = new UserNotificationMock(logger);
		this.MainPageViewModel = new();
	}

	internal MoneyFileTraceListener MoneyFileTraceListener { get; }

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

	protected void EnableSqlLogging()
	{
		this.Money.TraceSource.Switch.Level = SourceLevels.Verbose;
	}

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
			this.MoneyFileTraceListener.LogCounters();
			this.MainPageViewModel.Document?.Dispose();
			if (this.money.IsValueCreated)
			{
				this.Money.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}
