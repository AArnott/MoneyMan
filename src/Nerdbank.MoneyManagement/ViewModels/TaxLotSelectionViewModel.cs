// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class TaxLotSelectionViewModel : BindableBase
{
	private readonly SortedObservableCollection<TaxLotAssignmentViewModel> assignments = new(TaxLotAssignmentSort.Instance);
	private decimal actual;
	private bool showAllTaxLots = true;

	public TaxLotSelectionViewModel(InvestingTransactionViewModel transaction)
	{
		this.Transaction = transaction;

		this.RegisterDependentProperty(nameof(this.SalePrice), nameof(this.SalePriceFormatted));
		this.RegisterDependentProperty(nameof(this.RequiredAssignments), nameof(this.AssignmentsDeltaLabel));
		this.RegisterDependentProperty(nameof(this.ActualAssignments), nameof(this.AssignmentsDeltaLabel));
		this.RegisterDependentProperty(nameof(this.ShowAllTaxLots), nameof(this.ShowAllTaxLotsLabel));

		this.SelectOptimal = new SelectionCommand(this, "Optimal", null);
		this.SelectOldest = new SelectionCommand(this, "Oldest", null);
		this.SelectNewest = new SelectionCommand(this, "Newest", null);
		this.SelectMinimumGain = new SelectionCommand(this, "Minimum gain", null);
		this.SelectMaximumGain = new SelectionCommand(this, "Maximum gain", null);
		this.ClearSelections = new SelectionCommand(this, "Clear", null);

		this.RefreshAssignments();
	}

	public IReadOnlyList<TaxLotAssignmentViewModel> Assignments => this.assignments;

	public string Explanation => "Which tax lots are dispensed with this transaction?";

	public string ShowAllTaxLotsLabel => this.ShowAllTaxLots ? "Showing all available lots" : "Showing only assigned lots";

	public bool ShowAllTaxLots
	{
		get => this.showAllTaxLots;
		set
		{
			if (this.SetProperty(ref this.showAllTaxLots, value))
			{
				this.RefreshAssignments();
			}
		}
	}

	public string AcquiredHeader => "Acquired";

	public string PriceHeader => "Price";

	public string AvailableHeader => "Available";

	public string AssignedHeader => "Assigned";

	public string GainLossHeader => "Gain/Loss";

	public bool IsGainLossColumnVisible => this.Transaction.Action is not TransactionAction.Transfer;

	public string? AssignmentsDeltaLabel
	{
		get
		{
			if (this.RequiredAssignments is null)
			{
				return null;
			}

			decimal target = this.RequiredAssignments.Value;
			decimal deltaRequired = target - this.ActualAssignments;
			return deltaRequired switch
			{
				< 0 => $"{this.ActualAssignments} assigned ➖ {Math.Abs(deltaRequired)} excess = {target}",
				> 0 => $"{this.ActualAssignments} assigned ➕ {deltaRequired} more = {target}",
				_ => $"{this.ActualAssignments} assigned ✅ as required.",
			};
		}
	}

	public decimal ActualAssignments
	{
		get => this.actual;
		internal set => this.SetProperty(ref this.actual, value);
	}

	public decimal? RequiredAssignments => this.Transaction.WithdrawAmount;

	public decimal? SalePrice => this.Transaction.SimplePrice;

	public string? SalePriceFormatted => this.Transaction.DepositAsset?.Format(this.SalePrice);

	public ICommand SelectOptimal { get; }

	public ICommand SelectOldest { get; }

	public ICommand SelectNewest { get; }

	public ICommand SelectMinimumGain { get; }

	public ICommand SelectMaximumGain { get; }

	public ICommand ClearSelections { get; }

	internal InvestingTransactionViewModel Transaction { get; }

	internal TransactionEntryViewModel? ConsumingTransactionEntry => this.Transaction.Entries.SingleOrDefault(e => e.ModelAmount < 0);

	protected MoneyFile MoneyFile => this.Transaction.MoneyFile;

	protected DocumentViewModel DocumentViewModel => this.Transaction.ThisAccount.DocumentViewModel;

	internal void OnTransactionEntry_PropertyChanged(TransactionEntryViewModel sender, PropertyChangedEventArgs args)
	{
		switch (args.PropertyName)
		{
			case nameof(TransactionEntryViewModel.ModelAmount):
				this.RefreshAssignments();
				break;
		}
	}

	internal void OnTransactionEntriesChanged(NotifyCollectionChangedEventArgs e)
	{
		this.RefreshAssignments();
	}

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

	internal void OnTaxLotAssignmentChanged(TaxLotAssignment tla)
	{
		this.RefreshAssignments();
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
		TransactionEntryViewModel? consumingTransactionEntry = this.ConsumingTransactionEntry;
		if (assetId is null || consumingTransactionEntry is null)
		{
			this.assignments.Clear();
			return;
		}

		Dictionary<int, TaxLotAssignment> existingLotAssignmentsByTaxLotId =
			this.MoneyFile.GetTaxLotAssignments(consumingTransactionEntry.Id).ToDictionary(tla => tla.TaxLotId, tla => tla);
		if (existingLotAssignmentsByTaxLotId.Count > 0 && this.assignments.All(tla => tla.Id == 0))
		{
			// Transition from all view model speculation to what the database actually tells us to do.
			// This may be a terrible way to synchronize our view model with the model,
			// but it gets our existing tests to pass.
			this.assignments.Clear();
		}

		HashSet<int> unobservedTaxLotIds = this.assignments.Select(a => a.TaxLotId).ToHashSet();
		Dictionary<int, TaxLotAssignmentViewModel> assignmentsByTaxLotId = this.assignments.ToDictionary(a => a.TaxLotId);
		SQLite.TableQuery<UnsoldAsset> unsoldAssets =
			from lot in this.MoneyFile.UnsoldAssets
			where lot.AssetId == assetId.Value
			where lot.AcquiredDate <= this.Transaction.When && lot.TransactionDate <= this.Transaction.When
			select lot;
		Dictionary<int, decimal> existingLotSelections =
			(from lot in this.MoneyFile.ConsumedTaxLots
			 where lot.ConsumingTransactionEntryId == consumingTransactionEntry.Id
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
				existingLotAssignmentsByTaxLotId.TryGetValue(lot.TaxLotId, out TaxLotAssignment? tla);
				this.assignments.Add(assignment = new TaxLotAssignmentViewModel(this.DocumentViewModel, this, tla));
			}

			assignment.CopyFrom(lot, consumingTransactionEntry, alreadyAssigned);
			if (assignment.Assigned == 0 && !this.ShowAllTaxLots)
			{
				this.assignments.Remove(assignment);
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
