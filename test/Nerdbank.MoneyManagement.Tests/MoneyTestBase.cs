// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit.Abstractions;

public class MoneyTestBase : TestBase
{
	private readonly Lazy<MoneyFile> money;
	private readonly Lazy<DocumentViewModel> documentViewModel;

	public MoneyTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.money = new Lazy<MoneyFile>(delegate
		{
			MoneyFile result = MoneyFile.Load(":memory:");
			result.Logger = new TestLoggerAdapter(this.Logger);
			return result;
		});
		this.documentViewModel = new Lazy<DocumentViewModel>(() => new DocumentViewModel(this.Money));
	}

	protected MoneyFile Money => this.money.Value;

	protected DocumentViewModel DocumentViewModel => this.documentViewModel.Value;

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (this.money.IsValueCreated)
			{
				this.Money.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}
