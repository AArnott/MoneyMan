// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

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
	private readonly DocumentViewModel documentViewModel;

	/// <summary>
	/// Initializes a new instance of the <see cref="OfxAdapter"/> class.
	/// </summary>
	/// <param name="documentViewModel">The document that will be imported to or exported from.</param>
	public OfxAdapter(DocumentViewModel documentViewModel)
	{
		Requires.NotNull(documentViewModel, nameof(documentViewModel));

		this.documentViewModel = documentViewModel;
	}

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
			foreach (OfxStatement? statement in ofx.GetStatements())
			{
				cancellationToken.ThrowIfCancellationRequested();
				var bankStatement = statement as OfxBankStatement;
				AccountViewModel? account = await this.FindMatchingAccountAsync(bankStatement?.Account, cancellationToken);

				if (account is null)
				{
					if (this.documentViewModel.UserNotification is null)
					{
						// Unable to ask the user to confirm the account to import into.
						continue;
					}

					StringBuilder prompt = new("Which account should this statement be imported into?");
					if (bankStatement is not null)
					{
						prompt.Append($" (Bank routing # {bankStatement.Account.AccountNumber}, {bankStatement.Account.AccountType} account # {bankStatement.Account.AccountNumber})");
					}

					account = await this.documentViewModel.UserNotification.ChooseAccountAsync(prompt.ToString(), account, cancellationToken);
					if (account is null)
					{
						continue;
					}
				}

				batchImportTransaction = this.documentViewModel.MoneyFile.UndoableTransaction($"Import {Path.GetFileNameWithoutExtension(filePath)}", account.BankingViewSelection);
				if (account is BankingAccountViewModel bankingAccount)
				{
					foreach (OfxStatementTransaction transaction in statement.TransactionList.Transactions)
					{
						if (bankingAccount.FindTransactionEntryByFitId(transaction.FitId) is TransactionEntryViewModel existingEntry)
						{
							if (existingEntry.Cleared == ClearedState.None)
							{
								existingEntry.Cleared = ClearedState.Cleared;
							}

							// Skip this transaction, as it has already been imported.
							continue;
						}

						if (!string.IsNullOrWhiteSpace(transaction.CorrectFitId) && bankingAccount.FindTransactionEntryByFitId(transaction.CorrectFitId) is TransactionEntryViewModel entryInError)
						{
							// We must update a previously downloaded entry.
							entryInError.OfxFitId = transaction.FitId;

							if (entryInError.Cleared == ClearedState.None)
							{
								entryInError.Cleared = ClearedState.Cleared;
							}

							// Eventually we should apply any corrections from the bank.
							importedTransactionsCount++;
							continue;
						}

						// This is a new transaction. Import it.
						BankingTransactionViewModel bankingTransaction = bankingAccount.NewTransaction();
						bankingTransaction.Memo = transaction.Memo; // Memo2?
						if (!string.IsNullOrWhiteSpace(transaction.Memo2))
						{
							bankingTransaction.Memo += $" {transaction.Memo2}";
						}

						bankingTransaction.Payee = transaction.Payee?.Name;
						bankingTransaction.When = transaction.DatePosted.LocalDateTime.Date;
						bankingTransaction.Amount = transaction.Amount;
						bankingTransaction.Entries[0].OfxFitId = transaction.FitId;
						bankingTransaction.Cleared = ClearedState.Cleared;

						AdjustTransaction(bankingTransaction);
						importedTransactionsCount++;
					}
				}
				else
				{
					// Unsupported account type.
					continue;
				}
			}

			return importedTransactionsCount;
		}
		finally
		{
			batchImportTransaction?.Dispose();
		}
	}

	private static void AdjustTransaction(BankingTransactionViewModel bankingTransaction)
	{
		if (bankingTransaction.Memo is null)
		{
			return;
		}

		const string DepositFromPrefix = "Deposit from ";
		if (bankingTransaction.Amount > 0)
		{
			if (bankingTransaction.Memo.StartsWith(DepositFromPrefix, StringComparison.OrdinalIgnoreCase))
			{
				bankingTransaction.Payee = bankingTransaction.Memo.Substring(DepositFromPrefix.Length);
				bankingTransaction.Memo = null;
			}
			else if (ZelleMoneyReturnedPattern.Match(bankingTransaction.Memo) is { Success: true } match)
			{
				bankingTransaction.Payee = match.Groups["payee"].Value;
				bankingTransaction.Memo = match.Groups["memo"].Value;
			}
		}
		else if (bankingTransaction.Amount < 0)
		{
			const string WithdrawalFromPrefix = "Withdrawal from ";
			if (bankingTransaction.Memo.StartsWith(WithdrawalFromPrefix, StringComparison.OrdinalIgnoreCase) is true)
			{
				bankingTransaction.Payee = bankingTransaction.Memo.Substring(WithdrawalFromPrefix.Length);
				bankingTransaction.Memo = null;
			}
			else if (CheckCashedPattern.Match(bankingTransaction.Memo) is { Success: true } match)
			{
				if (int.TryParse(match.Groups["checkNumber"].Value, out var checkNumber))
				{
					bankingTransaction.CheckNumber = checkNumber;
					bankingTransaction.Memo = null;
				}
			}
		}
	}

	private async ValueTask<AccountViewModel?> FindMatchingAccountAsync(OfxBankAccount? account, CancellationToken cancellationToken)
	{
		AccountViewModel? viewModel = account is null ? null : this.documentViewModel.AccountsPanel.Accounts.FirstOrDefault(acct =>
			acct.OfxBankId == account.BankId && acct.OfxAcctId == account.AccountNumber);
		if (viewModel is not null)
		{
			// We're highly confident about the match. Go for it.
			return viewModel;
		}

		if (this.documentViewModel.UserNotification is not null)
		{
			StringBuilder prompt = new("Which account should this statement be imported into?");
			if (account is not null)
			{
				prompt.Append($" (Bank routing # \"{account.AccountNumber}\", \"{account.AccountType}\" account # \"{account.AccountNumber}\")");
			}

			viewModel = await this.documentViewModel.UserNotification.ChooseAccountAsync(prompt.ToString(), viewModel, cancellationToken);
			if (viewModel is not null && account is not null)
			{
				// Do what we can to remember user selection for next time.
				if (!string.IsNullOrWhiteSpace(account.BankId))
				{
					viewModel.OfxBankId = account.BankId;
				}

				if (!string.IsNullOrWhiteSpace(account.AccountNumber))
				{
					viewModel.OfxAcctId = account.AccountNumber;
				}
			}
		}

		return viewModel;
	}
}
