// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Text;
using Microsoft;
using Nerdbank.MoneyManagement.ViewModels;
using OfxNet;

namespace Nerdbank.MoneyManagement;

public class OfxAdapter
{
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

	/// <summary>
	/// Imports an OFX file.
	/// </summary>
	/// <param name="ofxFilePath">The path to the OFX file.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The number of statements that were imported successfully.</returns>
	public async Task<int> ImportOfxAsync(string ofxFilePath, CancellationToken cancellationToken)
	{
		Requires.NotNullOrEmpty(ofxFilePath, nameof(ofxFilePath));

		cancellationToken.ThrowIfCancellationRequested();
		OfxDocument ofx = OfxDocument.Load(ofxFilePath);
		int convertedStatementsCount = 0;
		IDisposable? batchImportTransaction = null;
		try
		{
			foreach (OfxStatement? statement in ofx.GetStatements())
			{
				cancellationToken.ThrowIfCancellationRequested();
				OfxBankStatement? bankStatement = statement as OfxBankStatement;
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

				batchImportTransaction = this.documentViewModel.MoneyFile.UndoableTransaction($"Import {Path.GetFileNameWithoutExtension(ofxFilePath)}", account.BankingViewSelection);
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
							continue;
						}

						// This is a new transaction. Import it.
						BankingTransactionViewModel bankingTransaction = bankingAccount.NewTransaction();
						bankingTransaction.Memo = transaction.Memo; // Memo2?
						if (!string.IsNullOrWhiteSpace(transaction.Memo2))
						{
							bankingTransaction.Memo += $" {transaction.Memo2}";
						}

						bankingTransaction.When = transaction.DatePosted.LocalDateTime.Date;
						bankingTransaction.Amount = transaction.Amount;
						bankingTransaction.Entries[0].OfxFitId = transaction.FitId;
						bankingTransaction.Cleared = ClearedState.Cleared;
					}
				}
				else
				{
					// Unsupported account type.
					continue;
				}

				convertedStatementsCount++;
			}

			return convertedStatementsCount;
		}
		finally
		{
			batchImportTransaction?.Dispose();
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
