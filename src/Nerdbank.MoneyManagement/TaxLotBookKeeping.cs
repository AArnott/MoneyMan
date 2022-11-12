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

		List<TaxLotAssignment> existing = this.moneyFile.TaxLotAssignments.Where(a => a.ConsumingTransactionEntryId == transactionEntryViewModel.Id).ToList();
		if (transactionEntryViewModel.Amount >= 0 || transactionEntryViewModel.Asset is null)
		{
			// We are not consuming any tax lots. Clear any existing assignments.
			this.moneyFile.Delete(existing);
			return;
		}

		if (existing.Count == 0)
		{
			BuildUpFromNothing();
			return;
		}

		// Are the existing assignments from a compatible asset?
		int anyLinkedTaxLotId = existing[0].TaxLotId;
		int existingAssetId =
			(from tl in this.moneyFile.TaxLots
			 where tl.Id == anyLinkedTaxLotId
			 join te in this.moneyFile.TransactionEntries on tl.CreatingTransactionEntryId equals te.Id
			 select te.AssetId).First();
		if (existingAssetId != transactionEntryViewModel.Asset.Id)
		{
			this.moneyFile.Delete(existing);
			BuildUpFromNothing();
			return;
		}

		decimal sum = -existing.Sum(a => a.Amount);
		if (sum < transactionEntryViewModel.Amount)
		{
			// We need to assign more tax lots
		}
		else if (sum > transactionEntryViewModel.Amount)
		{
			// We need to reduce tax lot assignments.
		}

		void BuildUpFromNothing()
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
