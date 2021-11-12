// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

/// <summary>
/// An interface optionally implemented by <see cref="IComparer{T}"/> classes
/// to help sorted collections avoid a re-sort when properties change that would never impact sort order.
/// </summary>
/// <typeparam name="T">The type of element to be sorted.</typeparam>
public interface IOptimizedComparer<T> : IComparer<T>
{
	/// <summary>
	/// Gets a value indicating whether the named property is an input into the <see cref="IComparer{T}.Compare(T, T)"/> method.
	/// </summary>
	/// <param name="propertyName">The name of the property defined on <typeparamref name="T"/>.</param>
	/// <returns><see langword="true"/> if the property is used to determine sort order; otherwise <see langword="false"/>.</returns>
	bool IsPropertySignificant(string propertyName);
}
