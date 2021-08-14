// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Nerdbank.MoneyManagement.ViewModels;
	using PCLCommandBase;

	public class AccountsPanelViewModel : BindableBase
	{
		private ObservableCollection<AccountViewModel> accounts = new ObservableCollection<AccountViewModel>();
		private AccountViewModel? selectedAccount;

		public ObservableCollection<AccountViewModel> Accounts
		{
			get => this.accounts;
			set => this.SetProperty(ref this.accounts, value);
		}

		public AccountViewModel? SelectedAccount
		{
			get => this.selectedAccount;
			set => this.SetProperty(ref this.selectedAccount, value);
		}
	}
}
