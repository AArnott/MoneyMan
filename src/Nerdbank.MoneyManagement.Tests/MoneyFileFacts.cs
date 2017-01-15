using System;
using System.IO;
using Nerdbank.MoneyManagement;
using Xunit;

public class MoneyFileFacts : IDisposable
{
    private string dbPath;

    public MoneyFileFacts()
    {
        this.dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }

    public void Dispose()
    {
        File.Delete(this.dbPath);
    }

    [Fact]
    public void Load_ThrowsOnNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => MoneyFile.Load(null));
        Assert.Throws<ArgumentException>(() => MoneyFile.Load(string.Empty));
    }

    [Fact]
    public void Load_NonExistentFile()
    {
        using (var money = MoneyFile.Load(this.dbPath))
        {
            Assert.NotNull(money);
            Assert.True(File.Exists(this.dbPath));
        }
    }
}
