// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using SQLite;
using Validation;
using static System.FormattableString;

namespace Nerdbank.MoneyManagement;

internal static class DatabaseSchemaUpgradeManager
{
	private static int latestVersion = GetLatestVersion();

	internal enum SchemaCompatibility
	{
		/// <summary>
		/// The database was created with an older version of the application. The database should be upgraded.
		/// </summary>
		RequiresDatabaseUpgrade = -1,

		/// <summary>
		/// The database has a schema that matches the current version of the application.
		/// </summary>
		VersionMatch = 0,

		/// <summary>
		/// The database was created with a newer version of the application. The application requires an upgrade before opening the database.
		/// </summary>
		RequiresAppUpgrade = 1,
	}

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

	internal static SchemaCompatibility IsUpgradeRequired(SQLiteConnection db)
	{
		int fileVersion = GetCurrentSchema(db);
		return (SchemaCompatibility)fileVersion.CompareTo(latestVersion);
	}

	/// <summary>
	/// Upgrades a database using a backup file to avoid corruption when an upgrade fails.
	/// </summary>
	/// <param name="path">The path to the database file to upgrade.</param>
	/// <exception cref="InvalidOperationException">Thrown when an upgrade failure occurs.</exception>
	internal static void Upgrade(string path)
	{
		// An upgrade is required. Copy the file elsewhere and execute the upgrade in another location
		// so we don't leave the database in a corrupted state if the upgrade goes badly.
		// Mitigating corruption with transactions doesn't work because we need to be able to enable/disable
		// pragma's that cannot be changed within a transaction during the upgrade.
		string tempPath = Path.GetTempFileName();
		File.Copy(path, tempPath, overwrite: true);
		try
		{
			int originalFileVersion;
			using (SQLiteConnection tempDb = new(tempPath))
			{
				Upgrade(tempDb, out originalFileVersion);
			}

			// We were successful. Rename the original file as a backup and move the successfully upgraded database to the original file.
			File.Move(path, Invariant($"{path}.v{originalFileVersion}.bak"), overwrite: true);
			File.Move(tempPath, path, overwrite: true);
		}
		catch
		{
			File.Delete(tempPath);
			throw;
		}
	}

	/// <inheritdoc cref="Upgrade(SQLiteConnection, int, out int)"/>
	internal static void Upgrade(SQLiteConnection db,  out int initialFileVersion) => Upgrade(db, latestVersion, out initialFileVersion);

	/// <summary>
	/// Upgrades a database in-place with no backup.
	/// </summary>
	/// <param name="db">The database to upgrade.</param>
	/// <param name="targetVersion">The schema version to upgrade to. Typically <see cref="latestVersion"/>.</param>
	/// <param name="initialFileVersion">Receives the original version of the file before the upgrade.</param>
	/// <exception cref="InvalidOperationException">Thrown when an upgrade failure occurs.</exception>
	internal static void Upgrade(SQLiteConnection db, int targetVersion, out int initialFileVersion)
	{
		initialFileVersion = GetCurrentSchema(db);

		// The upgrade process is one version at a time.
		// Every version from the past is represented in a case statement below with the required steps
		// to upgrade it to the subsequent version. We loop over each version till we reach the current schema.
		for (int nextVersion = initialFileVersion + 1; nextVersion <= targetVersion; nextVersion++)
		{
			// Complete each upgrade step within the context of a transaction.
			try
			{
				string sql = GetSqlUpgradeScript(nextVersion);
				ExecuteSql(db, sql);

				// Record the successful upgrade.
				db.Insert(new SchemaHistory
				{
					SchemaVersion = nextVersion,
					AppliedDateUtc = DateTime.UtcNow,
					AppVersion = ThisAssembly.AssemblyInformationalVersion,
				});
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to apply database schema version {nextVersion}. {ex.Message}", ex);
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
