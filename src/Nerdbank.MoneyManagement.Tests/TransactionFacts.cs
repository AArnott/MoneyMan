using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Xunit;

public class TransactionFacts : EntityTestBase
{
    [Fact]
    public void BasicPropertiesSerialization()
    {
        DateTime when = DateTime.Now;
        decimal amount = 5.2398345m;

        var t = new Transaction
        {
            When = when,
            Amount = amount,
        };

        Assert.Equal(when, t.When);
        Assert.Equal(amount, t.Amount);

        var t2 = this.SaveAndReload(t);
        
        Assert.NotEqual(0, t.Id);
        Assert.Equal(t.Id, t2.Id);
        Assert.Equal(when, t2.When);
        Assert.Equal(amount, t2.Amount);
    }
}
