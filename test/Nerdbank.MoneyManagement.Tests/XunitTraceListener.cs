// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

public class XunitTraceListener : TraceListener
{
	private readonly ITestOutputHelper logger;
	private readonly StringBuilder lineInProgress = new StringBuilder();
	private bool disposed;

	public XunitTraceListener(ITestOutputHelper logger)
	{
		this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public override bool IsThreadSafe => false;

	public bool HasLoggedErrors { get; private set; }

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id)
	{
		this.HasLoggedErrors |= eventType == TraceEventType.Error;
		base.TraceEvent(eventCache, source, eventType, id);
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
	{
		this.HasLoggedErrors |= eventType == TraceEventType.Error;
		base.TraceEvent(eventCache, source, eventType, id, message);
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string format, params object?[]? args)
	{
		this.HasLoggedErrors |= eventType == TraceEventType.Error;
		base.TraceEvent(eventCache, source, eventType, id, format, args);
	}

	public override void Write(string? message) => this.lineInProgress.Append(message);

	public override void WriteLine(string? message)
	{
		if (!this.disposed)
		{
			this.lineInProgress.Append(message);
			this.logger.WriteLine(this.lineInProgress.ToString());
			this.lineInProgress.Clear();
		}
	}

	protected override void Dispose(bool disposing)
	{
		this.disposed = true;
		base.Dispose(disposing);
	}
}
