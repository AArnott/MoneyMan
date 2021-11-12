// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using SQLite;
using Validation;

namespace Nerdbank.MoneyManagement;

internal static class DatabaseSchemaUpgradeManager
{
	private static int latestVersion = GetLatestVersion();

	internal static int GetCurrentSchema(SQLiteConnection db)
	{
		if (db.GetTableInfo(nameof(SchemaHistory)).Count == 0)
		{
			return 0;
		}

		TableQuery<SchemaHistory> table = db.Table<SchemaHistory>();
		if (!table.Any())
		{
			return 0;
		}

		return table.Max(s => s.SchemaVersion);
	}

	internal static bool IsSchemaCurrent(SQLiteConnection db) => GetCurrentSchema(db) == latestVersion;

	internal static void Upgrade(SQLiteConnection db)
	{
		// The version of the file is assumed to match the highest schema version this db has ever been.
		int initialFileVersion = GetCurrentSchema(db);

		// The upgrade process is one version at a time.
		// Every version from the past is represented in a case statement below with the required steps
		// to upgrade it to the subsequent version. We loop over each version till we reach the current schema.
		for (int targetVersion = initialFileVersion + 1; targetVersion <= latestVersion; targetVersion++)
		{
			// Complete each upgrade step within the context of a transaction.
			db.BeginTransaction();
			try
			{
				string sql = GetSqlUpgradeScript(targetVersion);
				ExecuteSql(db, sql);

				// Record the successful upgrade.
				db.Insert(new SchemaHistory
				{
					SchemaVersion = targetVersion,
					AppliedDateUtc = DateTime.UtcNow,
					AppVersion = ThisAssembly.AssemblyInformationalVersion,
				});
				db.Commit();
			}
			catch (Exception ex)
			{
				db.Rollback();
				throw new InvalidOperationException($"Failed to apply database schema version {targetVersion}.", ex);
			}
		}
	}

	private static void ExecuteSql(SQLiteConnection db, string sql)
	{
		int result = SQLitePCL.raw.sqlite3_exec(db.Handle, sql);
		if (result is not SQLitePCL.raw.SQLITE_OK or SQLitePCL.raw.SQLITE_DONE)
		{
			string errMsg = SQLitePCL.raw.sqlite3_errmsg(db.Handle).utf8_to_string();
			throw new Exception(errMsg);
		}
	}

	private static string GetSqlUpgradeScript(int targetSchemaVersion)
	{
		string sqlFileResourceName = $"{ThisAssembly.RootNamespace}.SchemaUpgradeScripts.{targetSchemaVersion}.sql";
		using Stream? sqlScriptStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(sqlFileResourceName);
		Assumes.NotNull(sqlScriptStream);
		using StreamReader sqlScriptStreamReader = new(sqlScriptStream);
		string sql = sqlScriptStreamReader.ReadToEnd();
		return sql;
	}

	private static int GetLatestVersion()
	{
		string startsWith = $"{ThisAssembly.RootNamespace}.SchemaUpgradeScripts.";
		string endsWith = ".sql";
		int latestVersion = (from name in Assembly.GetExecutingAssembly().GetManifestResourceNames()
							 where name.StartsWith(startsWith, StringComparison.Ordinal) && name.EndsWith(endsWith, StringComparison.Ordinal)
							 let version = int.Parse(name.Substring(startsWith.Length, name.Length - startsWith.Length - endsWith.Length), CultureInfo.InvariantCulture)
							 select version).Max();
		return latestVersion;
	}
}
