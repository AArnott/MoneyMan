// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Text;
using IonTechnologies.Qif;
using IonTechnologies.Qif.Transactions;
using Microsoft;
using Nerdbank.MoneyManagement.ViewModels;

namespace Nerdbank.MoneyManagement.Adapters;

public class QifAdapter : IFileAdapter
{
	private readonly DocumentViewModel documentViewModel;

	public QifAdapter(DocumentViewModel documentViewModel)
	{
		Requires.NotNull(documentViewModel, nameof(documentViewModel));

		this.documentViewModel = documentViewModel;
	}

	public IReadOnlyList<AdapterFileType> FileTypes { get; } = new AdapterFileType[]
	{
		new("Quicken Interchange Format", new[] { "qif" }),
	};

	public async Task<int> ImportAsync(string filePath, CancellationToken cancellationToken)
	{
		Requires.NotNullOrEmpty(filePath, nameof(filePath));
		using FileStream file = File.OpenRead(filePath);
		var document = QifDocument.Load(file);
		int records = 0;

		foreach (CategoryListTransaction category in document.CategoryListTransactions)
		{
			CategoryAccountViewModel? viewModel = this.documentViewModel.FindCategory(category.CategoryName);
			if (viewModel is null)
			{
				viewModel = this.documentViewModel.CategoriesPanel.NewCategory(category.CategoryName);
				records++;
			}
		}

		if (this.documentViewModel.UserNotification is null)
		{
			// Unable to ask the user to confirm the account to import into.
			return records;
		}

		StringBuilder prompt = new("Which account should this statement be imported into?");
		AccountViewModel? account = await this.documentViewModel.UserNotification.ChooseAccountAsync(prompt.ToString(), null, cancellationToken);
		if (account is not BankingAccountViewModel bankingAccount)
		{
			return records;
		}

		int transactionsImported = 0;
		foreach (BasicTransaction transaction in document.CreditCardTransactions.Concat(document.BankTransactions))
		{
			transactionsImported++;
			BankingTransactionViewModel viewModel = bankingAccount.NewTransaction();
			viewModel.When = transaction.Date;
			viewModel.Payee = transaction.Payee;
			viewModel.Amount = transaction.Amount;
			viewModel.Memo = transaction.Memo;
			viewModel.Cleared = transaction.ClearedStatus switch
			{
				"*" => ClearedState.Cleared,
				"X" => ClearedState.Reconciled,
				_ => ClearedState.None,
			};
		}

		return transactionsImported;
	}
}
