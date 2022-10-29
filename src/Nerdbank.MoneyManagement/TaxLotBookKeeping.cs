// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using Nerdbank.MoneyManagement.ViewModels;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Manages the relationships between tax lots and investment transactions.
/// </summary>
internal class TaxLotBookKeeping
{
	private readonly MoneyFile moneyFile;

	internal TaxLotBookKeeping(MoneyFile moneyFile)
	{
		this.moneyFile = moneyFile;
	}

	internal void UpdateLotAssignments(TransactionEntryViewModel transactionEntryViewModel)
	{
		if (transactionEntryViewModel.Transaction is not InvestingTransactionViewModel)
		{
			return;
		}

		if (transactionEntryViewModel.Amount >= 0 || transactionEntryViewModel.Asset is null)
		{
			// We are not consuming any tax lots. Clear any existing assignments.
			foreach (TaxLotAssignment row in this.moneyFile.TaxLotAssignments.Where(a => a.ConsumingTransactionEntryId == transactionEntryViewModel.Id))
			{
				this.moneyFile.Delete(row);
			}

			return;
		}

		// Do the assigned tax lots already match?
		// TODO: verify that the *type* of asset (still) matches as well.
		decimal sum = this.moneyFile.TaxLotAssignments.Sum(a => a.Amount);
		if (sum == transactionEntryViewModel.Amount)
		{
			// The tax lot assignments already match.
			return;
		}

		List<TaxLotAssignment> existing = this.moneyFile.TaxLotAssignments.Where(a => a.ConsumingTransactionEntryId == transactionEntryViewModel.Id).ToList();
		if (existing.Count == 0)
		{
			List<TaxLotAssignment> newAssignments = new();
			SQLite.TableQuery<UnsoldAsset> unsoldAssets =
				from lot in this.moneyFile.UnsoldAssets
				where lot.AssetId == transactionEntryViewModel.Asset.Id
				orderby lot.AcquiredDate
				select lot;
			decimal amountRequired = -transactionEntryViewModel.Amount;
			foreach (UnsoldAsset unsold in unsoldAssets)
			{
				decimal amountToTake = Math.Min(unsold.RemainingAmount, amountRequired);
				newAssignments.Add(new()
				{
					Amount = amountToTake,
					ConsumingTransactionEntryId = transactionEntryViewModel.Id,
					TaxLotId = unsold.TaxLotId,
				});
			}

			this.moneyFile.InsertAll(newAssignments);
		}
	}
}
