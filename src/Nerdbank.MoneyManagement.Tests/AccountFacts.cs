using Nerdbank.MoneyManagement;
using System;
using Xunit;

public class AccountFacts : EntityTestBase
{
    private Account account;

    public AccountFacts()
    {
        this.account = new Account();
    }

    [Fact]
    public void BasicPropertiesSerialization()
    {
        Assert.Null(this.account.Name);
        string expected = "some name";
        this.account.Name = expected;

        Assert.Equal(expected, this.account.Name);

        var account2 = this.SaveAndReload(this.account);

        Assert.NotEqual(0, this.account.Id);
        Assert.Equal(this.account.Id, account2.Id);
        Assert.Equal(expected, account2.Name);
    }

    [Fact]
    public void GetBalance_AcrossTransactions()
    {
        this.Money.Insert(this.account);

        decimal expected = 0;
        Assert.Equal(expected, this.Money.GetBalance(this.account));

        this.Money.Insert(this.account.Deposit(5));
        expected += 5;
        Assert.Equal(expected, this.Money.GetBalance(this.account));

        this.Money.Insert(this.account.Withdraw(2.5m));
        expected -= 2.5m;
        Assert.Equal(expected, this.Money.GetBalance(account));
    }

    [Fact]
    public void GetBalance_AcrossAccounts()
    {
        var account2 = new Account();
        this.Money.Insert(this.account);
        this.Money.Insert(account2);

        this.Money.Insert(this.account.Deposit(5));
        this.Money.Insert(this.account.Transfer(account2, 2.2m));

        Assert.Equal(2.8m, this.Money.GetBalance(this.account));
        Assert.Equal(2.2m, this.Money.GetBalance(account2));

        this.Money.Insert(account2.Transfer(this.account, 0.4m));

        Assert.Equal(3.2m, this.Money.GetBalance(this.account));
        Assert.Equal(1.8m, this.Money.GetBalance(account2));
    }
}
