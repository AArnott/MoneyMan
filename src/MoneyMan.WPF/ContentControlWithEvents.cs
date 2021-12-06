// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows.Controls;

namespace MoneyMan;

public class ContentControlWithEvents : ContentControl
{
	public event EventHandler? TemplateApplied;

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		this.TemplateApplied?.Invoke(this, EventArgs.Empty);
	}
}
