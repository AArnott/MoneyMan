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

    [Fact]
    public void Account_StoreReloadAndChange()
    {
        int accountKey;
        using (var money = MoneyFile.Load(this.dbPath))
        {
            var account = new Account { Name = "foo" };
            accountKey = money.Insert(account);
            Assert.Equal(accountKey, account.Id);
        }

        using (var money = MoneyFile.Load(this.dbPath))
        {
            var account = money.Accounts.First();
            Assert.Equal(accountKey, account.Id);
            Assert.Equal("foo", account.Name);
            account.Name = "bar";
            money.Update(account);
        }

        using (var money = MoneyFile.Load(this.dbPath))
        {
            Assert.Equal(1, money.Accounts.Count());
            var account = money.Get<Account>(accountKey);
            Assert.Equal(accountKey, account.Id);
            Assert.Equal("bar", account.Name);
        }
    }
}
