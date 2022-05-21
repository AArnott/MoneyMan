// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Validation;

namespace Nerdbank.MoneyManagement;

public static class IntegrityChecks
{
	/// <summary>
	/// Checks the data file for any integrity issues.
	/// </summary>
	/// <param name="file">The data file to scan.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A list of the issues found.</returns>
	public static IReadOnlyList<Issue> CheckIntegrity(this MoneyFile file, CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(file);
		List<Issue> issues = new();
		return issues;
	}

	public abstract class Issue
	{
		internal Issue(string message)
		{
			this.Message = message;
		}

		public string Message { get; internal set; }
	}
}
