// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;

internal class MoneyFileTraceListener : XunitTraceListener
{
	private Dictionary<MoneyFile.EventType, int> queryCounters = new();

	internal MoneyFileTraceListener(ITestOutputHelper logger)
		: base(logger)
	{
	}

	internal IReadOnlyDictionary<MoneyFile.EventType, int> QueryCounters => this.queryCounters;

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id)
	{
		this.WriteLine(EventType(id));
		this.IncrementCounter(id);
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
	{
		this.WriteLine($"{EventType(id)} {message}");
		this.IncrementCounter(id);
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
	{
		string message = format is not null
			? (args is not null ? string.Format(CultureInfo.CurrentCulture, format, args) : format)
			: string.Empty;
		this.WriteLine($"{EventType(id)} {message}");
		this.IncrementCounter(id);
	}

	public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
	{
		if (data is string sql)
		{
			StringReader sr = new(sql.Trim());
			string? line;
			while ((line = sr.ReadLine()) is not null)
			{
				this.WriteLine($"\t{line}");
			}
		}
		else
		{
			base.TraceData(eventCache, source, eventType, id, data);
		}
	}

	internal void ResetCounters() => this.queryCounters.Clear();

	internal void LogCounters()
	{
		if (this.queryCounters.Count > 0)
		{
			this.WriteLine("SQL counter summary:");
			foreach (var counter in this.queryCounters)
			{
				this.WriteLine($"  {counter.Value,3} {counter.Key}");
			}
		}
	}

	private static string EventType(int id) => (MoneyFile.EventType)id switch
	{
		MoneyFile.EventType.InsertQuery => "INSERT",
		MoneyFile.EventType.DeleteQuery => "DELETE",
		MoneyFile.EventType.UpdateQuery => "UPDATE",
		MoneyFile.EventType.SelectQuery => "SELECT",
		MoneyFile.EventType.Sql => "SQL",
		_ => ((MoneyFile.EventType)id).ToString(),
	};

	private void IncrementCounter(int id)
	{
		MoneyFile.EventType type = (MoneyFile.EventType)id;
		this.queryCounters.TryGetValue(type, out int counter);
		this.queryCounters[type] = counter + 1;
	}
}
