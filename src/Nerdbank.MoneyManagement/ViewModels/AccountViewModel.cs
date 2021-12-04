// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

public class AccountViewModel : EntityViewModel<Account>, ITransactionTarget
{
	private string name = string.Empty;
	private bool isClosed;
	private Account.AccountType type;

	public AccountViewModel(Account? model, DocumentViewModel documentViewModel)
		: base(documentViewModel.MoneyFile)
	{
		this.RegisterDependentProperty(nameof(this.Name), nameof(this.TransferTargetName));
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
		set => this.SetProperty(ref this.type, value);
	}

	protected internal DocumentViewModel DocumentViewModel { get; }

	protected string? DebuggerDisplay => this.Name;

	internal static AccountViewModel Create(Account model, DocumentViewModel documentViewModel)
	{
		return model.Type switch
		{
			Account.AccountType.Banking => new BankingAccountViewModel(model, documentViewModel),
			Account.AccountType.Investing => new InvestingAccountViewModel(model, documentViewModel),
			_ => throw new NotSupportedException("Unexpected account type."),
		};
	}

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
		this.Type = account.Type;
	}
}
