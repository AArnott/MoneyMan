// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Generic;
using Nerdbank.MoneyManagement;
using Xunit;
using Xunit.Abstractions;

public class UtilitiesTests : TestBase
{
	public UtilitiesTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void BinarySearch_Empty()
	{
		List<int> list = new(0);
		Assert.Equal(list.BinarySearch(1), Utilities.BinarySearch(list, 1));
	}

	[Fact]
	public void BinarySearch_VaryingLengthsAndIndexes()
	{
		const int MaxLength = 5;
		List<int> list = new(MaxLength);
		for (int length = 1; length <= MaxLength; length++)
		{
			list.Clear();
			for (int i = 0; i < length; i++)
			{
				list.Add(1 + (i * 2));
			}

			this.Logger.WriteLine("Testing with a length of: {0}", length);
			for (int value = list[0] - 1; value <= list[^1] + 1; value++)
			{
				this.Logger.WriteLine($"Searching for a position for {value} amongst: {{ {string.Join(", ", list)} }}");
				Assert.Equal(list.BinarySearch(value), Utilities.BinarySearch(list, value));
			}
		}
	}
}
