// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;
using System.Windows.Data;

namespace MoneyMan.Converters;

internal class EnumToOrdinalConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (targetType == typeof(int))
		{
			return (int)value;
		}
		else
		{
			throw new NotSupportedException();
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Enum.ToObject(targetType, value);
	}
}
