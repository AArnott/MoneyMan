using System;
using System.IO;
using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

public class MoneyFileFacts : IDisposable
{
    private readonly ITestOutputHelper logger;
    private string dbPath;

    public MoneyFileFacts(ITestOutputHelper logger)
    {
        this.logger = logger;
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
        using (var money = this.Load())
        {
            Assert.NotNull(money);
            Assert.True(File.Exists(this.dbPath));
        }
    }

    [Fact]
    public void Account_StoreReloadAndChange()
    {
        int accountKey;
        using (var money = this.Load())
        {
            var account = new Account { Name = "foo" };
            accountKey = money.Insert(account);
            Assert.Equal(accountKey, account.Id);
        }

        using (var money = this.Load())
        {
            var account = money.Accounts.First();
            Assert.Equal(accountKey, account.Id);
            Assert.Equal("foo", account.Name);
            account.Name = "bar";
            money.Update(account);
        }

        using (var money = this.Load())
        {
            Assert.Equal(1, money.Accounts.Count());
            var account = money.Get<Account>(accountKey);
            Assert.Equal(accountKey, account.Id);
            Assert.Equal("bar", account.Name);
        }
    }

    [Fact]
    public void GetNetWorth_WithAndWithoutAsOfDate()
    {
        using (var money = this.Load())
        {
            var acct1 = new Account { Name = "first" };
            var acct2 = new Account { Name = "second" };

            money.InsertAll(acct1, acct2);
            money.Insert(new Transaction { CreditAccountId = acct1.Id, When = DateTime.Parse("1/1/2015"), Amount = 7 });
            money.Insert(new Transaction { CreditAccountId = acct2.Id, When = DateTime.Parse("1/1/2016"), Amount = 3 });
            money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/1/2016"), Amount = 2.5m });
            money.Insert(new Transaction { DebitAccountId = acct1.Id, CreditAccountId = acct2.Id, When = DateTime.Parse("2/1/2016"), Amount = 1 });
            money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/1/2016 11:59 PM"), Amount = 1.3m });
            money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/2/2016"), Amount = 4m });
            money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/2/2222"), Amount = 0.3m });

            Assert.Equal(6.2m, money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Parse("2/1/2016") }));
            Assert.Equal(1.9m, money.GetNetWorth());
        }
    }

    [Fact]
    public void GetNetWorth_WithClosedAccounts()
    {
        using (var money = this.Load())
        {
            var openAccount = new Account { Name = "first" };
            var closedAccount = new Account { Name = "second", IsClosed = true };

            money.InsertAll(openAccount, closedAccount);
            money.Insert(openAccount.Deposit(7));
            money.Insert(closedAccount.Deposit(3));

            Assert.Equal(7, money.GetNetWorth());
            Assert.Equal(10, money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { IncludeClosedAccounts = true }));
        }
    }

    private MoneyFile Load()
    {
        var file = MoneyFile.Load(this.dbPath);
        file.Logger = new TestLoggerAdapter(this.logger);
        return file;
    }
}
