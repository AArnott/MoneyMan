// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;
using Xunit.Sdk;

public class BankingAccountViewModelTests : MoneyTestBase
{
	private AssetViewModel alternateCurrency;
	private BankingAccountViewModel checking;
	private BankingAccountViewModel? savings;

	public BankingAccountViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.alternateCurrency = this.DocumentViewModel.AssetsPanel.NewAsset("alternate");
		this.alternateCurrency.Type = Asset.AssetType.Currency;

		this.checking = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Checking");
		this.savings = this.DocumentViewModel.AccountsPanel.NewBankingAccount("Savings");
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
	}

	[Fact]
	public void Name()
	{
		Assert.Equal("Checking", this.checking.Name);
		TestUtilities.AssertPropertyChangedEvent(
			this.checking,
			() => this.checking.Name = "changed",
			nameof(this.checking.Name));
		Assert.Equal("changed", this.checking.Name);
	}

	[Fact]
	public void Name_Validation()
	{
		this.checking.Name = string.Empty;

		this.Logger.WriteLine(this.checking.Error);
		Assert.NotEqual(string.Empty, this.checking[nameof(this.checking.Name)]);
		Assert.Equal(this.checking[nameof(this.checking.Name)], this.checking.Error);

		this.checking.Name = "a";
		Assert.Equal(string.Empty, this.checking[nameof(this.checking.Name)]);
	}

	[Fact]
	public void CurrencyAssets()
	{
		Assert.Contains(this.checking.CurrencyAssets, asset => asset.TickerSymbol == "USD");
	}

	[Fact]
	public void CurrencyAsset()
	{
		AssetViewModel? usd = this.DocumentViewModel.AssetsPanel.FindAsset("United States Dollar");
		Assumes.NotNull(usd);
		Assert.Same(usd, this.checking.CurrencyAsset);
		Assert.False(this.checking.CurrencyAssetIsReadOnly);
	}

	[Fact]
	public void CurrencyAsset_AcrossReloads()
	{
		Assert.Equal(this.Money.PreferredAssetId, this.checking.CurrencyAsset?.Id);
		this.ReloadViewModel();
		Assert.Equal(this.Money.PreferredAssetId, this.checking.CurrencyAsset?.Id);
	}

	[Fact]
	public void TransferTargetName()
	{
		this.checking.Name = "tt-test";
		Assert.Equal($"[{this.checking.Name}]", this.checking.TransferTargetName);
	}

	[Fact]
	public void TransferTargetName_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(this.checking, () => this.checking.Name = "other", nameof(this.checking.Name), nameof(this.checking.TransferTargetName));
	}

	[Fact]
	public void IsClosed()
	{
		Assert.False(this.checking.IsClosed);
		this.checking.IsClosed = true;
		Assert.True(this.checking.IsClosed);
	}

	[Fact]
	public void IsClosed_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.checking,
			() => this.checking.IsClosed = true,
			nameof(this.checking.IsClosed));
		TestUtilities.AssertPropertyChangedEvent(
			this.checking,
			() => this.checking.IsClosed = true);
	}

	[Fact]
	public void Type()
	{
		Assert.Equal(Account.AccountType.Banking, this.checking.Type);
		this.checking.Type = Account.AccountType.Investing;
		Assert.Equal(Account.AccountType.Investing, this.checking.Type);
	}

	[Fact]
	public void Type_PropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.checking,
			() => this.checking.Type = Account.AccountType.Investing,
			nameof(this.checking.Type));
		TestUtilities.AssertPropertyChangedEvent(
			this.checking,
			() => this.checking.Type = Account.AccountType.Investing);
	}

	[Fact]
	public void ApplyTo()
	{
		this.checking.Name = "some name";
		this.checking.IsClosed = !this.checking.IsClosed;
		this.checking.Type = Account.AccountType.Investing;
		this.checking.CurrencyAsset = this.alternateCurrency;

		this.checking.ApplyToModel();

		Assert.Equal(this.checking.Name, this.checking.Model!.Name);
		Assert.Equal(this.checking.IsClosed, this.checking.Model.IsClosed);
		Assert.Equal(this.checking.Type, this.checking.Model.Type);
		Assert.Equal(this.checking.CurrencyAsset?.Id, this.checking.Model.CurrencyAssetId);
	}

	[Fact]
	public void CopyFrom_Null()
	{
		Assert.Throws<ArgumentNullException>(() => this.checking.CopyFrom(null!));
	}

	[Fact]
	public void CopyFrom()
	{
		this.checking.Model!.Name = "another name";
		this.checking.Model.IsClosed = true;
		this.checking.Model.Type = Account.AccountType.Investing;
		this.checking.Model.CurrencyAssetId = this.alternateCurrency.Id;
		this.checking.CopyFrom(this.checking.Model);

		Assert.Equal(this.checking.Model.Name, this.checking.Name);
		Assert.Equal(this.checking.Model.IsClosed, this.checking.IsClosed);
		Assert.Equal(this.checking.Model.Type, this.checking.Type);
		Assert.Equal(this.checking.Model.CurrencyAssetId, this.checking.CurrencyAsset?.Id);
	}

	[Fact]
	public void Ctor_From_Volatile_Entity()
	{
		BankingAccountViewModel newAccount = this.DocumentViewModel.AccountsPanel.NewBankingAccount();
		Assert.Equal(0, newAccount.Model!.Id);

		Assert.Equal(newAccount.Name, newAccount.Model.Name);

		// Test auto-save behavior.
		newAccount.Name = "another name";
		Assert.Equal(newAccount.Name, newAccount.Model.Name);

		Account fromDb = this.Money.Accounts.First(tx => tx.Id == newAccount.Model.Id);
		Assert.Equal(newAccount.Model.Name, fromDb.Name);
		Assert.Single(this.Money.Accounts, a => a.Name == newAccount.Name);
	}

	[Fact]
	public void Ctor_From_Db_Entity()
	{
		var account = new Account
		{
			Name = "some account",
			CurrencyAssetId = this.Money.PreferredAssetId,
		};
		this.Money.Insert(account);

		var alternate = new BankingAccountViewModel(account, this.DocumentViewModel);

		Assert.Equal(account.Id, alternate.Id);
		Assert.Equal(account.Name, alternate.Name);

		// Test auto-save behavior.
		alternate.Name = "some other account";
		Assert.Equal(alternate.Name, account.Name);

		Account fromDb = this.Money.Accounts.First(tx => tx.Id == account.Id);
		Assert.Equal(account.Name, fromDb.Name);
		Assert.Single(this.Money.Accounts, a => a.Name == alternate.Name);
	}

	[Fact]
	public void PopulatesWithTransactionsFromDb()
	{
		this.Money.Action.Deposit(this.checking, 1);
		this.Money.Action.Withdraw(this.checking, 1);
		this.ReloadViewModel();
		Assert.Equal(2, this.checking.Transactions.Count(t => t.IsPersisted));
	}

	[Fact]
	public async Task DeleteTransaction()
	{
		this.Money.Action.Deposit(this.checking, 5);
		BankingTransactionViewModel txViewModel = this.checking.Transactions[0];
		this.DocumentViewModel.SelectedTransaction = txViewModel;
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Assert.Empty(this.Money.Transactions);
	}

	[Fact]
	public void DeleteVolatileTransaction()
	{
		BankingTransactionViewModel tx = this.checking.Transactions[^1];
		tx.Amount = 5;
		tx.Memo = "memo";
		this.checking.DeleteTransaction(tx);
		Assert.NotEmpty(this.checking.Transactions);
		tx = this.checking.Transactions[^1];
		Assert.Equal(0, tx.Amount);
		Assert.True(string.IsNullOrEmpty(tx.Memo));
		Assert.Null(tx.OtherAccount);
	}

	[Fact]
	public async Task DeleteTransactions()
	{
		this.Money.Action.Deposit(this.checking, 5);
		this.Money.Action.Deposit(this.checking, 12);
		this.Money.Action.Deposit(this.checking, 15);
		Assert.False(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
		this.DocumentViewModel.SelectedTransactions = this.checking.Transactions.Where(t => t.Amount != 12).ToArray();
		Assert.True(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Transaction remainingTransaction = Assert.Single(this.Money.Transactions);
		TransactionEntry remainingEntry = Assert.Single(this.Money.TransactionEntries);
		Assert.Equal(12, remainingEntry.Amount);
		Assert.Equal(remainingTransaction.Id, remainingEntry.TransactionId);
	}

	[Fact]
	public void ChangingVolatileTransactionProducesNewOne()
	{
		BankingTransactionViewModel tx1 = Assert.Single(this.checking.Transactions);
		Assert.False(tx1.IsPersisted);
		tx1.Amount = 50;
		Assert.True(tx1.IsPersisted);
		Assert.Equal(2, this.checking.Transactions.Count);
		BankingTransactionViewModel tx2 = this.checking.Transactions[^1];
		Assert.False(tx2.IsPersisted);
	}

	[Fact]
	public void VolatileTransaction_Properties()
	{
		BankingTransactionViewModel tx = Assert.Single(this.checking.Transactions);
		Assert.False(tx.IsPersisted);
		Assert.Equal(DateTime.Today, tx.When);
	}

	[Fact]
	public void Value()
	{
		Assert.Equal(0m, this.checking.Value);
		this.Money.Action.Deposit(this.checking, 10);
		this.Money.Action.Withdraw(this.checking, 2);

		this.AssertNowAndAfterReload(delegate
		{
			Assert.Equal(8m, this.checking.Value);
		});
	}

	[Fact]
	public void NewTransactionAddedToCollection()
	{
		BankingTransactionViewModel tx = this.checking.NewTransaction();
		Assert.Contains(tx, this.checking.Transactions);
	}

	[Fact]
	public void TransactionSorting_2Transactions()
	{
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.When = new DateTime(2021, 1, 1);

		BankingTransactionViewModel tx2 = this.checking.NewTransaction();
		tx2.When = new DateTime(2021, 1, 2);

		Assert.Equal(new[] { tx1.TransactionId, tx2.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));

		tx1.When = tx2.When + TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.TransactionId, tx1.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));
	}

	[Fact]
	public void TransactionSorting_3Transactions()
	{
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.When = new DateTime(2021, 1, 1);

		BankingTransactionViewModel tx2 = this.checking.NewTransaction();
		tx2.When = new DateTime(2021, 1, 3);

		BankingTransactionViewModel tx3 = this.checking.NewTransaction();
		tx3.When = new DateTime(2021, 1, 5);

		Assert.Equal(new[] { tx1.TransactionId, tx2.TransactionId, tx3.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));

		tx1.When = tx2.When + TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.TransactionId, tx1.TransactionId, tx3.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));

		tx1.When = tx3.When + TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.TransactionId, tx3.TransactionId, tx1.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));

		tx1.When += TimeSpan.FromDays(1);
		Assert.Equal(new[] { tx2.TransactionId, tx3.TransactionId, tx1.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));
	}

	[Fact]
	public void TransactionsSortedOnLoad()
	{
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.When = new DateTime(2021, 1, 3);
		tx1.Amount = 1;

		BankingTransactionViewModel tx2 = this.checking.NewTransaction();
		tx2.When = new DateTime(2021, 1, 2);
		tx2.Amount = 2;

		// Confirm that a reload does not mess up transaction order.
		Assert.Equal(new[] { tx2.TransactionId, tx1.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));
		this.ReloadViewModel();
		Assert.Equal(new[] { tx2.TransactionId, tx1.TransactionId, 0 }, this.checking.Transactions.Select(tx => tx.TransactionId));
	}

	[Fact]
	public async Task Balance_Updates()
	{
		Assert.Equal(0, this.checking.Value);

		BankingTransactionViewModel txViewModel = this.checking.NewTransaction();
		txViewModel.Amount = 10;
		Assert.Equal(10, this.checking.Value);

		txViewModel.Amount = 8;
		Assert.Equal(8, this.checking.Value);

		Assert.Contains(txViewModel, this.checking.Transactions);
		this.DocumentViewModel.SelectedTransaction = this.checking.Transactions[0];
		await TestUtilities.AssertPropertyChangedEventAsync(this.checking, () => this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync(), nameof(this.checking.Value));
		Assert.Equal(0, this.checking.Value);
	}

	[Fact]
	public void TransactionBalancesInitializedOnLoad()
	{
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.When = new DateTime(2021, 1, 2);
		tx1.Amount = 5m;

		BankingTransactionViewModel tx2 = this.checking.NewTransaction();
		tx2.When = new DateTime(2021, 1, 3);
		tx2.Amount = 8m;

		this.ReloadViewModel();
		Assert.Equal(5m, this.checking.Transactions[0].Balance);
		Assert.Equal(13m, this.checking.Transactions[1].Balance);
	}

	[Fact]
	public void SettingBalanceDoesNotPersistAccount()
	{
		var account = new Account { Name = "some account" };
		this.Money.Insert(account);
		this.checking = new BankingAccountViewModel(account, this.DocumentViewModel);
		bool eventRaised = false;
		this.Money.EntitiesChanged += (s, e) => eventRaised = true;
		this.checking.Value = 10;
		Assert.False(eventRaised);
	}

	[Fact]
	public void Balance_ChangesFromTransactionChangeInOtherAccount()
	{
		BankingTransactionViewModel txViewModel = this.checking.NewTransaction();
		txViewModel.Amount = -10;
		txViewModel.OtherAccount = this.savings;
		Assert.Equal(-10, this.checking.Value);

		Assert.Equal(10, this.savings!.Value);

		txViewModel.Amount = -5;
		Assert.Equal(5, this.savings!.Value);
	}

	[Fact]
	public void TransferFromDbAppearsInBothAccounts()
	{
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.Amount = -10;
		tx1.OtherAccount = this.savings;

		// An counterpart transfer view model should have been added to the savings account.
		BankingTransactionViewModel tx2 = Assert.Single(this.savings!.Transactions, t => t.IsPersisted);
		Assert.Same(this.checking, tx2.OtherAccount);
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	[Fact]
	public void NewTransferShowsUpInBothAccounts()
	{
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.Amount = -10;
		tx1.OtherAccount = this.savings;

		// A counterpart transfer view model should have been added to the savings account.
		BankingTransactionViewModel tx2 = Assert.Single(this.savings!.Transactions, t => t.IsPersisted);
		Assert.Same(this.checking, tx2.OtherAccount);
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	[Fact]
	public void DeletedTransferIsRemovedFromBothAccounts()
	{
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.Amount = -10;
		tx1.OtherAccount = this.savings;

		BankingTransactionViewModel tx2 = this.savings!.Transactions[0];

		this.checking.DeleteTransaction(tx1);
		Assert.Empty(this.savings!.Transactions.Where(tx => tx.IsPersisted));
	}

	[Fact]
	public void TransferBecomesOrdinaryWhenOneAccountIsDeleted()
	{
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.Amount = -10;
		tx1.OtherAccount = this.savings;

		this.DocumentViewModel.AccountsPanel.DeleteAccount(this.savings!);

		this.AssertNowAndAfterReload(delegate
		{
			tx1 = this.checking.FindTransaction(tx1.TransactionId) ?? throw new XunitException("Missing transaction.");
			Assert.Null(tx1.OtherAccount);
			Assert.Single(tx1.Entries);
		});
	}

	[Fact]
	public void TransferChangedToCategoryIsRemovedFromOtherAccount()
	{
		CategoryAccountViewModel cat = this.DocumentViewModel.CategoriesPanel.NewCategory("Household");

		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.Amount = -10;
		tx1.OtherAccount = this.savings;

		Assert.Single(this.savings!.Transactions.Where(t => t.IsPersisted));
		tx1.OtherAccount = cat;
		Assert.Empty(this.savings!.Transactions.Where(t => t.IsPersisted));
		Assert.Contains(tx1, this.checking.Transactions);
	}

	[Fact]
	public void TransferPropertyChangesAreReflectedInOtherAccount()
	{
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
		BankingTransactionViewModel tx1 = this.checking.NewTransaction();
		tx1.Amount = -10;
		tx1.OtherAccount = this.savings;

		BankingTransactionViewModel tx2 = this.savings!.Transactions[0];
		tx1.Memo = "memo 1";
		Assert.Equal(tx1.Memo, tx2.Memo);
		tx1.Amount = 5;
		Assert.Equal(-tx1.Amount, tx2.Amount);
	}

	/// <summary>
	/// Verifies that a split transaction where a split line item represents a transfer to another account also appears
	/// in the other account, and that whatever the split item amount is appears and impacts the account balance.
	/// </summary>
	[Fact]
	public void SplitTransferTransactionAppearsInOtherAccount()
	{
		BankingTransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		this.AssertNowAndAfterReload(delegate
		{
			BankingTransactionViewModel txSavings = this.savings!.Transactions[0];
			Assert.True(txSavings.ContainsSplits);

			Assert.Equal(40, txSavings.Amount);
			Assert.Equal(40, this.savings!.Value);
			Assert.Equal(tx.When, txSavings.When);

			Assert.IsType<CategoryAccountViewModel>(txSavings.Splits[0].Account);
			Assert.Equal(100, txSavings.Splits[0].Amount);

			Assert.Same(this.checking, txSavings.Splits[1].Account);
			Assert.Equal(-60, txSavings.Splits[1].Amount);
		});
	}

	/// <summary>
	/// Verifies that a split transaction where <em>multiple</em> split line items represent a transfer to another account also appears
	/// in the other account, and that whatever the split item amount is appears and impacts the account balance.
	/// </summary>
	[Fact]
	public void MultipleSplitTransfersTransactionAppearsInOtherAccount()
	{
		BankingTransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		TransactionEntryViewModel split3 = tx.NewSplit();
		split3.Amount = -5;
		split3.Account = this.savings;
		tx.Amount += split3.Amount;
		Assert.Equal(tx.GetSplitTotal(), this.checking.Value);

		this.AssertNowAndAfterReload(delegate
		{
			BankingTransactionViewModel txSavings = this.savings!.Transactions[0];
			Assert.Equal(45, txSavings.Amount);
			Assert.Equal(100, txSavings.Splits[0].Amount);
			Assert.Equal(-55, txSavings.Splits[1].Amount);

			// TODO: How should two splits into savings be represented?
		});
	}

	/// <summary>
	/// Verifies that an attempt to delete a transaction that is actually just a split that transfers with another account
	/// cannot be deleted from the view of the other account.
	/// This is important because it would quietly upset the sum of the splits that (presumably) match the total of the overall transaction.
	/// </summary>
	[Fact]
	public void SplitTransferTransactionCannotBeDeletedFromOtherAccount()
	{
		BankingTransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.Equal(tx.GetSplitTotal(), this.checking.Value);

		BankingTransactionViewModel txSavings = this.savings!.Transactions[0];
		Assert.Throws<InvalidOperationException>(() => this.savings!.DeleteTransaction(txSavings));
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.savings;
		this.DocumentViewModel.SelectedTransaction = txSavings;
		Assert.False(this.DocumentViewModel.DeleteTransactionsCommand.CanExecute());
	}

	[Fact]
	public void SplitTransferTransactionAllowLimitedChangesFromOtherAccount()
	{
		BankingTransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();

		BankingTransactionViewModel txSavings = this.savings!.Transactions[0];

		// Disallow changes to amount, since that can upset the balance on the overall transaction.
		Assert.Throws<InvalidOperationException>(() => txSavings.Amount += 1);

		// Disallow changes to category, since that is set in the original transaction to this foreign account.
		Assert.Throws<InvalidOperationException>(() => txSavings.OtherAccount = this.DocumentViewModel.CategoriesPanel.Categories.First());

		// Allow updating the memo field.
		txSavings.Memo = "some memo";
		TransactionEntryViewModel splitTransfer = Assert.Single(tx.Splits, s => s.Account == this.savings);
		Assert.Equal(txSavings.Memo, splitTransfer.Memo);

		// Allow updating the cleared flag, independently of the parent transaction.
		txSavings.Cleared = ClearedState.Cleared;
		Assert.Equal(ClearedState.None, tx.Cleared);
	}

	[Fact]
	public void DeletingSplitTransferAlsoRemovesTransactionsFromOtherAccounts()
	{
		BankingTransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.NotEmpty(this.savings!.Transactions);
		tx.DeleteSplit(tx.Splits.Single(s => s.Account is BankingAccountViewModel));

		this.AssertNowAndAfterReload(delegate
		{
			Assert.False(Assert.Single(this.savings!.Transactions).IsPersisted);
		});
	}

	[Fact]
	public void DeletingSplitTransactionWithTransfersAlsoRemovesTransactionsFromOtherAccounts()
	{
		BankingTransactionViewModel tx = this.CreateSplitWithCategoryAndTransfer();
		Assert.NotEmpty(this.savings!.Transactions);
		this.checking.DeleteTransaction(tx);

		this.AssertNowAndAfterReload(delegate
		{
			Assert.Empty(this.savings!.Transactions.Where(tx => tx.IsPersisted));
		});
	}

	[Fact]
	public async Task NewTransaction_Undo()
	{
		const string memo = "some memo";
		this.savings!.Transactions[^1].Memo = memo;
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Accounts;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		this.RefetchViewModels();
		Assert.DoesNotContain(this.savings!.Transactions, t => t.Memo == memo);
		Assert.False(this.savings!.Transactions[^1].IsPersisted);

		Assert.Equal(DocumentViewModel.SelectableViews.Banking, this.DocumentViewModel.SelectedViewIndex);
		Assert.Same(this.savings, this.DocumentViewModel.BankingPanel.SelectedAccount);
		Assert.Null(this.DocumentViewModel.SelectedTransaction);
	}

	[Fact]
	public async Task DeleteTransaction_Undo()
	{
		const string memo = "some memo";
		this.DocumentViewModel.SelectedTransaction = this.savings!.Transactions[0];
		this.savings!.Transactions[0].Memo = memo;
		await this.DocumentViewModel.DeleteTransactionsCommand.ExecuteAsync();
		Assert.Null(this.savings!.Transactions[0].Memo);
		this.DocumentViewModel.SelectedViewIndex = DocumentViewModel.SelectableViews.Accounts;

		await this.DocumentViewModel.UndoCommand.ExecuteAsync();
		this.RefetchViewModels();
		Assert.Equal(memo, this.savings!.Transactions[0].Memo);
		Assert.True(this.savings!.Transactions[0].IsPersisted);
		Assert.False(this.savings!.Transactions[^1].IsPersisted);

		Assert.Equal(DocumentViewModel.SelectableViews.Banking, this.DocumentViewModel.SelectedViewIndex);
		Assert.Same(this.savings, this.DocumentViewModel.BankingPanel.SelectedAccount);
		Assert.Same(this.savings!.Transactions[0], this.DocumentViewModel.SelectedTransaction);
	}

	protected override void ReloadViewModel()
	{
		base.ReloadViewModel();
		this.RefetchViewModels();
		this.DocumentViewModel.BankingPanel.SelectedAccount = this.checking;
	}

	private void RefetchViewModels()
	{
		this.checking = (BankingAccountViewModel)this.DocumentViewModel.AccountsPanel.Accounts.Single(a => a.Name == "Checking");
		this.savings = (BankingAccountViewModel?)this.DocumentViewModel.AccountsPanel.Accounts.SingleOrDefault(a => a.Name == "Savings");
	}

	private BankingTransactionViewModel CreateSplitWithCategoryAndTransfer()
	{
		CategoryAccountViewModel cat1 = this.DocumentViewModel.CategoriesPanel.NewCategory("Salary");

		BankingTransactionViewModel tx = this.checking.NewTransaction();
		TransactionEntryViewModel split1 = tx.NewSplit();
		split1.Amount = 100; // gross
		split1.Account = cat1;
		TransactionEntryViewModel split2 = tx.Splits[^1];
		split2.Amount = -40; // send $40 of the $100 to savings
		split2.Account = this.savings;
		tx.Amount = split1.Amount + split2.Amount;

		return tx;
	}
}
