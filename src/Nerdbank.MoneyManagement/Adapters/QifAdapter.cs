﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft;
using Nerdbank.MoneyManagement.ViewModels;
using Nerdbank.Qif;

namespace Nerdbank.MoneyManagement.Adapters;

public class QifAdapter : IFileAdapter
{
	private readonly MoneyFile moneyFile;
	private readonly IUserNotification? userNotification;
	private readonly Dictionary<string, Account> categories = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Asset> assetsByName = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Asset> assetsBySymbol = new(StringComparer.OrdinalIgnoreCase);
	private QifDocument? importingDocument;

	public QifAdapter(DocumentViewModel documentViewModel)
	{
		// We don't store the document view model itself in a field to avoid the temptation to mix
		// use of the model and (much slower) view model APIs during import.
		this.moneyFile = documentViewModel.MoneyFile;
		this.userNotification = documentViewModel.UserNotification;
	}

	public TraceSource TraceSource { get; } = new TraceSource(nameof(QifAdapter)) { Switch = { Level = SourceLevels.Warning } };

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
			records += this.ImportAssets();
			records += this.ImportPrices();
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

	private IEnumerable<Qif.Transaction> GetAllTransactions() => this.importingDocument?.Transactions.Concat(this.importingDocument.Accounts.SelectMany(a => a.Transactions)) ?? Enumerable.Empty<Qif.Transaction>();

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
		foreach (Qif.Transaction transaction in this.GetAllTransactions())
		{
			if (transaction is Qif.BankTransaction bankTransaction)
			{
				AddCategory(bankTransaction.Category);
				foreach (BankSplit split in bankTransaction.Splits)
				{
					AddCategory(split.Category);
				}
			}
			else if (transaction is Qif.InvestmentTransaction investmentTransaction)
			{
				AddCategory(investmentTransaction.AccountForTransfer);
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

	private int ImportAssets()
	{
		Assumes.NotNull(this.importingDocument);

		foreach (Asset asset in this.moneyFile.Assets)
		{
			this.RecordAsset(asset);
		}

		List<Asset> addedAssets = new();
		foreach (Qif.Security security in this.importingDocument.Securities)
		{
			if (!this.assetsByName.TryGetValue(security.Name, out Asset? referencedAsset))
			{
				Asset asset = new()
				{
					Name = security.Name,
					TickerSymbol = security.Symbol,
					Type = Asset.AssetType.Security,
				};
				this.RecordAsset(asset);
				addedAssets.Add(asset);
			}
		}

		foreach (Qif.InvestmentTransaction transaction in this.GetAllTransactions().OfType<Qif.InvestmentTransaction>())
		{
			if (!string.IsNullOrEmpty(transaction.Security))
			{
				if (!this.assetsByName.TryGetValue(transaction.Security, out Asset? referencedAsset))
				{
					Asset asset = new()
					{
						Name = transaction.Security,
						Type = Asset.AssetType.Security,
					};
					this.RecordAsset(asset);
					addedAssets.Add(asset);
				}
			}
		}

		this.moneyFile.InsertAll(addedAssets);
		return addedAssets.Count;
	}

	private int ImportPrices()
	{
		Assumes.NotNull(this.importingDocument);
		int recordsImported = 0;

		List<AssetPrice> pricesToAdd = new();
		foreach (Qif.Price price in this.importingDocument.Prices)
		{
			if (this.assetsBySymbol.TryGetValue(price.Symbol, out Asset? asset))
			{
				pricesToAdd.Add(new AssetPrice
				{
					AssetId = asset.Id,
					ReferenceAssetId = this.moneyFile.PreferredAssetId,
					When = price.Date,
					PriceInReferenceAsset = price.Value,
				});
				recordsImported++;
			}
		}

		this.moneyFile.InsertAll(pricesToAdd, "OR IGNORE");

		return recordsImported;
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
			Account? transferAccount = this.FindTransferAccount(importingTransaction.Category);
			if (transferAccount is not null && transferAccount.Type != Account.AccountType.Category && importingTransaction.Amount >= 0 && target.Id != transferAccount.Id)
			{
				// When it comes to transfers, skip importing the transaction on the receiving side
				// so that we don't end up importing them on both ends and ending up with duplicates.
				// For splits with transfers it's even more important to drop the receiving side because
				// when it comes to paychecks, there is just one sender and many receiving accounts.
				// QIF only expresses the split on one account, so we always import a transaction with splits.
				// We get that for free because splits don't (tend to?) have transfer accounts mentioned in the Category.
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
					int? categoryId = this.FindCategoryId(importingTransaction.Category) ?? transferAccount?.Id;
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
						this.FindTransferAccount(split.Category)?.Id;
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
		Assumes.NotNull(target.CurrencyAssetId);

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
			};

			TransactionEntry? newEntry1, newEntry2, newEntry3;
			switch (importingTransaction.Action)
			{
				case InvestmentTransaction.Actions.XIn:
					// When it comes to transfers, skip importing the transaction on the receiving side
					// so that we don't end up importing them on both ends and ending up with duplicates.
					// For splits with transfers it's even more important to drop the receiving side because
					// when it comes to paychecks, there is just one sender and many receiving accounts.
					// QIF only expresses the split on one account, so we always import a transaction with splits.
					// We get that for free because splits don't (tend to?) have transfer accounts mentioned in the Category.
					continue;
				case InvestmentTransaction.Actions.XOut or InvestmentTransaction.Actions.WithdrwX or InvestmentTransaction.Actions.ContribX:
					newTransaction.Action = TransactionAction.Transfer;
					Account? transferAccount = this.FindTransferAccount(importingTransaction.AccountForTransfer);
					Verify.Operation(transferAccount is { CurrencyAssetId: not null }, "Transfer account isn't recognized or has not set a currency: {0}", importingTransaction.AccountForTransfer);
					Verify.Operation(importingTransaction.AmountTransferred is not null, "The transfer amount is unspecified.");

					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = -importingTransaction.AmountTransferred.Value,
						AssetId = target.CurrencyAssetId.Value,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					newEntry2 = new()
					{
						AccountId = transferAccount.Id,
						Amount = importingTransaction.AmountTransferred.Value,
						AssetId = transferAccount.CurrencyAssetId.Value,
					};
					newEntryTuples.Add((newTransaction, newEntry2));

					if (importingTransaction.Action == "XIn")
					{
						newEntry1.Amount *= -1;
						newEntry2.Amount *= -1;
					}

					break;
				case InvestmentTransaction.Actions.Cash:
					newTransaction.Action = importingTransaction.TransactionAmount < 0 ? TransactionAction.Withdraw : TransactionAction.Deposit;
					if (importingTransaction is not { TransactionAmount: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.TransactionAmount.Value,
						AssetId = this.moneyFile.PreferredAssetId,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					// Our view model doesn't support categories on deposits yet.
					if ((transferAccount = this.FindTransferAccount(importingTransaction.AccountForTransfer)) is not null)
					{
						if (transferAccount.Type != Account.AccountType.Category)
						{
							newTransaction.Action = TransactionAction.Transfer;
						}

						newEntry2 = new()
						{
							AccountId = transferAccount.Id,
							Amount = -importingTransaction.TransactionAmount.Value,
							AssetId = target.CurrencyAssetId.Value,
						};
						newEntryTuples.Add((newTransaction, newEntry2));
					}

					break;
				case InvestmentTransaction.Actions.Buy or InvestmentTransaction.Actions.Sell or InvestmentTransaction.Actions.BuyX or InvestmentTransaction.Actions.SellX:
					newTransaction.Action = TransactionAction.Buy;
					transferAccount = importingTransaction.Action is InvestmentTransaction.Actions.Buy or InvestmentTransaction.Actions.Sell ?
						target : this.FindTransferAccount(importingTransaction.AccountForTransfer);
					Verify.Operation(transferAccount is not null, "Transfer account not known.");
					if (importingTransaction is not { Quantity: not null, TransactionAmount: not null, Security: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Verify.Operation(this.assetsByName.TryGetValue(importingTransaction.Security, out Asset? asset), "No matching asset: {0}", importingTransaction.Security);

					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.Quantity.Value,
						AssetId = asset.Id,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					newEntry2 = null;
					if (importingTransaction.TransactionAmount.HasValue)
					{
						newEntry2 = new()
						{
							AccountId = transferAccount.Id,
							Amount = -importingTransaction.TransactionAmount.Value,
							AssetId = target.CurrencyAssetId.Value,
						};
						newEntryTuples.Add((newTransaction, newEntry2));
					}

					if (importingTransaction.Action is InvestmentTransaction.Actions.Sell or InvestmentTransaction.Actions.SellX)
					{
						newTransaction.Action = TransactionAction.Sell;
						newEntry1.Amount *= -1;
						if (newEntry2 is not null)
						{
							newEntry2.Amount *= -1;
						}
					}

					if (importingTransaction.Commission.HasValue)
					{
						newEntry3 = new()
						{
							AccountId = this.moneyFile.CurrentConfiguration.CommissionAccountId,
							Amount = importingTransaction.Commission.Value,
							AssetId = target.CurrencyAssetId.Value,
						};
						newEntryTuples.Add((newTransaction, newEntry3));
					}

					break;
				case InvestmentTransaction.Actions.IntInc:
					newTransaction.Action = TransactionAction.Interest;
					if (importingTransaction is not { TransactionAmount: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Assumes.NotNull(target.CurrencyAssetId);

					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.TransactionAmount.Value,
						AssetId = target.CurrencyAssetId.Value,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					break;
				case InvestmentTransaction.Actions.Div:
					newTransaction.Action = TransactionAction.Dividend;
					if (importingTransaction is not { TransactionAmount: not null, Security: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Assumes.NotNull(target.CurrencyAssetId);
					Verify.Operation(this.assetsByName.TryGetValue(importingTransaction.Security, out asset), "No matching asset: {0}", importingTransaction.Security);
					newTransaction.RelatedAssetId = asset.Id;

					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.TransactionAmount.Value,
						AssetId = target.CurrencyAssetId.Value,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					break;
				case InvestmentTransaction.Actions.ReinvDiv:
				case InvestmentTransaction.Actions.ReinvInt:
				case InvestmentTransaction.Actions.ReinvSh:
				case InvestmentTransaction.Actions.ReinvLg:
					newTransaction.Action = TransactionAction.Dividend;
					if (importingTransaction is not { Quantity: not null, Security: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Assumes.NotNull(target.CurrencyAssetId);
					Verify.Operation(this.assetsByName.TryGetValue(importingTransaction.Security, out asset), "No matching asset: {0}", importingTransaction.Security);

					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.Quantity.Value,
						AssetId = asset.Id,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					if (importingTransaction.TransactionAmount is not null)
					{
						// Create 2 more entries to represent that cash value was given,
						// then spent on the asset that was added to account.
						newEntry2 = new()
						{
							AccountId = target.Id,
							Amount = importingTransaction.TransactionAmount.Value,
							AssetId = target.CurrencyAssetId.Value,
						};
						newEntryTuples.Add((newTransaction, newEntry2));
						newEntry3 = new()
						{
							AccountId = target.Id,
							Amount = -importingTransaction.TransactionAmount.Value,
							AssetId = target.CurrencyAssetId.Value,
						};
						newEntryTuples.Add((newTransaction, newEntry3));
					}

					break;
				case InvestmentTransaction.Actions.ShrsIn:
					if (TryGetTransferOtherAccount(out Account? otherAccount))
					{
						// Skip transfers IN to avoid double-accounting.
						continue;
					}

					newTransaction.Action = TransactionAction.Add;
					if (importingTransaction is not { Quantity: not null, Security: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Verify.Operation(this.assetsByName.TryGetValue(importingTransaction.Security, out asset), "No matching asset: {0}", importingTransaction.Security);
					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.Quantity.Value,
						AssetId = asset.Id,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					break;
				case InvestmentTransaction.Actions.ShrsOut:
					newTransaction.Action = TransactionAction.Remove;
					if (importingTransaction is not { Quantity: not null, Security: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Verify.Operation(this.assetsByName.TryGetValue(importingTransaction.Security, out asset), "No matching asset: {0}", importingTransaction.Security);
					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = -importingTransaction.Quantity.Value,
						AssetId = asset.Id,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					// If we can recognize where the shares went, make this a transfer.
					if (TryGetTransferOtherAccount(out transferAccount))
					{
						newTransaction.Action = TransactionAction.Transfer;
						newEntry2 = new()
						{
							AccountId = transferAccount.Id,
							Amount = importingTransaction.Quantity.Value,
							AssetId = asset.Id,
						};
						newEntryTuples.Add((newTransaction, newEntry2));
					}

					break;
				case InvestmentTransaction.Actions.ShtSell:
					newTransaction.Action = TransactionAction.ShortSale;
					if (importingTransaction is not { Quantity: not null, Security: not null, TransactionAmount: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Verify.Operation(this.assetsByName.TryGetValue(importingTransaction.Security, out asset), "No matching asset: {0}", importingTransaction.Security);
					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = -importingTransaction.Quantity.Value,
						AssetId = asset.Id,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					newEntry2 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.TransactionAmount.Value,
						AssetId = target.CurrencyAssetId.Value,
					};
					newEntryTuples.Add((newTransaction, newEntry2));

					if (importingTransaction.Commission.HasValue)
					{
						newEntry3 = new()
						{
							AccountId = this.moneyFile.CurrentConfiguration.CommissionAccountId,
							Amount = importingTransaction.Commission.Value,
							AssetId = target.CurrencyAssetId.Value,
						};
						newEntryTuples.Add((newTransaction, newEntry3));
					}

					break;
				case InvestmentTransaction.Actions.CvrShrt:
					newTransaction.Action = TransactionAction.CoverShort;
					if (importingTransaction is not { Quantity: not null, Security: not null, TransactionAmount: not null })
					{
						LogIncompleteTransaction();
						continue;
					}

					Verify.Operation(this.assetsByName.TryGetValue(importingTransaction.Security, out asset), "No matching asset: {0}", importingTransaction.Security);
					newEntry1 = new()
					{
						AccountId = target.Id,
						Amount = importingTransaction.Quantity.Value,
						AssetId = asset.Id,
					};
					newEntryTuples.Add((newTransaction, newEntry1));

					newEntry2 = new()
					{
						AccountId = target.Id,
						Amount = -importingTransaction.TransactionAmount.Value,
						AssetId = target.CurrencyAssetId.Value,
					};
					newEntryTuples.Add((newTransaction, newEntry2));

					if (importingTransaction.Commission.HasValue)
					{
						newEntry3 = new()
						{
							AccountId = this.moneyFile.CurrentConfiguration.CommissionAccountId,
							Amount = importingTransaction.Commission.Value,
							AssetId = target.CurrencyAssetId.Value,
						};
						newEntryTuples.Add((newTransaction, newEntry3));
					}

					break;
				case InvestmentTransaction.Actions.StkSplit:
				case InvestmentTransaction.Actions.CGLong:
				case InvestmentTransaction.Actions.CGShort:
				case InvestmentTransaction.Actions.MargInt:
				default:
					this.TraceSource.TraceEvent(TraceEventType.Warning, 0, "Unsupported record type: {0}", importingTransaction.Action);
					break;
			}

			void LogIncompleteTransaction() => this.TraceSource.TraceEvent(TraceEventType.Warning, 0, "Skipping transaction in account \"{0}\" because it is missing required fields: {1}", target.Name, importingTransaction);
			bool TryGetTransferOtherAccount([NotNullWhen(true)] out Account? account)
			{
				const string xfrToPrefix = "Xfr To: ";
				const string xfrFromPrefix = "Xfr From: ";
				string accountName;
				if (importingTransaction.Memo?.StartsWith(xfrToPrefix, StringComparison.OrdinalIgnoreCase) is true)
				{
					accountName = importingTransaction.Memo.Substring(xfrToPrefix.Length);
				}
				else if (importingTransaction.Memo?.StartsWith(xfrFromPrefix, StringComparison.OrdinalIgnoreCase) is true)
				{
					accountName = importingTransaction.Memo.Substring(xfrFromPrefix.Length);
				}
				else
				{
					account = null;
					return false;
				}

				account = this.moneyFile.Accounts.FirstOrDefault(a => a.Name == accountName);
				return account is not null;
			}

			newTransactions.Add(newTransaction);
			transactionsImported++;

			foreach ((Transaction _, TransactionEntry entry) in newEntryTuples)
			{
				entry.Cleared = FromQifClearedState(importingTransaction.ClearedStatus);
			}
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

	private int? FindCategoryId(string? category)
	{
		return category is not null && this.categories.TryGetValue(category, out Account? account) ? account.Id : null;
	}

	/// <summary>
	/// Searches for an account matching a category or account name.
	/// </summary>
	/// <param name="categoryOrAccountName">The name of a category or an [account]. Accounts are only matched when surrounded by brackets.</param>
	/// <returns>The matching account, if any.</returns>
	private Account? FindTransferAccount(string? categoryOrAccountName)
	{
		if (categoryOrAccountName is not null)
		{
			if (categoryOrAccountName.Length > 2 && categoryOrAccountName[0] == '[' && categoryOrAccountName[^1] == ']')
			{
				string accountName = categoryOrAccountName[1..^1];
				return this.moneyFile.Accounts.FirstOrDefault(a => a.Name == accountName);
			}
			else if (this.categories.TryGetValue(categoryOrAccountName, out Account? account))
			{
				return account;
			}
		}

		return null;
	}

	private void RecordAsset(Asset asset)
	{
		this.assetsByName.Add(asset.Name, asset);
		if (asset.TickerSymbol is not null)
		{
			this.assetsBySymbol.TryAdd(asset.TickerSymbol, asset);
		}
	}
}
