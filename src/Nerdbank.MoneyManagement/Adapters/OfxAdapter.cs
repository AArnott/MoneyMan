// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft;
using Nerdbank.MoneyManagement.ViewModels;
using OfxNet;

namespace Nerdbank.MoneyManagement.Adapters;

public class OfxAdapter : IFileAdapter
{
	private static readonly Regex CheckCashedPattern = new(@"^Check #(?<checkNumber>\d+) Cashed$", RegexOptions.Compiled);
	private static readonly Regex ZelleMoneyReturnedPattern = new(@"^(?<memo>Zelle money returned) from (?<payee>.+)$", RegexOptions.Compiled);
	private readonly MoneyFile moneyFile;
	private readonly IUserNotification? userNotification;
	private readonly DocumentViewModel documentViewModel;

	/// <summary>
	/// Initializes a new instance of the <see cref="OfxAdapter"/> class.
	/// </summary>
	/// <param name="documentViewModel">The document that will be imported to or exported from.</param>
	public OfxAdapter(DocumentViewModel documentViewModel)
	{
		Requires.NotNull(documentViewModel, nameof(documentViewModel));

		this.moneyFile = documentViewModel.MoneyFile;
		this.userNotification = documentViewModel.UserNotification;
		this.documentViewModel = documentViewModel;
	}

	public TraceSource TraceSource { get; } = new TraceSource(nameof(OfxAdapter)) { Switch = { Level = SourceLevels.Warning } };

	public IReadOnlyList<AdapterFileType> FileTypes { get; } = new AdapterFileType[]
	{
		new("Open Financial Exchange", new[] { "ofx" }),
		new("Quicken Financial Exchange", new[] { "qfx" }),
	};

	public async Task<int> ImportAsync(string filePath, CancellationToken cancellationToken)
	{
		Requires.NotNullOrEmpty(filePath, nameof(filePath));

		cancellationToken.ThrowIfCancellationRequested();
		var ofx = OfxDocument.Load(filePath);
		var importedTransactionsCount = 0;
		IDisposable? batchImportTransaction = null;
		try
		{
			List<Transaction> newTransactions = new();
			List<(Transaction, TransactionEntry)> newEntryTuples = new();
			foreach (OfxStatement? statement in ofx.GetStatements())
			{
				cancellationToken.ThrowIfCancellationRequested();
				var bankStatement = statement as OfxBankStatement;
				Account? account = await this.FindMatchingAccountAsync(bankStatement?.Account, cancellationToken);

				if (account is null)
				{
					if (this.userNotification is null)
					{
						// Unable to ask the user to confirm the account to import into.
						continue;
					}

					StringBuilder prompt = new("Which account should this statement be imported into?");
					if (bankStatement?.Account is not null)
					{
						prompt.Append($" (Bank routing # {bankStatement.Account.AccountNumber}, {bankStatement.Account.AccountType} account # {bankStatement.Account.AccountNumber})");
					}

					account = await this.userNotification.ChooseAccountAsync(prompt.ToString(), null, cancellationToken);
					if (account is null)
					{
						continue;
					}
				}

				batchImportTransaction = this.moneyFile.UndoableTransaction($"Import transactions from {Path.GetFileNameWithoutExtension(filePath)}", this.documentViewModel.GetAccount(account.Id).BankingViewSelection);
				if (account is { Type: Account.AccountType.Banking } bankingAccount)
				{
					foreach (OfxStatementTransaction transaction in statement.TransactionList?.Transactions ?? Enumerable.Empty<OfxStatementTransaction>())
					{
						TransactionEntry existingEntry = this.moneyFile.TransactionEntries.FirstOrDefault(e => e.OfxFitId == transaction.FitId);
						if (existingEntry is not null)
						{
							if (existingEntry.Cleared == ClearedState.None)
							{
								existingEntry.Cleared = ClearedState.Cleared;
							}

							// Skip this transaction, as it has already been imported.
							continue;
						}

						TransactionEntry entryInError = this.moneyFile.TransactionEntries.FirstOrDefault(e => e.OfxFitId == transaction.CorrectFitId);
						if (!string.IsNullOrWhiteSpace(transaction.CorrectFitId) && entryInError is not null)
						{
							// We must update a previously downloaded entry.
							entryInError.OfxFitId = transaction.FitId;

							if (entryInError.Cleared == ClearedState.None)
							{
								entryInError.Cleared = ClearedState.Cleared;
							}

							// Eventually we should apply any corrections from the bank.
							importedTransactionsCount++;
							this.moneyFile.Update(entryInError);
							continue;
						}

						// This is a new transaction. Import it.
						Transaction bankingTransaction = new()
						{
							Memo = transaction.Memo, // Memo2?
							Payee = transaction.Payee?.Name,
							When = transaction.DatePosted.LocalDateTime.Date,
						};
						if (!string.IsNullOrWhiteSpace(transaction.Memo2))
						{
							bankingTransaction.Memo += $" {transaction.Memo2}";
						}

						TransactionEntry entry = new()
						{
							AccountId = account.Id,
							AssetId = this.moneyFile.PreferredAssetId,
							Amount = transaction.Amount,
							OfxFitId = transaction.FitId,
							Cleared = ClearedState.Cleared,
						};

						AdjustTransaction(bankingTransaction, entry);
						newTransactions.Add(bankingTransaction);
						newEntryTuples.Add((bankingTransaction, entry));
						importedTransactionsCount++;
					}
				}
				else
				{
					// Unsupported account type.
					continue;
				}
			}

			this.moneyFile.InsertAll(newTransactions);
			List<TransactionEntry> newEntries = new(newEntryTuples.Count);
			foreach ((Transaction, TransactionEntry) item in newEntryTuples)
			{
				item.Item2.TransactionId = item.Item1.Id;
				newEntries.Add(item.Item2);
			}

			this.moneyFile.InsertAll(newEntries);

			return importedTransactionsCount;
		}
		finally
		{
			batchImportTransaction?.Dispose();
		}
	}

	private static void AdjustTransaction(Transaction transaction, TransactionEntry ownEntry)
	{
		if (transaction.Memo is null)
		{
			return;
		}

		const string DepositFromPrefix = "Deposit from ";
		if (ownEntry.Amount > 0)
		{
			if (transaction.Memo.StartsWith(DepositFromPrefix, StringComparison.OrdinalIgnoreCase))
			{
				transaction.Payee = transaction.Memo.Substring(DepositFromPrefix.Length);
				transaction.Memo = null;
			}
			else if (ZelleMoneyReturnedPattern.Match(transaction.Memo) is { Success: true } match)
			{
				transaction.Payee = match.Groups["payee"].Value;
				transaction.Memo = match.Groups["memo"].Value;
			}
		}
		else if (ownEntry.Amount < 0)
		{
			const string WithdrawalFromPrefix = "Withdrawal from ";
			if (transaction.Memo.StartsWith(WithdrawalFromPrefix, StringComparison.OrdinalIgnoreCase) is true)
			{
				transaction.Payee = transaction.Memo.Substring(WithdrawalFromPrefix.Length);
				transaction.Memo = null;
			}
			else if (CheckCashedPattern.Match(transaction.Memo) is { Success: true } match)
			{
				if (int.TryParse(match.Groups["checkNumber"].Value, out var checkNumber))
				{
					transaction.CheckNumber = checkNumber;
					transaction.Memo = null;
				}
			}
		}
	}

	private async ValueTask<Account?> FindMatchingAccountAsync(OfxBankAccount? ofxAccount, CancellationToken cancellationToken)
	{
		Account? account = ofxAccount is null ? null : this.moneyFile.Accounts.FirstOrDefault(acct =>
			acct.OfxBankId == ofxAccount.BankId && acct.OfxAcctId == ofxAccount.AccountNumber);
		if (account is not null)
		{
			// We're highly confident about the match. Go for it.
			return account;
		}

		if (this.userNotification is not null)
		{
			StringBuilder prompt = new("Which account should this statement be imported into?");
			if (ofxAccount is not null)
			{
				prompt.Append($" (Bank routing # \"{ofxAccount.AccountNumber}\", \"{ofxAccount.AccountType}\" account # \"{ofxAccount.AccountNumber}\")");
			}

			AccountViewModel? accountViewModel = await this.userNotification.ChooseAccountAsync(prompt.ToString(), null, cancellationToken);
			if (accountViewModel is not null && ofxAccount is not null)
			{
				account = accountViewModel.Model;

				// Do what we can to remember user selection for next time.
				if (!string.IsNullOrWhiteSpace(ofxAccount.BankId))
				{
					account.OfxBankId = ofxAccount.BankId;
				}

				if (!string.IsNullOrWhiteSpace(ofxAccount.AccountNumber))
				{
					account.OfxAcctId = ofxAccount.AccountNumber;
					this.moneyFile.Update(account);
				}
			}
		}

		return account;
	}
}
