// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using PCLCommandBase;

	public class MainPageViewModel : BindableBase
	{
		private AccountsPanelViewModel accountsPanel = new AccountsPanelViewModel();

		public AccountsPanelViewModel AccountsPanel
		{
			get => this.accountsPanel;
			set => this.SetProperty(ref this.accountsPanel, value);
		}
	}
}
