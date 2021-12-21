// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Nerdbank.MoneyManagement;

internal class NonZeroAttribute : ValidationAttribute
{
	public NonZeroAttribute()
		: base("This value must be non-zero.")
	{
	}

	public override bool IsValid(object? value)
	{
		if (value is Enum)
		{
			if (Enum.GetUnderlyingType(value.GetType()) == typeof(int))
			{
				value = (int)value;
			}
		}

		return value switch
		{
			int num => num != 0,
			decimal real => real != 0,
			_ => false,
		};
	}
}
