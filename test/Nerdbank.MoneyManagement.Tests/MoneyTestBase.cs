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
	private readonly string dbPath;

	public MoneyTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		this.Money = MoneyFile.Load(this.dbPath);
		this.Money.Logger = new TestLoggerAdapter(this.Logger);
	}

	protected MoneyFile Money { get; set; }

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.Money.Dispose();
			File.Delete(this.dbPath);
		}

		base.Dispose(disposing);
	}
}
