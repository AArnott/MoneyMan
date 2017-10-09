// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.Tests.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Nerdbank.MoneyManagement.ViewModels;
    using Xunit;
    using Xunit.Abstractions;

    public class MainPageViewModelTests
    {
        private readonly ITestOutputHelper logger;
        private MainPageViewModel viewModel = new MainPageViewModel();

        public MainPageViewModelTests(ITestOutputHelper logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Fact]
        public void AccountsPanel_NotNull()
        {
            Assert.NotNull(this.viewModel.AccountsPanel);
        }

        [Fact]
        public void AccountsPanel_PropertyChanged()
        {
            int eventRaised = 0;
            this.viewModel.PropertyChanged += (s, e) =>
            {
                Assert.Equal(nameof(this.viewModel.AccountsPanel), e.PropertyName);
                eventRaised++;
            };
            this.viewModel.AccountsPanel = new AccountsPanelViewModel();
            Assert.Equal(1, eventRaised);
        }
    }
}
