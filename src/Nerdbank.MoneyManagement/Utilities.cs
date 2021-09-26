// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using System;
	using System.ComponentModel;

	internal static class Utilities
	{
		/// <summary>
		/// Attaches a handler of <see cref="INotifyPropertyChanged.PropertyChanged"/> events that will be
		/// automatically removed when the handler returns <see langword="true" />.
		/// </summary>
		/// <param name="this">The object to watch for property changed events.</param>
		/// <param name="listener">The handler.</param>
		internal static void ObservePropertyChangesUntil(this INotifyPropertyChanged @this, Func<object?, PropertyChangedEventArgs, bool> listener)
		{
			@this.PropertyChanged += Handler;

			void Handler(object? sender, PropertyChangedEventArgs e)
			{
				if (listener(sender, e))
				{
					@this.PropertyChanged -= Handler;
				}
			}
		}

		/// <summary>
		/// Uses <see cref="INotifyPropertyChanged"/> to watch an object until data validation is satisfied,
		/// then invokes a callback.
		/// </summary>
		/// <typeparam name="T">The type of object to be watched.</typeparam>
		/// <param name="this">The object to watch for property changed events.</param>
		/// <param name="callback">The handler, which is given the <paramref name="this"/> argument.</param>
		internal static void NotifyWhenValid<T>(this T @this, Action<T> callback)
			where T : INotifyPropertyChanged, IDataErrorInfo
		{
			ObservePropertyChangesUntil(
				@this,
				(s, e) =>
				{
					if (string.IsNullOrEmpty(@this.Error))
					{
						callback(@this);
						return true;
					}

					return false;
				});
		}
	}
}
