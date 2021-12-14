// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class AccountViewModel : EntityViewModel<Account>, ITransactionTarget
{
	private decimal value;
	private string name = string.Empty;
	private bool isClosed;
	private Account.AccountType type;

	public AccountViewModel(Account? model, DocumentViewModel documentViewModel)
		: base(documentViewModel.MoneyFile)
	{
		this.RegisterDependentProperty(nameof(this.Name), nameof(this.TransferTargetName));
		this.RegisterDependentProperty(nameof(this.IsEmpty), nameof(this.TypeIsReadOnly));
		this.AutoSave = true;

		this.DocumentViewModel = documentViewModel;
		if (model is object)
		{
			this.CopyFrom(model);
		}
	}

	[Required]
	public string Name
	{
		get => this.name;
		set => this.SetProperty(ref this.name, value);
	}

	public string? TransferTargetName => $"[{this.Name}]";

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

	protected internal DocumentViewModel DocumentViewModel { get; }

	/// <summary>
	/// Gets a value indicating whether the account is empty, and therefore able to change to another type.
	/// </summary>
	protected abstract bool IsEmpty { get; }

	protected abstract bool IsPopulated { get; }

	protected string? DebuggerDisplay => this.Name;

	internal static AccountViewModel Create(Account model, DocumentViewModel documentViewModel)
	{
		AccountViewModel accountViewModel = Create(model, model.Type, documentViewModel);
		accountViewModel.CopyFrom(model);
		return accountViewModel;
	}

	/// <summary>
	/// Creates a new instance of this account of the appropriate derived runtime type to match the current value of <see cref="Type"/>.
	/// </summary>
	/// <returns>A new instance of <see cref="AccountViewModel"/>.</returns>
	internal AccountViewModel Recreate()
	{
		Verify.Operation(this.Model is object, "Model must exist.");
		AccountViewModel newViewModel = Create(this.Model, this.Type, this.DocumentViewModel);

		// Copy over the base view model properties manually in case it wasn't in a valid state to copy to the model.
		newViewModel.type = this.Type;
		newViewModel.name = this.Name;
		newViewModel.isClosed = this.IsClosed;

		return newViewModel;
	}

	internal virtual void NotifyTransactionDeleted(Transaction transaction)
	{
		if (!this.IsPopulated)
		{
			// Nothing to refresh.
			return;
		}

		if (this.FindTransaction(transaction.Id) is { } transactionViewModel)
		{
			this.RemoveTransactionFromViewModel(transactionViewModel);
		}
	}

	internal abstract void NotifyTransactionChanged(Transaction transaction);

	internal abstract EntityViewModel<Transaction>? FindTransaction(int id);

	protected abstract void RemoveTransactionFromViewModel(EntityViewModel<Transaction> transaction);

	protected override void ApplyToCore(Account account)
	{
		Requires.NotNull(account, nameof(account));

		account.Name = this.name;
		account.IsClosed = this.IsClosed;
		account.Type = this.Type;
	}

	protected override void CopyFromCore(Account account)
	{
		this.Name = account.Name;
		this.IsClosed = account.IsClosed;
		if (account.Type != this.type)
		{
			this.type = account.Type;
			this.OnPropertyChanged(nameof(this.Type));
		}
	}

	private static AccountViewModel Create(Account? model, Account.AccountType type, DocumentViewModel documentViewModel)
	{
		return type switch
		{
			Account.AccountType.Banking => new BankingAccountViewModel(model, documentViewModel),
			Account.AccountType.Investing => new InvestingAccountViewModel(model, documentViewModel),
			_ => throw new NotSupportedException("Unexpected account type."),
		};
	}
}
