// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

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

        Payee? p2 = this.SaveAndReload(p);

        Assert.Equal(name, p2.Name);
    }
}
