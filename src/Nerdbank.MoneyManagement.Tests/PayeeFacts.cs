using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

public class PayeeFacts : EntityTestBase
{
    public PayeeFacts(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public void BasicPropertiesSerialization()
    {
        const string name = "Some name";
        var p = new Payee
        {
            Name = name,
        };

        Assert.Equal(name, p.Name);

        var p2 = this.SaveAndReload(p);

        Assert.Equal(name, p2.Name);
    }
}
