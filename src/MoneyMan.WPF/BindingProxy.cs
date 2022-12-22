// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows;

namespace MoneyMan;

/// <summary>
/// A WPF element that can be used is a ResourceDictionary to help DataGrid columns to databind on something in their context.
/// </summary>
/// <remarks>
/// Inspired by <see href="https://thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/">this blog post</see>.
/// </remarks>
public class BindingProxy : Freezable
{
	// Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty DataProperty =
		DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value to serve as a data binding source for another element.
	/// </summary>
	public object Data
	{
		get { return (object)this.GetValue(DataProperty); }
		set { this.SetValue(DataProperty, value); }
	}

	protected override Freezable CreateInstanceCore()
	{
		return new BindingProxy();
	}
}
