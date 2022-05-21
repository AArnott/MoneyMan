// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

public interface IPresentedWindowViewModel
{
	/// <summary>
	/// Occurs when the view model wants the view to close.
	/// </summary>
	public event EventHandler? Closing;
}
