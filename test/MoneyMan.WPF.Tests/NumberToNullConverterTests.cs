// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Globalization;
using MoneyMan.Converters;
using Xunit;

public class NumberToNullConverterTests
{
	private readonly NumberOrNullConverter converter = new();

	[Fact]
	public void Convert()
	{
		Assert.Equal(3, this.converter.Convert(3, typeof(string), null, CultureInfo.InvariantCulture));
		Assert.Equal(string.Empty, this.converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture));
	}

	[Fact]
	public void ConvertBack()
	{
		Assert.Null(this.converter.ConvertBack(string.Empty, typeof(int?), null, CultureInfo.InvariantCulture));
		Assert.Equal(3, this.converter.ConvertBack("3", typeof(int?), null, CultureInfo.InvariantCulture));
		Assert.Equal("hi", this.converter.ConvertBack("hi", typeof(int?), null, CultureInfo.InvariantCulture));
	}
}
