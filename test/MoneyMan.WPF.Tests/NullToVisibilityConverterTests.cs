// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Globalization;
using System.Windows;
using MoneyMan.Converters;
using Xunit;

public class NullToVisibilityConverterTests
{
	private readonly NullToVisibilityConverter converter = new();

	[Fact]
	public void Convert()
	{
		Assert.Equal(Visibility.Collapsed, this.converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
		Assert.Equal(Visibility.Visible, this.converter.Convert(3, typeof(Visibility), null, CultureInfo.InvariantCulture));
		Assert.Equal(Visibility.Visible, this.converter.Convert("hi", typeof(Visibility), null, CultureInfo.InvariantCulture));
		Assert.Equal(Visibility.Visible, this.converter.Convert(string.Empty, typeof(Visibility), null, CultureInfo.InvariantCulture));
	}

	[Fact]
	public void ConvertBack()
	{
		// If this method on the converter is ever implemented, this should be replaced with tests of that functionality.
		Assert.Throws<NotImplementedException>(() => this.converter.ConvertBack(Visibility.Collapsed, typeof(object), null, CultureInfo.InvariantCulture));
	}
}
