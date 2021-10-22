// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using Microsoft;
	using Xunit;

	internal static class TestUtilities
	{
		internal static void AssertPropertyChangedEvent(INotifyPropertyChanged sender, Action trigger, params string[] expectedPropertiesChanged)
		{
			AssertPropertyChangedEvent(sender, trigger, invertExpectation: false, expectedPropertiesChanged);
		}

		internal static void AssertPropertyChangedEvent(INotifyPropertyChanged sender, Action trigger, bool invertExpectation, params string[] expectedPropertiesChanged)
		{
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
			AssertPropertyChangedEventAsync(
				sender,
				() =>
				{
					trigger();
					return Task.CompletedTask;
				},
				invertExpectation,
				expectedPropertiesChanged).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
		}

		internal static NotifyCollectionChangedEventArgs AssertCollectionChangedEvent(INotifyCollectionChanged sender, Action trigger)
		{
			NotifyCollectionChangedEventArgs? args = null;
			sender.CollectionChanged += Handler;
			try
			{
				trigger();
				if (args is null)
				{
					Assert.True(false, "Expected event was not raised.");
				}

				return args!;
			}
			finally
			{
				sender.CollectionChanged -= Handler;
			}

			void Handler(object? eventSender, NotifyCollectionChangedEventArgs e)
			{
				Assert.Same(sender, eventSender);
				args = e;
			}
		}

		internal static void AssertCommandCanExecuteChanged(ICommand command, Action trigger)
		{
			bool raised = false;
			command.CanExecuteChanged += Handler;
			try
			{
				trigger();
				Assert.True(raised);
			}
			finally
			{
				command.CanExecuteChanged -= Handler;
			}

			void Handler(object? sender, EventArgs args)
			{
				Assert.Same(command, sender);
				raised = true;
			}
		}

		internal static void AssertNoCollectionChangedEvent(INotifyCollectionChanged sender, Action trigger)
		{
			bool raised = false;
			sender.CollectionChanged += Handler;
			try
			{
				trigger();
				Assert.False(raised, "Unexpected event was raised.");
			}
			finally
			{
				sender.CollectionChanged -= Handler;
			}

			void Handler(object? eventSender, NotifyCollectionChangedEventArgs e)
			{
				raised = true;
			}
		}

		internal static Task AssertPropertyChangedEventAsync(INotifyPropertyChanged sender, Func<Task> trigger, params string[] expectedPropertiesChanged)
		{
			return AssertPropertyChangedEventAsync(sender, trigger, invertExpectation: false, expectedPropertiesChanged);
		}

		internal static async Task AssertPropertyChangedEventAsync(INotifyPropertyChanged sender, Func<Task> trigger, bool invertExpectation, params string[] expectedPropertiesChanged)
		{
			Requires.NotNull(sender, nameof(sender));
			Requires.NotNull(trigger, nameof(trigger));
			Requires.NotNull(expectedPropertiesChanged, nameof(expectedPropertiesChanged));

			var actualPropertiesChanged = new HashSet<string>(StringComparer.Ordinal);
			PropertyChangedEventHandler handler = (s, e) =>
			{
				Assert.Same(sender, s);
				Assumes.NotNull(e.PropertyName);
				actualPropertiesChanged.Add(e.PropertyName);
			};

			sender.PropertyChanged += handler;
			try
			{
				await trigger();
				if (invertExpectation)
				{
					Assert.DoesNotContain(actualPropertiesChanged, expectedPropertiesChanged.Contains);
				}
				else
				{
					Assert.Subset(actualPropertiesChanged, expectedPropertiesChanged.ToHashSet(StringComparer.Ordinal));
				}
			}
			finally
			{
				sender.PropertyChanged -= handler;
			}
		}

		internal static void AssertRaises(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, Action trigger)
		{
			AssertRaisesAsync(addHandler, removeHandler, () =>
			{
				trigger();
				return Task.CompletedTask;
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
			}).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
		}

		internal static async Task AssertRaisesAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, Func<Task> trigger)
		{
			bool raised = false;
			addHandler(Handler);
			try
			{
				await trigger();
				Assert.True(raised, "Expected event not raised.");
			}
			finally
			{
				removeHandler(Handler);
			}

			void Handler(object? sender, EventArgs args)
			{
				raised = true;
			}
		}
	}
}
