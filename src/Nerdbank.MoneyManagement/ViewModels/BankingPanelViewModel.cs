// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class BankingPanelViewModel : BindableBase
{
	private SortedObservableCollection<AccountViewModel> accounts = new(AccountSort.Instance);
	private List<AccountViewModel> closedAccounts = new();
	private AccountViewModel? selectedAccount;

	public string Title => "Banking";

	public string NetWorthCaption => "Net worth";

	public IReadOnlyList<AccountViewModel> Accounts => this.accounts;

	public AccountViewModel? SelectedAccount
	{
		get => this.selectedAccount;
		set => this.SetProperty(ref this.selectedAccount, value);
	}

	public string WhenHeader => "Date";

	public string PayeeHeader => "Payee";

	public string CategoryHeader => "Category";

	public string MemoHeader => "Memo";

	public string AmountHeader => "Amount";

	public string CheckNumberHeader => "Check No.";

	public string ClearedHeader => "Clr";

	public string BalanceHeader => "Balance";

	public AccountViewModel? FindAccount(int id) => this.Accounts.FirstOrDefault(acct => acct.Id == id);

	internal void Add(AccountViewModel account)
	{
		if (account.IsClosed)
		{
			this.closedAccounts.Add(account);
		}
		else
		{
			this.accounts.Add(account);
		}

		account.PropertyChanged += this.Account_PropertyChanged;
	}

	internal void Remove(AccountViewModel account)
	{
		if (this.accounts.Remove(account) < 0)
		{
			this.closedAccounts.Remove(account);
		}

		account.PropertyChanged -= this.Account_PropertyChanged;
	}

	/// <summary>
	/// Clears the view model without deleting anything from the database.
	/// </summary>
	internal void ClearViewModel()
	{
		this.accounts.Clear();
		this.closedAccounts.Clear();
		this.selectedAccount = null;
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
					this.accounts.Add(account);
				}

				break;
		}
	}
}
