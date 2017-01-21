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

public class EntityTestBase : IDisposable
{
    private readonly string dbPath;
    private readonly ITestOutputHelper logger;

    public EntityTestBase(ITestOutputHelper logger)
    {
        this.logger = logger;
        this.dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        this.Money = MoneyFile.Load(this.dbPath);
        this.Money.Logger = new TestLoggerAdapter(this.logger);
    }

    protected MoneyFile Money { get; set; }

    public void Dispose()
    {
        this.Money.Dispose();
        File.Delete(this.dbPath);
    }

    public T SaveAndReload<T>(T obj)
        where T : new()
    {
        int pk = this.Money.Insert(obj);
        return this.Money.Get<T>(pk);
    }
}
