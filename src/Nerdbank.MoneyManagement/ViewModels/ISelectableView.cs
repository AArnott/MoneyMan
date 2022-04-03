// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

/// <summary>
/// An interface that understands how to select a view.
/// </summary>
public interface ISelectableView
{
	/// <summary>
	/// Makes a best effort to select a given entity in the app.
	/// </summary>
	/// <remarks>
	/// Implementations should never use the original view model except to extract its ID, because it may be a view model from a deleted and resurrected entity,
	/// and therefore a new view model exists to represent it.
	/// </remarks>
	void Select();
}
