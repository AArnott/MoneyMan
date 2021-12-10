// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MoneyMan.Converters;

public class TypeToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (parameter is Type type)
		{
			return type.IsInstanceOfType(value)
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		throw new ArgumentException($"{nameof(parameter)} is expected to be an instance of System.Type.");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
