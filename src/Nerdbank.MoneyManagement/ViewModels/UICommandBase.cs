// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

/// <summary>
/// A base type for a command that is intended for display to the user.
/// </summary>
public abstract class UICommandBase : CommandBase
{
	public abstract string Caption { get; }

	public virtual string? InputGestureText => null;

	public virtual bool Visible => true;
}
