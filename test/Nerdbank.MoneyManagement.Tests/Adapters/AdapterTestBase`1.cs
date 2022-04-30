// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;
using Nerdbank.MoneyManagement.Adapters;

public abstract class AdapterTestBase<T> : MoneyTestBase
	where T : IFileAdapter
{
	protected AdapterTestBase(ITestOutputHelper logger)
		: base(logger)
	{
		this.Checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
	}

	protected abstract T Adapter { get; }

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

	protected async Task<int> ImportAsync(string testAssetFileName)
	{
		Verify.Operation(this.Adapter is not null, "Set the adapter first.");

		// Suspend view model updates the same way that the import command does.
		int result;
		using (this.DocumentViewModel.SuspendViewModelUpdates())
		{
			result = await this.Adapter.ImportAsync(this.GetTestDataFile(testAssetFileName), this.TimeoutToken);
		}

		this.RefetchViewModels();
		return result;
	}
}
