using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;

public class EntityTestBase : IDisposable
{
    private readonly string dbPath;

    public EntityTestBase()
    {
        this.dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        this.Money = MoneyFile.Load(this.dbPath);
    }

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

    protected MoneyFile Money { get; set; }
}
