// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit.Abstractions;

public class MoneyTestBase : TestBase
{
	private readonly Lazy<MoneyFile> money;
	private Lazy<DocumentViewModel> documentViewModel;

	public MoneyTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.money = new Lazy<MoneyFile>(delegate
		{
			MoneyFile result = MoneyFile.Load(":memory:");
			result.Logger = new TestLoggerAdapter(this.Logger);
			return result;
		});
		this.documentViewModel = new Lazy<DocumentViewModel>(() => new DocumentViewModel(this.Money, ownsMoneyFile: false) { UserNotification = this.UserNotification });
	}

	protected MoneyFile Money => this.money.Value;

	protected DocumentViewModel DocumentViewModel => this.documentViewModel.Value;

	private protected UserNotificationMock UserNotification { get; } = new();

	protected virtual void ReloadViewModel()
	{
		if (this.documentViewModel.IsValueCreated is true)
		{
			this.documentViewModel.Value.Dispose();
			this.documentViewModel = new Lazy<DocumentViewModel>(() => new DocumentViewModel(this.Money, ownsMoneyFile: false));
		}
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
			if (this.documentViewModel.IsValueCreated)
			{
				this.documentViewModel.Value.Dispose();
			}

			if (this.money.IsValueCreated)
			{
				this.Money.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}
