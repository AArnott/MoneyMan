// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class TransactionViewModel : EntityViewModel
{
	private DateTime when;
	private string? memo;
	private ObservableCollection<TransactionEntryViewModel> entries;
	private Transaction transaction;

	public TransactionViewModel(AccountViewModel thisAccount, IReadOnlyList<TransactionAndEntry> models)
		: base(thisAccount.MoneyFile)
	{
		this.ThisAccount = thisAccount;
		this.SplitModels(models, out this.transaction, out this.entries);
	}

	/// <summary>
	/// Gets the account this transaction was created to be displayed within.
	/// </summary>
	public AccountViewModel ThisAccount { get; }

	public Transaction Transaction
	{
		get => this.transaction;
		private set => this.transaction = value;
	}

	public IReadOnlyList<TransactionEntryViewModel> Entries => this.entries;

	public int TransactionId => this.Transaction.Id;

	public override bool IsPersisted => this.TransactionId > 0;

	/// <inheritdoc cref="Transaction.When"/>
	public DateTime When
	{
		get => this.when;
		set => this.SetProperty(ref this.when, value);
	}

	public string? Memo
	{
		get => this.memo;
		set => this.SetProperty(ref this.memo, value);
	}

	protected override ModelBase? UndoTarget => this.Transaction;

	/// <summary>
	/// Updates this view model and those in <see cref="Entries"/> to match those in the specified models.
	/// </summary>
	/// <param name="models">The new models to (re)initialize based on.</param>
	public void CopyFrom(IReadOnlyList<TransactionAndEntry> models)
	{
		Requires.Argument(models.Count == 0 || models[0].TransactionId == this.Transaction.Id, nameof(models), "The entity ID does not match.");
		this.SplitModels(models, out this.transaction, out this.entries);
		this.CopyFromCore();
	}

	internal void NotifyAccountDeleted(ICollection<int> accountIds)
	{
		List<TransactionEntryViewModel>? impactedEntries = null;
		foreach (TransactionEntryViewModel entry in this.Entries)
		{
			if (entry.Account is not null && accountIds.Contains(entry.Account.Id))
			{
				impactedEntries ??= new();
				impactedEntries.Add(entry);
			}
		}

		if (impactedEntries is not null)
		{
			foreach (TransactionEntryViewModel removedEntry in impactedEntries)
			{
				this.entries.Remove(removedEntry);
			}
		}
	}

	internal void NotifyReassignCategory(ICollection<CategoryAccountViewModel> oldCategories, CategoryAccountViewModel? newCategory)
	{
		List<TransactionEntryViewModel>? entriesToDelete = null;
		foreach (TransactionEntryViewModel entry in this.Entries)
		{
			if (oldCategories.Contains(entry.Account))
			{
				if (newCategory is null)
				{
					entriesToDelete ??= new();
					entriesToDelete.Add(entry);
				}
				else
				{
					entry.Account = newCategory;
				}
			}
		}

		if (entriesToDelete is not null)
		{
			foreach (TransactionEntryViewModel entry in entriesToDelete)
			{
				this.entries.Remove(entry);
			}
		}
	}

	/// <summary>
	/// Copies fields from <see cref="Transaction"/> to this view model.
	/// </summary>
	/// <remarks>
	/// Overrides of this method should call the base method, and should <em>not</em> call <see cref="EntityViewModel{TEntity}.CopyFrom(TEntity)"/>
	/// on the elements of <see cref="Entries"/> because the <see cref="TransactionViewModel"/> class handles this.
	/// </remarks>
	protected virtual void CopyFromCore()
	{
		this.When = this.Transaction.When;
		this.Memo = this.Transaction.Memo;

		foreach (TransactionEntryViewModel entry in this.entries)
		{
			entry.CopyFrom(entry.Model);
		}
	}

	/// <summary>
	/// Writes the properties in this view model and the <see cref="Entries"/> to the underlying models.
	/// </summary>
	/// <remarks>
	/// Overrides of this method should call the base method after making sure the <see cref="Entries"/> collection is up to date,
	/// and should <em>not</em> call <see cref="EntityViewModel{TEntity}.CopyFrom(TEntity)"/>
	/// on the elements of <see cref="Entries"/> because the <see cref="TransactionViewModel"/> class handles this.
	/// </remarks>
	protected override void ApplyToCore()
	{
		this.Transaction.When = this.When;
		this.Transaction.Memo = this.Memo;

		foreach (TransactionEntryViewModel entry in this.entries)
		{
			entry.ApplyToModel();
		}
	}

	protected override void SaveCore()
	{
		this.MoneyFile.InsertOrReplace(this.Transaction);
	}

	private void SplitModels(IReadOnlyList<TransactionAndEntry> models, out Transaction transaction, out ObservableCollection<TransactionEntryViewModel> entries)
	{
		if (models.Count == 0)
		{
			transaction = new();
			entries = new();
		}
		else
		{
			transaction = new Transaction(models[0]);
			entries = new ObservableCollection<TransactionEntryViewModel>(models.Select(te => new TransactionEntryViewModel(this, new TransactionEntry(te))));
		}
	}
}
