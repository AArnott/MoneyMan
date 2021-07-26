// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.Tests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;
    using Xunit;

    internal static class TestUtilities
    {
        internal static void AssertPropertyChangedEvent(INotifyPropertyChanged sender, Action trigger, params string[] expectedPropertiesChanged)
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
                trigger();
                Assert.Equal(expectedPropertiesChanged, actualPropertiesChanged);
            }
            finally
            {
                sender.PropertyChanged -= handler;
            }
        }
    }
}
