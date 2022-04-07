// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement.Adapters;

public abstract class AdapterTestBase : MoneyTestBase
{
	protected AdapterTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.Checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
	}

	protected BankingAccountViewModel Checking { get; private set; }

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.RefetchViewModels();
	}

	protected virtual void RefetchViewModels()
	{
		this.Checking = (BankingAccountViewModel)this.DocumentViewModel.GetAccount(this.Checking.Id);
	}
}
