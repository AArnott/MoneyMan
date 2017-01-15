using Nerdbank.MoneyManagement;
using System;
using Xunit;

public class AccountFacts
{
    private Account account;

    public AccountFacts()
    {
        this.account = new Account();
    }

    [Fact]
    public void Name_GetSet()
    {
        Assert.Null(this.account.Name);
        string expected = "some name";
        this.account.Name = expected;
        Assert.Equal(expected, this.account.Name);
    }
}
