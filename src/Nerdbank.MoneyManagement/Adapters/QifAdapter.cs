// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Text;
using Microsoft;
using Nerdbank.MoneyManagement.ViewModels;
using Nerdbank.Qif;

namespace Nerdbank.MoneyManagement.Adapters;

public class QifAdapter : IFileAdapter
{
	private readonly MoneyFile moneyFile;
	private readonly IUserNotification? userNotification;
	private readonly Dictionary<string, Qif.Category> importingCategoriesByName = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Account> existingCategories = new(StringComparer.OrdinalIgnoreCase);
	private QifDocument? importingDocument;

	public QifAdapter(DocumentViewModel documentViewModel)
	{
		// We don't store the document view model itself in a field to avoid the temptation to mix
		// use of the model and (much slower) view model APIs during import.
		this.moneyFile = documentViewModel.MoneyFile;
		this.userNotification = documentViewModel.UserNotification;
	}

	public IReadOnlyList<AdapterFileType> FileTypes { get; } = new AdapterFileType[]
	{
		new("Quicken Interchange Format", new[] { "qif" }),
	};

	public async Task<int> ImportAsync(string filePath, CancellationToken cancellationToken)
	{
		Requires.NotNullOrEmpty(filePath, nameof(filePath));

		IDisposable? batchImportTransaction = null;
		try
		{
			this.importingDocument = QifDocument.Load(filePath);
			int records = 0;

			this.IndexCategories();

			if (this.importingDocument.Transactions.Count > 0 && this.importingDocument.Accounts.Count == 0)
			{
				// We're importing a file that carries transactions with no account information.
				if (this.userNotification is null)
				{
					// Unable to ask the user to confirm the account to import into.
					return records;
				}

				StringBuilder prompt = new("Which account should this statement be imported into?");
				AccountViewModel? account = await this.userNotification.ChooseAccountAsync(prompt.ToString(), null, cancellationToken);
				if (account is BankingAccountViewModel bankingAccount)
				{
					batchImportTransaction = this.moneyFile.UndoableTransaction($"Import transactions from {Path.GetFileNameWithoutExtension(filePath)}", null);
					records += this.ImportTransactions(bankingAccount, this.importingDocument.Transactions.OfType<BankTransaction>());
				}
				else if (account is InvestingAccountViewModel investmentAccount)
				{
					batchImportTransaction = this.moneyFile.UndoableTransaction($"Import transactions from {Path.GetFileNameWithoutExtension(filePath)}", null);
					records += this.ImportTransactions(investmentAccount, this.importingDocument.Transactions.OfType<InvestmentTransaction>());
				}
			}
			else if (this.importingDocument.Transactions.Count == 0 && this.importingDocument.Accounts.Count > 0)
			{
				batchImportTransaction = this.moneyFile.UndoableTransaction($"Import accounts from {Path.GetFileNameWithoutExtension(filePath)}", null);

				// We're importing a file that contains accounts, which may each contain transactions.
				// First, import all accounts so that later we can create transfers.
				foreach (Qif.Account importingAccount in this.importingDocument.Accounts)
				{
					Account localAccount = this.moneyFile.Accounts.FirstOrDefault(acc => acc.Name == importingAccount.Name);
					if (localAccount is null)
					{
						localAccount = new()
						{
							Name = importingAccount.Name,
							Type = importingAccount.AccountType == AccountType.Investment ? Account.AccountType.Investing : Account.AccountType.Banking,
							CurrencyAssetId = this.moneyFile.PreferredAssetId,
						};
						this.moneyFile.Insert(localAccount);
					}
				}

				// Now run through each account again, this time importing transactions into each.
				foreach (Qif.Account importingAccount in this.importingDocument.Accounts)
				{
					Account localAccount = this.moneyFile.Accounts.FirstOrDefault(acc => acc.Name == importingAccount.Name);
					if (importingAccount is Qif.BankAccount importingBankAccount)
					{
						records += this.ImportTransactions(localAccount, importingBankAccount.Transactions);
					}
					else if (importingAccount is Qif.InvestmentAccount importingInvestmentAccount)
					{
						records += this.ImportTransactions(localAccount, importingInvestmentAccount.Transactions);
					}
				}
			}
			else if (this.importingDocument.Categories.Count > 0)
			{
				// Just import all the categories.
				batchImportTransaction = this.moneyFile.UndoableTransaction($"Import categories from {Path.GetFileNameWithoutExtension(filePath)}", null);
				foreach (Category category in this.importingDocument.Categories)
				{
					this.GetOrImportCategory(category.Name, out bool imported);
					if (imported)
					{
						records++;
					}
				}
			}

			return records;
		}
		finally
		{
			this.ClearImportState();
			batchImportTransaction?.Dispose();
		}
	}

	private static ClearedState FromQifClearedState(Qif.ClearedState value)
	{
		return value switch
		{
			Qif.ClearedState.Cleared => ClearedState.Cleared,
			Qif.ClearedState.Reconciled => ClearedState.Reconciled,
			_ => ClearedState.None,
		};
	}

	private void IndexCategories()
	{
		Assumes.NotNull(this.importingDocument);

		// Just import all the categories.
		foreach (Category category in this.importingDocument.Categories)
		{
			this.importingCategoriesByName.Add(category.Name, category);
		}
	}

	private int GetOrImportCategory(string name, out bool imported)
	{
		Requires.NotNullOrEmpty(name, nameof(name));

		imported = false;
		if (this.existingCategories.TryGetValue(name, out Account? existingCategory))
		{
			return existingCategory.Id;
		}

		existingCategory = this.moneyFile.Categories.FirstOrDefault(cat => cat.Name == name);
		if (existingCategory is null)
		{
			existingCategory = this.importingCategoriesByName.TryGetValue(name, out Qif.Category? importingCategory)
				? new() { Name = importingCategory.Name }
				: new() { Name = name };
			existingCategory.Type = Account.AccountType.Category;
			this.moneyFile.Insert(existingCategory);
			imported = true;
		}

		this.existingCategories.Add(name, existingCategory);
		return existingCategory.Id;
	}

	private void ClearImportState()
	{
		this.existingCategories.Clear();
		this.importingCategoriesByName.Clear();
		this.importingDocument = null;
	}

	private int ImportTransactions(Account target, IEnumerable<BankTransaction> transactions)
	{
		int importedCount = 0;

		// We insert all entities at once at the end of the loop because it's *much* faster for sqlite than
		// to add each entity individually.
		List<Transaction> newTransactions = new();
		List<(Transaction, TransactionEntry)> newEntryTuples = new();
		foreach (BankTransaction importingTransaction in transactions)
		{
			importedCount++;
			Transaction newTransaction = new()
			{
				When = importingTransaction.Date,
				Memo = importingTransaction.Memo,
				Payee = importingTransaction.Payee,
			};
			if (int.TryParse(importingTransaction.Number, out int checkNumber))
			{
				newTransaction.CheckNumber = checkNumber;
			}

			newTransactions.Add(newTransaction);
			TransactionEntry newEntry = new()
			{
				Amount = importingTransaction.Amount,
				Cleared = FromQifClearedState(importingTransaction.ClearedStatus),
				AccountId = target.Id,
				AssetId = this.moneyFile.CurrentConfiguration.PreferredAssetId,
			};
			newEntryTuples.Add((newTransaction, newEntry));

			if (!string.IsNullOrEmpty(importingTransaction.Category))
			{
				TransactionEntry categoryEntry = new()
				{
					AccountId = this.GetOrImportCategory(importingTransaction.Category, out bool importedCategory),
					Amount = importingTransaction.Amount,
					AssetId = this.moneyFile.CurrentConfiguration.PreferredAssetId,
				};
				newEntryTuples.Add((newTransaction, categoryEntry));
				if (importedCategory)
				{
					importedCount++;
				}
			}
		}

		// We first insert the transactions so we get IDs for all of them.
		// Then we can set the IDs on each TransactionEntry and insert those as well.
		this.moneyFile.InsertAll(newTransactions);
		List<TransactionEntry> newEntries = new(newEntryTuples.Count);
		foreach ((Transaction, TransactionEntry) item in newEntryTuples)
		{
			item.Item2.TransactionId = item.Item1.Id;
			newEntries.Add(item.Item2);
		}

		this.moneyFile.InsertAll(newEntries);

		return importedCount;
	}

	private int ImportTransactions(Account target, IEnumerable<InvestmentTransaction> transactions)
	{
		int transactionsImported = 0;
		foreach (InvestmentTransaction transaction in transactions)
		{
			////transactionsImported++;
		}

		return transactionsImported;
	}
}
