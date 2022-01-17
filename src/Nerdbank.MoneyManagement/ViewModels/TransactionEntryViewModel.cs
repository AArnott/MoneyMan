// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class TransactionEntryViewModel : EntityViewModel<TransactionEntry>
{
	private readonly TransactionViewModel parent;
	private string? memo;
	private AccountViewModel? account;
	private decimal amount;
	private AssetViewModel? asset;
	private ClearedState cleared;

	public TransactionEntryViewModel(TransactionViewModel parent, TransactionEntry? model = null)
		: base(parent.ThisAccount.MoneyFile, model)
	{
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.AmountFormatted));
		this.parent = parent;
		this.CopyFrom(this.Model);
	}

	public override bool IsReadyToSave => base.IsReadyToSave && this.Transaction.IsPersisted;

	/// <summary>
	/// Gets the <see cref="TransactionViewModel"/> to which this belongs.
	/// </summary>
	public TransactionViewModel Transaction => this.parent;

	/// <inheritdoc cref="TransactionViewModel.ThisAccount"/>
	public AccountViewModel ThisAccount => this.parent.ThisAccount;

	public string? Memo
	{
		get => this.memo;
		set => this.SetProperty(ref this.memo, value);
	}

	[Required]
	public AccountViewModel? Account
	{
		get => this.account;
		set => this.SetProperty(ref this.account, value);
	}

	/// <summary>
	/// Gets the set of accounts that this <see cref="TransactionEntryViewModel"/> may choose from to set its <see cref="Account"/> property.
	/// </summary>
	public IEnumerable<AccountViewModel> AvailableTransactionTargets
		=> this.ThisAccount.DocumentViewModel.TransactionTargets.Where(tt => tt != this.ThisAccount && tt != this.ThisAccount.DocumentViewModel.SplitCategory);

	public decimal Amount
	{
		get => this.amount;
		set => this.SetProperty(ref this.amount, value);
	}

	public string? AmountFormatted => this.ThisAccount?.CurrencyAsset?.Format(this.Amount);

	[Required]
	public AssetViewModel? Asset
	{
		get => this.asset;
		set => this.SetProperty(ref this.asset, value);
	}

	public ClearedState Cleared
	{
		get => this.cleared;
		set => this.SetProperty(ref this.cleared, value);
	}

	protected DocumentViewModel DocumentViewModel => this.ThisAccount.DocumentViewModel;

	private string DebuggerDisplay => $"TransactionEntry: ({this.Id}): {this.Memo} {this.Account?.Name} {this.Amount}";

	protected override void ApplyToCore()
	{
		Verify.Operation(this.Account is not null, "{0} must be set first.", nameof(this.Account));
		Verify.Operation(this.Asset is not null, "{0} must be set first.", nameof(this.Asset));

		this.Model.TransactionId = this.Transaction.TransactionId;
		this.Model.Memo = this.Memo;
		this.Model.AccountId = this.Account.Id;
		this.Model.Amount = this.Amount;
		this.Model.AssetId = this.Asset.Id;
		this.Cleared = this.Cleared;
	}

	protected override void CopyFromCore()
	{
		this.Memo = this.Model.Memo;
		this.Account = this.Model.AccountId == 0 ? null : this.DocumentViewModel.GetAccount(this.Model.AccountId);
		this.Amount = this.Model.Amount;
		this.Asset = this.DocumentViewModel.GetAsset(this.Model.AssetId);
		this.Cleared = this.Model.Cleared;
	}
}
