// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class AccountViewModel : EntityViewModel<Account>
{
	private AssetViewModel? currencyAsset;
	private decimal value;
	private string name = string.Empty;
	private bool isClosed;
	private Account.AccountType type;

	public AccountViewModel(Account? model, DocumentViewModel documentViewModel)
		: base(documentViewModel.MoneyFile, model)
	{
		this.RegisterDependentProperty(nameof(this.Name), nameof(this.TransferTargetName));
		this.RegisterDependentProperty(nameof(this.IsEmpty), nameof(this.TypeIsReadOnly));
		this.RegisterDependentProperty(nameof(this.Value), nameof(this.ValueFormatted));

		this.DocumentViewModel = documentViewModel;
		this.CopyFrom(this.Model);
	}

	[Required]
	public string Name
	{
		get => this.name;
		set
		{
			Requires.NotNull(value, nameof(value));
			this.SetProperty(ref this.name, value);
		}
	}

	public abstract string? TransferTargetName { get; }

	/// <inheritdoc cref="Account.IsClosed"/>
	public bool IsClosed
	{
		get => this.isClosed;
		set => this.SetProperty(ref this.isClosed, value);
	}

	/// <inheritdoc cref="Account.Type"/>
	public Account.AccountType Type
	{
		get => this.type;
		set
		{
			Verify.Operation(this.type == value || this.IsEmpty, "Cannot change type of account when it contains transactions.");
			this.SetProperty(ref this.type, value);
		}
	}

	public bool TypeIsReadOnly => !this.IsEmpty;

	public string CurrencyAssetLabel => "Currency";

	public AssetViewModel? CurrencyAsset
	{
		get => this.currencyAsset;
		set
		{
			if (this.currencyAsset != value)
			{
				AssetViewModel? before = this.currencyAsset;
				this.SetProperty(ref this.currencyAsset, value);
				before?.NotifyUseChange();
				value?.NotifyUseChange();
			}
		}
	}

	public IEnumerable<AssetViewModel> CurrencyAssets => this.DocumentViewModel.AssetsPanel.Assets.Where(a => a.Type == Asset.AssetType.Currency);

	public bool CurrencyAssetIsReadOnly => !this.IsEmpty;

	/// <summary>
	/// Gets or sets the value of this account, measured in the user's preferred currency.
	/// </summary>
	/// <remarks>
	/// For ordinary banking accounts, this is simply the balance on the account (converted to the user's preferred currency as appropriate).
	/// For investment accounts, this so the net value of all positions held in that account.
	/// </remarks>
	public decimal Value
	{
		get => this.value;
		set => this.SetProperty(ref this.value, value);
	}

	public string? ValueFormatted => this.DocumentViewModel.DefaultCurrency?.Format(this.Value);

	protected internal DocumentViewModel DocumentViewModel { get; }

	/// <summary>
	/// Gets a value indicating whether the account is empty, and therefore able to change to another type.
	/// </summary>
	protected abstract bool IsEmpty { get; }

	/// <summary>
	/// Gets a value indicating whether this account has a populated collection of transaction view models.
	/// </summary>
	protected abstract bool IsPopulated { get; }

	protected string? DebuggerDisplay => this.Name;

	public abstract void DeleteTransaction(TransactionViewModel transaction);

	public abstract TransactionViewModel? FindTransaction(int? id);

	internal static AccountViewModel Create(Account model, DocumentViewModel documentViewModel)
	{
		AccountViewModel accountViewModel = Create(model, model.Type, documentViewModel);
		return accountViewModel;
	}

	/// <summary>
	/// Creates a new instance of this account of the appropriate derived runtime type to match the current value of <see cref="Type"/>.
	/// </summary>
	/// <returns>A new instance of <see cref="AccountViewModel"/>.</returns>
	internal AccountViewModel Recreate()
	{
		this.Model.Type = this.Type;
		AccountViewModel newViewModel = Create(this.Model, this.Type, this.DocumentViewModel);

		// Copy over the base view model properties manually in case it wasn't in a valid state to copy to the model.
		newViewModel.type = this.Type;
		newViewModel.name = this.Name;
		newViewModel.isClosed = this.IsClosed;

		return newViewModel;
	}

	internal virtual void NotifyTransactionDeleted(TransactionEntry transactionEntry)
	{
		if (!this.IsPopulated)
		{
			// Nothing to refresh.
			return;
		}

		if (this.FindTransaction(transactionEntry.TransactionId) is { } transactionViewModel)
		{
			this.RemoveTransactionFromViewModel(transactionViewModel);
		}
	}

	internal abstract void NotifyTransactionChanged(IReadOnlyList<TransactionAndEntry> transaction);

	internal abstract void NotifyAccountDeleted(ICollection<int> accountIds);

	protected static void ThrowOnUnexpectedAccountType(string parameterName, Account.AccountType expectedType, Account.AccountType actualType)
	{
		Requires.Argument(expectedType == actualType, parameterName, "Type mismatch. Expected {0} but was {1}.", expectedType, actualType);
	}

	protected abstract void RemoveTransactionFromViewModel(TransactionViewModel transaction);

	protected override bool IsPersistedProperty(string propertyName)
	{
		return base.IsPersistedProperty(propertyName)
			&& propertyName is not (nameof(this.TransferTargetName) or nameof(this.IsEmpty) or nameof(this.IsPopulated) or nameof(this.Value))
			&& !propertyName.EndsWith("Formatted");
	}

	protected override void ApplyToCore()
	{
		this.Model.Name = this.name;
		this.Model.IsClosed = this.IsClosed;
		this.Model.Type = this.Type;
		this.Model.CurrencyAssetId = this.CurrencyAsset?.Id;
	}

	protected override void CopyFromCore()
	{
		this.Name = this.Model.Name;
		this.IsClosed = this.Model.IsClosed;
		if (this.Model.Type != this.type)
		{
			this.type = this.Model.Type;
			this.OnPropertyChanged(nameof(this.Type));
		}

		this.CurrencyAsset = this.DocumentViewModel.GetAsset(this.Model.CurrencyAssetId);
		if (this.Model.IsPersisted)
		{
			this.Value = this.MoneyFile.GetValue(this.Model);
		}
	}

	protected IEnumerable<T> CreateEntryViewModels<T>(Func<IReadOnlyList<TransactionAndEntry>, T> viewModelFactory)
		where T : TransactionViewModel
	{
		// Our looping algorithm here depends on the enumerated transactions being sorted by TransactionId.
		List<TransactionAndEntry> group = new();
		foreach (TransactionAndEntry transactionAndEntry in this.MoneyFile.GetTopLevelTransactionsFor(this.Id))
		{
			if (group.Count == 0 || group[^1].TransactionId == transactionAndEntry.TransactionId)
			{
				// This entry belongs to this new or existing group.
				group.Add(transactionAndEntry);
			}
			else
			{
				// We have reached the first element of the next group, so flush the one we've been building up.
				T transactionViewModel = viewModelFactory(group);
				yield return transactionViewModel;

				// Now add this new row to the next group.
				group.Clear();
				group.Add(transactionAndEntry);
			}
		}

		// Flush out whatever makes up the last group.
		if (group.Count > 0)
		{
			T transactionViewModel = viewModelFactory(group);
			yield return transactionViewModel;
		}
	}

	private static AccountViewModel Create(Account model, Account.AccountType type, DocumentViewModel documentViewModel)
	{
		return type switch
		{
			Account.AccountType.Banking => new BankingAccountViewModel(model, documentViewModel),
			Account.AccountType.Investing => new InvestingAccountViewModel(model, documentViewModel),
			Account.AccountType.Category => new CategoryAccountViewModel(model, documentViewModel),
			_ => throw new NotSupportedException("Unexpected account type."),
		};
	}
}
