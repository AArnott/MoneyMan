// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class TaxLotSelectionViewModel : BindableBase
{
	private readonly SortedObservableCollection<TaxLotAssignmentViewModel> assignments = new(TaxLotAssignmentSort.Instance);
	private decimal actual;

	public TaxLotSelectionViewModel(InvestingTransactionViewModel transaction)
	{
		this.Transaction = transaction;

		this.RegisterDependentProperty(nameof(this.SalePrice), nameof(this.SalePriceFormatted));

		this.SelectOldest = new SelectionCommand(this, "Oldest", null);
		this.SelectNewest = new SelectionCommand(this, "Newest", null);
		this.SelectMinimumGain = new SelectionCommand(this, "Minimum gain", null);
		this.SelectMaximumGain = new SelectionCommand(this, "Maximum gain", null);
		this.ClearSelections = new SelectionCommand(this, "Clear", null);

		this.RefreshAssignments();
	}

	public IReadOnlyList<TaxLotAssignmentViewModel> Assignments => this.assignments;

	public string Explanation => "Which tax lots are dispensed with this transaction?";

	public string AcquiredHeader => "Acquired";

	public string PriceHeader => "Price";

	public string AvailableHeader => "Available";

	public string AssignedHeader => "Assigned";

	public string GainLossHeader => "Gain/Loss";

	public bool IsGainLossColumnVisible => this.Transaction.Action is not TransactionAction.Transfer;

	public decimal ActualAssignments
	{
		get => this.actual;
		internal set => this.SetProperty(ref this.actual, value);
	}

	public decimal RequiredAssignments => this.Transaction.WithdrawAmount ?? 0;

	public decimal? SalePrice => this.Transaction.SimplePrice;

	public string? SalePriceFormatted => this.Transaction.DepositAsset?.Format(this.SalePrice);

	public ICommand SelectOldest { get; }

	public ICommand SelectNewest { get; }

	public ICommand SelectMinimumGain { get; }

	public ICommand SelectMaximumGain { get; }

	public ICommand ClearSelections { get; }

	internal InvestingTransactionViewModel Transaction { get; }

	protected MoneyFile MoneyFile => this.Transaction.MoneyFile;

	protected DocumentViewModel DocumentViewModel => this.Transaction.ThisAccount.DocumentViewModel;

	internal void OnTransactionPropertyChanged(string? propertyName)
	{
		switch (propertyName)
		{
			case nameof(InvestingTransactionViewModel.WithdrawAmount):
				this.OnPropertyChanged(nameof(this.RequiredAssignments));
				break;
			case nameof(InvestingTransactionViewModel.DepositAmount):
				this.OnPropertyChanged(nameof(this.SalePrice));
				break;
			case nameof(InvestingTransactionViewModel.When):
				this.RefreshAssignments();
				break;
			case nameof(InvestingTransactionViewModel.WithdrawAsset):
				this.RefreshAssignments();
				break;
			case nameof(InvestingTransactionViewModel.Action):
				this.OnPropertyChanged(nameof(this.IsGainLossColumnVisible));
				break;
		}
	}

	internal void OnAssignmentPropertyChanged(TaxLotAssignmentViewModel sender, string? propertyName)
	{
		switch (propertyName)
		{
			case nameof(TaxLotAssignmentViewModel.Assigned):
				this.RefreshActualAssignments();
				break;
		}
	}

	protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);
		foreach (TaxLotAssignmentViewModel assignment in this.assignments)
		{
			assignment.OnSelectionViewModelPropertyChanged(propertyName);
		}
	}

	private void RefreshAssignments()
	{
		int? assetId = this.Transaction.WithdrawAsset?.Id;
		if (assetId is null)
		{
			this.assignments.Clear();
			return;
		}

		int? consumingTransactionEntryId = this.Transaction.Entries.SingleOrDefault(e => e.ModelAmount < 0)?.Id;
		HashSet<int> unobservedTaxLotIds = this.assignments.Select(a => a.TaxLotId).ToHashSet();
		Dictionary<int, TaxLotAssignmentViewModel> assignmentsByTaxLotId = this.assignments.ToDictionary(a => a.TaxLotId);
		SQLite.TableQuery<UnsoldAsset> unsoldAssets =
			from lot in this.MoneyFile.UnsoldAssets
			where lot.AssetId == assetId.Value
			where lot.AcquiredDate <= this.Transaction.When && lot.TransactionDate <= this.Transaction.When
			select lot;
		IDictionary<int, decimal> existingLotSelections = consumingTransactionEntryId is null ? ImmutableDictionary<int, decimal>.Empty :
			(from lot in this.MoneyFile.ConsumedTaxLots
			 where lot.ConsumingTransactionEntryId == consumingTransactionEntryId.Value
			 group lot by lot.TaxLotId into lotSet
			 select (TaxLotId: lotSet.Key, Amount: lotSet.Sum(l => l.Amount))).ToDictionary(kv => kv.TaxLotId, kv => kv.Amount);
		foreach (UnsoldAsset lot in unsoldAssets)
		{
			if (existingLotSelections.TryGetValue(lot.TaxLotId, out decimal alreadyAssigned))
			{
				lot.RemainingAmount += alreadyAssigned;
			}

			if (lot.RemainingAmount <= 0)
			{
				continue;
			}

			unobservedTaxLotIds.Remove(lot.TaxLotId);
			if (!assignmentsByTaxLotId.TryGetValue(lot.TaxLotId, out TaxLotAssignmentViewModel? assignment))
			{
				this.assignments.Add(assignment = new TaxLotAssignmentViewModel(this.DocumentViewModel, this));
			}

			assignment.TaxLotId = lot.TaxLotId;
			assignment.AcquisitionDate = lot.AcquiredDate;
			assignment.Available = lot.RemainingAmount;
			assignment.CostBasisAsset = this.DocumentViewModel.GetAsset(lot.CostBasisAssetId) ?? this.Transaction.ThisAccount.CurrencyAsset;
			assignment.Assigned = alreadyAssigned;

			if (lot.CostBasisAmount.HasValue)
			{
				assignment.Price = lot.CostBasisAmount.Value / lot.AcquiredAmount;
			}
			else
			{
				this.MoneyFile.GetTransactionDetails(lot.TransactionId);
			}
		}

		// Purge any tax lots that are no longer an option.
		foreach (int taxLotId in unobservedTaxLotIds)
		{
			this.assignments.Remove(assignmentsByTaxLotId[taxLotId]);
		}

		this.RefreshActualAssignments();
	}

	private void RefreshActualAssignments()
	{
		decimal sum = 0;
		foreach (TaxLotAssignmentViewModel assignment in this.assignments)
		{
			sum += assignment.Assigned;
		}

		this.ActualAssignments = sum;
	}

	private TransactionEntryViewModel? GetCostBasis()
	{
		return null;
	}

	private class SelectionCommand : UICommandBase
	{
		private readonly TaxLotSelectionViewModel viewModel;

		private readonly Action? action;

		internal SelectionCommand(TaxLotSelectionViewModel viewModel, string caption, Action? action)
		{
			this.viewModel = viewModel;
			this.Caption = caption;
			this.action = action;
		}

		public override string Caption { get; }

		public override bool CanExecute(object? parameter = null) => this.action is not null;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			Verify.Operation(this.action is not null, "This command is disabled.");
			this.action();
			return Task.CompletedTask;
		}
	}
}
