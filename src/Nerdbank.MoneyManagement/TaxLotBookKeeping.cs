// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using Microsoft;
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

		Dictionary<int, TaxLotAssignment> existingByTaxLotId = this.moneyFile.GetTaxLotAssignments(transactionEntryViewModel.Id)
			.ToDictionary(tla => tla.TaxLotId);
		decimal targetAmount = -transactionEntryViewModel.Model.Amount;
		decimal amountCurrentlyAssigned = existingByTaxLotId.Values.Sum(a => a.Amount);
		if (transactionEntryViewModel.Model.Amount >= 0 || transactionEntryViewModel.Asset is null)
		{
			// We are not consuming any tax lots. Clear any existing assignments.
			this.moneyFile.Delete(existingByTaxLotId.Values);
			return;
		}

		if (existingByTaxLotId.Count == 0)
		{
			IncreaseTaxLotAssignments();
			return;
		}

		// Are the existing assignments from a compatible asset?
		int anyLinkedTaxLotId = existingByTaxLotId.First().Value.TaxLotId;
		int existingAssetId =
			(from tl in this.moneyFile.TaxLots
			 where tl.Id == anyLinkedTaxLotId
			 join te in this.moneyFile.TransactionEntries on tl.CreatingTransactionEntryId equals te.Id
			 select te.AssetId).First();
		if (existingAssetId != transactionEntryViewModel.Asset.Id)
		{
			this.moneyFile.Delete(existingByTaxLotId.Values);
			amountCurrentlyAssigned = 0;
			IncreaseTaxLotAssignments();
			return;
		}

		if (amountCurrentlyAssigned < targetAmount)
		{
			// We need to increase tax lot assignments.
			IncreaseTaxLotAssignments();
			return;
		}
		else if (amountCurrentlyAssigned > targetAmount)
		{
			// We need to reduce tax lot assignments.
			decimal assignedSoFar = 0;
			foreach (TaxLotAssignment tla in existingByTaxLotId.Values)
			{
				if (assignedSoFar >= targetAmount)
				{
					this.moneyFile.Delete(tla);
				}
				else
				{
					if (assignedSoFar + tla.Amount > targetAmount)
					{
						// This is the first one that assigns too much. Reduce it.
						tla.Amount = targetAmount - assignedSoFar;
						this.moneyFile.Update(tla);
					}

					assignedSoFar += tla.Amount;
				}
			}
		}

		void IncreaseTaxLotAssignments()
		{
			List<TaxLotAssignment> newAssignments = new();
			SQLite.TableQuery<UnsoldAsset> unsoldAssets =
				from lot in this.moneyFile.UnsoldAssets
				where lot.AssetId == transactionEntryViewModel.Asset.Id
				orderby lot.AcquiredDate
				select lot;
			decimal remainingRequired = targetAmount - amountCurrentlyAssigned;
			Assumes.True(remainingRequired >= 0);
			foreach (UnsoldAsset unsold in unsoldAssets)
			{
				if (remainingRequired == 0)
				{
					break;
				}

				decimal amountToTake = Math.Min(unsold.RemainingAmount, remainingRequired);
				if (existingByTaxLotId.TryGetValue(unsold.TaxLotId, out TaxLotAssignment? existingAssignment))
				{
					existingAssignment.Amount += amountToTake;
					this.moneyFile.Update(existingAssignment);
				}
				else
				{
					newAssignments.Add(new()
					{
						Amount = amountToTake,
						ConsumingTransactionEntryId = transactionEntryViewModel.Id,
						TaxLotId = unsold.TaxLotId,
					});
				}

				remainingRequired -= amountToTake;
			}

			this.moneyFile.InsertAll(newAssignments);
		}
	}
}
