// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft;

namespace MoneyMan.Converters;

public class TypeToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		Type? testType = parameter as Type;
		Requires.Argument(testType is object, nameof(parameter), "Converter parameter must be a Type.");
		if (targetType != typeof(Visibility))
		{
			throw new NotSupportedException("This converter only produces " + nameof(Visibility) + " value.");
		}

		return testType.IsInstanceOfType(value) ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
