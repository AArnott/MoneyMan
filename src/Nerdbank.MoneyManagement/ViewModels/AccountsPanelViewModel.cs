﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using PCLCommandBase;
	using Validation;

	public class AccountsPanelViewModel : BindableBase
	{
		private readonly DocumentViewModel documentViewModel;
		private readonly ObservableCollection<AccountViewModel> accounts = new();
		private AccountViewModel? selectedAccount;
		private IList? selectedAccounts;

		public AccountsPanelViewModel(DocumentViewModel documentViewModel)
		{
			this.AddCommand = new AddAccountCommand(this);
			this.DeleteCommand = new DeleteAccountCommand(this);
			this.documentViewModel = documentViewModel;
		}

		public CommandBase AddCommand { get; }

		/// <summary>
		/// Gets a command that deletes all accounts in the <see cref="SelectedAccounts"/> collection, if that property is set;
		/// otherwise the <see cref="SelectedAccount"/> is deleted.
		/// </summary>
		public CommandBase DeleteCommand { get; }

		public IReadOnlyCollection<AccountViewModel> Accounts => this.accounts;

		/// <summary>
		/// Gets or sets the selected account, or one of the selected accounts.
		/// </summary>
		public AccountViewModel? SelectedAccount
		{
			get => this.selectedAccount;
			set => this.SetProperty(ref this.selectedAccount, value);
		}

		/// <summary>
		/// Gets or sets a collection of selected accounts.
		/// </summary>
		/// <remarks>
		/// This is optional. When set, the <see cref="DeleteCommand"/> will use this collection as the set of accounts to delete.
		/// When not set, the <see cref="SelectedAccount"/> will be used by the <see cref="DeleteCommand"/>.
		/// </remarks>
		public IList? SelectedAccounts
		{
			get => this.selectedAccounts;
			set => this.SetProperty(ref this.selectedAccounts, value);
		}

		internal AccountViewModel? AddingAccount { get; set; }

		public void Add(AccountViewModel accountViewModel)
		{
			this.accounts.Add(accountViewModel);
			this.documentViewModel.TransactionTargets.Add(accountViewModel);
		}

		/// <summary>
		/// Creates a new <see cref="Account"/> and <see cref="AccountViewModel"/>.
		/// The <see cref="AccountViewModel"/> is added to the view model collection,
		/// but the <see cref="Account"/> will only be added to the database when a property on it has changed.
		/// </summary>
		/// <param name="name">The name for the new account.</param>
		/// <returns>The new <see cref="AccountViewModel"/>.</returns>
		public AccountViewModel NewAccount(string name = "")
		{
			if (this.AddingAccount is object)
			{
				this.SelectedAccount = this.AddingAccount;
				return this.AddingAccount;
			}

			AccountViewModel newAccountViewModel = new(null, this.documentViewModel)
			{
				Model = new(),
			};

			this.accounts.Add(newAccountViewModel);
			this.SelectedAccount = newAccountViewModel;
			if (string.IsNullOrEmpty(name))
			{
				this.AddingAccount = newAccountViewModel;
				newAccountViewModel.NotifyWhenValid(s =>
				{
					if (this.AddingAccount == s)
					{
						this.documentViewModel.TransactionTargets.Add(s);
						if (!s.IsClosed)
						{
							this.documentViewModel.BankingPanel.Add(s);
						}

						this.AddingAccount = null;
					}
				});
			}
			else
			{
				newAccountViewModel.Name = name;
				this.documentViewModel.TransactionTargets.Add(newAccountViewModel);
				this.documentViewModel.BankingPanel.Add(newAccountViewModel);
			}

			return newAccountViewModel;
		}

		public void DeleteAccount(AccountViewModel accountViewModel)
		{
			this.accounts.Remove(accountViewModel);
			this.documentViewModel.BankingPanel.Remove(accountViewModel);
			this.documentViewModel.TransactionTargets.Remove(accountViewModel);

			if (accountViewModel.Model is object)
			{
				this.documentViewModel.MoneyFile?.Delete(accountViewModel.Model);
			}

			if (this.SelectedAccount == accountViewModel)
			{
				this.SelectedAccount = null;
			}

			if (this.AddingAccount == accountViewModel)
			{
				this.AddingAccount = null;
			}
		}

		private class AddAccountCommand : CommandBase
		{
			private readonly AccountsPanelViewModel viewModel;

			public AddAccountCommand(AccountsPanelViewModel viewModel)
			{
				this.viewModel = viewModel;
			}

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				this.viewModel.NewAccount();
				return Task.CompletedTask;
			}
		}

		private class DeleteAccountCommand : CommandBase
		{
			private readonly AccountsPanelViewModel viewModel;
			private INotifyCollectionChanged? subscribedSelectedAccounts;

			public DeleteAccountCommand(AccountsPanelViewModel viewModel)
			{
				this.viewModel = viewModel;
				this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
				this.SubscribeToSelectionChanged();
			}

			public string Caption => "_Delete";

			public override bool CanExecute(object? parameter) => base.CanExecute(parameter) && (this.viewModel.SelectedAccounts?.Count > 0 || this.viewModel.SelectedAccount is object);

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				if (this.viewModel.SelectedAccounts is object)
				{
					foreach (AccountViewModel account in this.viewModel.SelectedAccounts.OfType<AccountViewModel>().ToList())
					{
						this.viewModel.DeleteAccount(account);
					}
				}
				else if (this.viewModel.SelectedAccount is object)
				{
					this.viewModel.DeleteAccount(this.viewModel.SelectedAccount);
				}

				return Task.CompletedTask;
			}

			private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName is nameof(this.viewModel.SelectedAccounts))
				{
					this.SubscribeToSelectionChanged();
				}
				else if (e.PropertyName is nameof(this.viewModel.SelectedAccount))
				{
					this.OnCanExecuteChanged();
				}
			}

			private void SelectedAccounts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.OnCanExecuteChanged();

			private void SubscribeToSelectionChanged()
			{
				if (this.subscribedSelectedAccounts is object)
				{
					this.subscribedSelectedAccounts.CollectionChanged -= this.SelectedAccounts_CollectionChanged;
				}

				this.subscribedSelectedAccounts = this.viewModel.SelectedAccounts as INotifyCollectionChanged;

				if (this.subscribedSelectedAccounts is object)
				{
					this.subscribedSelectedAccounts.CollectionChanged += this.SelectedAccounts_CollectionChanged;
				}
			}
		}
	}
}
