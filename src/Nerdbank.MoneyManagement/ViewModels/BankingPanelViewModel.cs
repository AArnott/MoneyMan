// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using PCLCommandBase;

	public class BankingPanelViewModel : BindableBase
	{
		private ObservableCollection<AccountViewModel> accounts = new();
		private List<AccountViewModel> closedAccounts = new();
		private AccountViewModel? selectedAccount;

		public IReadOnlyList<AccountViewModel> Accounts => this.accounts;

		public AccountViewModel? SelectedAccount
		{
			get => this.selectedAccount;
			set => this.SetProperty(ref this.selectedAccount, value);
		}

		internal void Add(AccountViewModel account)
		{
			if (account.IsClosed)
			{
				this.closedAccounts.Add(account);
			}
			else
			{
				this.accounts.AddInSortOrder(account, AccountSort.Instance);
			}

			account.PropertyChanged += this.Account_PropertyChanged;
		}

		internal void Remove(AccountViewModel account)
		{
			if (!this.accounts.Remove(account))
			{
				this.closedAccounts.Remove(account);
			}

			account.PropertyChanged -= this.Account_PropertyChanged;
		}

		private void Account_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var account = (AccountViewModel)sender!;
			switch (e.PropertyName)
			{
				case nameof(AccountViewModel.IsClosed):
					if (account.IsClosed)
					{
						this.accounts.Remove(account);
						this.closedAccounts.Add(account);
					}
					else
					{
						this.closedAccounts.Remove(account);
						this.accounts.AddInSortOrder(account, AccountSort.Instance);
					}

					break;
			}

			// Confirm the account is still in the proper sort order.
			this.accounts.UpdateSortPosition(account, AccountSort.Instance);
		}
	}
}
