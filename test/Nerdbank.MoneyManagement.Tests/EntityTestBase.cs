// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.IO;
using Nerdbank.MoneyManagement;
using Xunit.Abstractions;

public class EntityTestBase : TestBase
{
    private readonly string dbPath;

    public EntityTestBase(ITestOutputHelper logger)
        : base(logger)
    {
        this.dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        this.Money = MoneyFile.Load(this.dbPath);
        this.Money.Logger = new TestLoggerAdapter(this.Logger);
    }

    protected MoneyFile Money { get; set; }

    public void Dispose()
    {
        this.Money.Dispose();
        File.Delete(this.dbPath);
    }

    public T SaveAndReload<T>(T obj)
        where T : notnull, new()
    {
        int pk = this.Money.Insert(obj);
        return this.Money.Get<T>(pk);
    }
}
