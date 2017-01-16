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
}
