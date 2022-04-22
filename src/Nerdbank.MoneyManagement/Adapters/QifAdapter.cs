﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Text;
using System.Xml.Serialization;
using Microsoft;
using Nerdbank.MoneyManagement.ViewModels;
using Nerdbank.Qif;

namespace Nerdbank.MoneyManagement.Adapters;

public class QifAdapter : IFileAdapter
{
	private readonly MoneyFile moneyFile;
	private readonly IUserNotification? userNotification;
	private readonly Dictionary<string, Account> categories = new(StringComparer.OrdinalIgnoreCase);
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

			batchImportTransaction = this.moneyFile.UndoableTransaction($"Import {Path.GetFileNameWithoutExtension(filePath)}", null);
			records += this.ImportCategories();
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
					records += this.ImportTransactions(bankingAccount, this.importingDocument.Transactions.OfType<BankTransaction>());
				}
				else if (account is InvestingAccountViewModel investmentAccount)
				{
					records += this.ImportTransactions(investmentAccount, this.importingDocument.Transactions.OfType<InvestmentTransaction>());
				}
			}
			else if (this.importingDocument.Transactions.Count == 0 && this.importingDocument.Accounts.Count > 0)
			{
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

	private int ImportCategories()
	{
		Assumes.NotNull(this.importingDocument);

		foreach (Account category in this.moneyFile.Categories)
		{
			this.categories.Add(category.Name, category);
		}

		// Import the explicit categories, if any.
		foreach (Qif.Category category in this.importingDocument.Categories)
		{
			if (!this.categories.ContainsKey(category.Name))
			{
				this.categories.Add(
					category.Name,
					new Account
					{
						Name = category.Name,
						Type = Account.AccountType.Category,
					});
			}
		}

		// Also infer the categories that are referenced by the transactions.
		// These *may* be redundant with the explicit categories, but the explicit categories list
		// may be absent from the file altogether.
		IEnumerable<Qif.BankTransaction> allTransactions = this.importingDocument.Transactions
			.Concat(this.importingDocument.Accounts.SelectMany(a => a.Transactions))
			.OfType<Qif.BankTransaction>();
		foreach (Qif.BankTransaction transaction in allTransactions)
		{
			AddCategory(transaction.Category);
			foreach (BankSplit split in transaction.Splits)
			{
				AddCategory(split.Category);
			}

			void AddCategory(string? category)
			{
				// Skip empty categories or categories that refer to other accounts involved in a transfer.
				if (!string.IsNullOrEmpty(category) && category[0] != '[')
				{
					if (!this.categories.ContainsKey(category))
					{
						this.categories.Add(
							category,
							new Account
							{
								Name = category,
								Type = Account.AccountType.Category,
							});
					}
				}
			}
		}

		List<Account> newCategories = this.categories.Values.Where(v => v.Id == 0).ToList();
		this.moneyFile.InsertAll(newCategories);
		return newCategories.Count;
	}

	private void ClearImportState()
	{
		this.categories.Clear();
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
			int? transferAccountId = this.FindTransferAccountId(importingTransaction.Category);
			if (transferAccountId is not null && importingTransaction.Amount >= 0 && target.Id != transferAccountId)
			{
				// When it comes to transfers, skip importing the transaction on the receiving side
				// so that we don't end up importing them on both ends and ending up with duplicates.
				// For splits with transfers it's even more important to drop the receiving side because
				// when it comes to paychecks, there is just one sender and many receiving accounts.
				// TODO: figure out the splits transfers details. QIF only expresses the split on one account.
				continue;
			}

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

			if (importingTransaction.Splits.IsEmpty)
			{
				if (!string.IsNullOrEmpty(importingTransaction.Category))
				{
					int? categoryId =
						this.categories.TryGetValue(importingTransaction.Category, out Account? category) ? category.Id :
						transferAccountId;
					if (categoryId.HasValue && categoryId.Value != target.Id)
					{
						TransactionEntry categoryEntry = new()
						{
							AccountId = categoryId.Value,
							Amount = -importingTransaction.Amount,
							AssetId = this.moneyFile.CurrentConfiguration.PreferredAssetId,
						};
						newEntryTuples.Add((newTransaction, categoryEntry));
					}
				}
			}
			else
			{
				// For a split, Quicken uses the first split category as the main transaction category,
				// which is meaningless so we just ignore the main category in this case.
				foreach (BankSplit split in importingTransaction.Splits)
				{
					int? categoryId =
						split.Category is not null && this.categories.TryGetValue(split.Category, out Account? category) ? category.Id :
						this.FindTransferAccountId(split.Category);
					if (categoryId.HasValue && split.Amount.HasValue)
					{
						TransactionEntry categoryEntry = new()
						{
							AccountId = categoryId.Value,
							Amount = -split.Amount.Value,
							AssetId = this.moneyFile.CurrentConfiguration.PreferredAssetId,
							Memo = split.Memo,
						};
						newEntryTuples.Add((newTransaction, categoryEntry));
					}
				}
			}
		}

		this.InsertAllTransactions(newTransactions, newEntryTuples);

		return importedCount;
	}

	private int ImportTransactions(Account target, IEnumerable<InvestmentTransaction> transactions)
	{
		int transactionsImported = 0;
		List<Transaction> newTransactions = new();
		List<(Transaction, TransactionEntry)> newEntryTuples = new();
		foreach (InvestmentTransaction importingTransaction in transactions)
		{
			Transaction newTransaction = new()
			{
				When = importingTransaction.Date,
				Payee = importingTransaction.Payee,
				Memo = importingTransaction.Memo,
				Action = importingTransaction.Action switch
				{
					"XOut" => TransactionAction.Transfer,
					_ => throw new NotSupportedException("Unsupported investment transaction Action: " + importingTransaction.Action),
				},
			};

			switch (importingTransaction.Action)
			{
				case "XOut":
					int? transferAccountId = this.FindTransferAccountId(importingTransaction.AccountForTransfer);
					if (transferAccountId is null)
					{
						throw new InvalidOperationException("The transfer account isn't recognized: " + importingTransaction.AccountForTransfer);
					}

					if (importingTransaction.AmountTransferred is null)
					{
						throw new InvalidOperationException("The transfer amount is unspecified.");
					}

					TransactionEntry newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = -importingTransaction.AmountTransferred.Value,
						AssetId = this.moneyFile.PreferredAssetId,
					};

					TransactionEntry newEntry2 = new()
					{
						AccountId = transferAccountId.Value,
						Amount = importingTransaction.AmountTransferred.Value,
						AssetId = this.moneyFile.PreferredAssetId,
					};

					newEntryTuples.Add((newTransaction, newEntry1));
					newEntryTuples.Add((newTransaction, newEntry2));
					break;
			}

			newTransactions.Add(newTransaction);
			transactionsImported++;
		}

		this.InsertAllTransactions(newTransactions, newEntryTuples);

		return transactionsImported;
	}

	private void InsertAllTransactions(List<Transaction> newTransactions, List<(Transaction Transaction, TransactionEntry Entry)> newEntryTuples)
	{
		// We first insert the transactions so we get IDs for all of them.
		// Then we can set the IDs on each TransactionEntry and insert those as well.
		this.moneyFile.InsertAll(newTransactions);
		List<TransactionEntry> newEntries = new(newEntryTuples.Count);
		foreach ((Transaction Transaction, TransactionEntry Entry) item in newEntryTuples)
		{
			item.Entry.TransactionId = item.Transaction.Id;
			newEntries.Add(item.Entry);
		}

		this.moneyFile.InsertAll(newEntries);
	}

	private int? FindTransferAccountId(string? category)
	{
		if (category is not null && category.Length > 2 && category[0] == '[' && category[^1] == ']')
		{
			string accountName = category[1..^1];
			return this.moneyFile.Accounts.FirstOrDefault(a => a.Name == accountName)?.Id;
		}

		return null;
	}
}
