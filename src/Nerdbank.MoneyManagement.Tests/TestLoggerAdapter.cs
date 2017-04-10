// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Validation;
using Xunit.Abstractions;

internal class TestLoggerAdapter : TextWriter
{
    private readonly ITestOutputHelper logger;

    public TestLoggerAdapter(ITestOutputHelper testLogger)
    {
        Requires.NotNull(testLogger, nameof(testLogger));
        this.logger = testLogger;
    }

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char ch)
    {
        throw new NotImplementedException();
    }

    public override void WriteLine()
    {
        this.logger.WriteLine(string.Empty);
    }

    public override void WriteLine(object value)
    {
        this.logger.WriteLine(value.ToString());
    }

    public override void WriteLine(string value)
    {
        this.logger.WriteLine(value);
    }

    public override void WriteLine(string format, params object[] args)
    {
        this.logger.WriteLine(format, args);
    }
}
