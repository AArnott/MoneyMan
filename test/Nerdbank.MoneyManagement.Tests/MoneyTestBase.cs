// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Xunit.Abstractions;

public class MoneyTestBase : TestBase
{
	private Lazy<MoneyFile> money;

	public MoneyTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.money = new Lazy<MoneyFile>(() =>
		{
			MoneyFile result = MoneyFile.Load(this.GenerateTemporaryFileName());
			result.Logger = new TestLoggerAdapter(this.Logger);
			return result;
		});
	}

	protected MoneyFile Money => this.money.Value;

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
