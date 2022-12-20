// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft;
using Nerdbank.MoneyManagement.ViewModels;
using Nerdbank.Qif;

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

	internal bool IsTaxLotCreationAppropriate(TransactionEntryViewModel transactionEntryViewModel)
		=> transactionEntryViewModel.Transaction is InvestingTransactionViewModel { Action: TransactionAction.Add or TransactionAction.Buy or TransactionAction.ShortSale or TransactionAction.Transfer } && transactionEntryViewModel.ModelAmount > 0;

	/// <summary>
	/// Adds, deletes or updates tax lots created by a given transaction entry.
	/// </summary>
	/// <param name="transactionEntryViewModel">The view model of the transaction entry to update created tax lots for.</param>
	internal void UpdateLotCreations(TransactionEntryViewModel transactionEntryViewModel)
	{
		if (transactionEntryViewModel.CreatedTaxLots is null)
		{
			// This should only happen when IsTaxLotCreationAppropriate returns false,
			// and we should therefore purge all persisted tax lots.
			this.moneyFile.PurgeTaxLotsCreatedBy(transactionEntryViewModel.Id);
			return;
		}

		decimal amountCurrentlyAssigned = transactionEntryViewModel.CreatedTaxLots.Sum(lot => lot.Amount);

		// If this is a transfer, the tax lots are required to match on both sides.
		InvestingTransactionViewModel? investingTx = transactionEntryViewModel.Transaction as InvestingTransactionViewModel;
		if (investingTx is { Action: TransactionAction.Transfer })
		{
			Assumes.True(TryFindOtherEntry(out TransactionEntryViewModel? fromEntry));
			List<ConsumedTaxLot> consumedTaxLots = this.moneyFile.ConsumedTaxLots.Where(tla => tla.ConsumingTransactionEntryId == fromEntry.Id).ToList();

			// Add or remove tax lots created by this transfer till the count of tax lots created equal the number of tax lots consumed.
			transactionEntryViewModel.CreatedTaxLots.AddRange(Enumerable.Range(0, consumedTaxLots.Count - transactionEntryViewModel.CreatedTaxLots.Count).Select(_ => CreateTaxLot()));
			transactionEntryViewModel.CreatedTaxLots.RemoveRange(consumedTaxLots.Count, transactionEntryViewModel.CreatedTaxLots.Count - consumedTaxLots.Count);

			// Now initialize each created tax lot based on the values of the consumed tax lots.
			for (int i = 0; i < transactionEntryViewModel.CreatedTaxLots.Count; i++)
			{
				ConsumedTaxLot consumedTaxLot = consumedTaxLots[i];
				TaxLotViewModel createdTaxLot = transactionEntryViewModel.CreatedTaxLots[i];
				createdTaxLot.AcquiredDate = consumedTaxLot.AcquiredDate;
				createdTaxLot.Amount = consumedTaxLot.Amount;
				createdTaxLot.CostBasisAmount = consumedTaxLot.CostBasisAmount;
				createdTaxLot.CostBasisAsset = transactionEntryViewModel.DocumentViewModel.GetAsset(consumedTaxLot.CostBasisAssetId);
			}
		}
		else
		{
			if (transactionEntryViewModel.CreatedTaxLots.Count > 1)
			{
				// Remove excess tax lots.
				transactionEntryViewModel.CreatedTaxLots.RemoveRange(1, transactionEntryViewModel.CreatedTaxLots.Count - 1);
			}

			// Update the amounts in the tax lots.
			TaxLotViewModel? taxLotViewModel = GetOrCreateFirstTaxLot();
			taxLotViewModel.AmountIsInherited = true;
			taxLotViewModel.AcquiredDateIsInherited = true;

			// Update cost basis if we can.
			if (TryFindCostBasisEntry(out TransactionEntryViewModel? costBasis))
			{
				taxLotViewModel.CostBasisAmount = Math.Abs(costBasis.Amount);
				taxLotViewModel.CostBasisAsset = costBasis.Asset;
			}
			else if (investingTx is { AcquisitionPrice: not null })
			{
				taxLotViewModel.AcquiredDate = investingTx.AcquisitionDate;
				taxLotViewModel.CostBasisAmount = investingTx.AcquisitionPrice * transactionEntryViewModel.Amount;
				taxLotViewModel.CostBasisAsset = investingTx.ThisAccount.CurrencyAsset;
			}
		}

		this.moneyFile.PurgeTaxLotsCreatedBy(transactionEntryViewModel.Id, transactionEntryViewModel.CreatedTaxLots.Select(e => e.Id));
		foreach (TaxLotViewModel lot in transactionEntryViewModel.CreatedTaxLots)
		{
			lot.Save();
		}

		TaxLotViewModel CreateTaxLot()
		{
			TaxLotViewModel result = new(transactionEntryViewModel.DocumentViewModel, transactionEntryViewModel);
			transactionEntryViewModel.CreatedTaxLots.Add(result);
			return result;
		}

		TaxLotViewModel GetOrCreateFirstTaxLot() => transactionEntryViewModel.CreatedTaxLots.Count > 0 ? transactionEntryViewModel.CreatedTaxLots[0] : CreateTaxLot();

		bool TryFindOtherEntry([NotNullWhen(true)] out TransactionEntryViewModel? other)
		{
			if (transactionEntryViewModel.Transaction.Entries.Count == 2)
			{
				other = transactionEntryViewModel.Transaction.Entries[transactionEntryViewModel.Transaction.Entries[0] == transactionEntryViewModel ? 1 : 0];
				return true;
			}

			other = null;
			return false;
		}

		bool TryFindCostBasisEntry([NotNullWhen(true)] out TransactionEntryViewModel? costBasis)
		{
			// A simple transaction with only two entries makes finding the 'cost' basis for this fairly trivial.
			costBasis = null;
			return transactionEntryViewModel.Transaction is InvestingTransactionViewModel && TryFindOtherEntry(out costBasis);
		}
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
				where lot.AssetId == transactionEntryViewModel.Asset.Id && lot.RemainingAmount > 0
				where lot.AcquiredDate <= transactionEntryViewModel.Transaction.When && lot.TransactionDate <= transactionEntryViewModel.Transaction.When
				orderby lot.AcquiredDate, lot.RemainingAmount // keep this in sync with TaxLotAssignmentSort
				select lot;
			decimal remainingRequired = targetAmount - amountCurrentlyAssigned;
			Assumes.True(remainingRequired >= 0);
			foreach (UnsoldAsset unsold in unsoldAssets)
			{
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

				if (remainingRequired == 0)
				{
					break;
				}
			}

			this.moneyFile.InsertAll(newAssignments);
		}
	}
}
