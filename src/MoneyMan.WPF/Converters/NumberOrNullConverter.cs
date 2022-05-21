// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;
using System.Windows.Data;

namespace MoneyMan.Converters;

public class NumberOrNullConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		// Converts an int? to a string.
		return value is null ? string.Empty : value;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		// Converts a string to an int?
		string? str = (string?)value;
		if (string.IsNullOrEmpty(str))
		{
			return null;
		}

		if (int.TryParse(str, NumberStyles.Integer, culture, out int result))
		{
			return result;
		}

		return value;
	}
}
