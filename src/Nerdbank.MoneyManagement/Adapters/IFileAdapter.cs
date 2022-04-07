// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.Adapters;

/// <summary>
/// Describes a file import class.
/// </summary>
public interface IFileAdapter
{
	/// <summary>
	/// Gets the type of file that this adapter can communicate with.
	/// </summary>
	IReadOnlyList<AdapterFileType> FileTypes { get; }

	/// <summary>
	/// Imports a file into the current document.
	/// </summary>
	/// <param name="filePath">The path to the file to be imported.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The number of imported records.</returns>
	Task<int> ImportAsync(string filePath, CancellationToken cancellationToken);
}

/// <summary>
/// Describes a type of file that may be suppored by an <see cref="IFileAdapter"/>.
/// </summary>
/// <param name="DisplayName">A human recognizable name for the file type.</param>
/// <param name="FileExtensions">The extensions that this file may use, without the leading period.</param>
public record AdapterFileType(string DisplayName, IReadOnlyList<string> FileExtensions);
