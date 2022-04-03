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
	private string? ofxFitId;
	private string? memo;
	private AccountViewModel? account;
	private decimal amount;
	private AssetViewModel? asset;
	private ClearedState cleared;

	public TransactionEntryViewModel(TransactionViewModel parent, TransactionEntry? model = null)
		: base(parent.ThisAccount.MoneyFile, model)
	{
		this.AutoSave = false;
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.AmountFormatted));
		this.parent = parent;
		this.CopyFrom(this.Model);
		this.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName is object && this.IsPersistedProperty(e.PropertyName))
			{
				parent.Entry_PropertyChanged(this, e);
			}
		};
	}

	public override bool IsReadyToSave => this.IsReadyToSaveIsolated && this.Transaction.IsPersisted;

	public bool IsReadyToSaveIsolated => base.IsReadyToSave && !this.IsApplyingToModel;

	public bool IsEmpty => string.IsNullOrWhiteSpace(this.Memo) && this.Amount == 0 && this.Cleared == ClearedState.None && (this.Account is null || this.Account == this.ThisAccount);

	/// <summary>
	/// Gets the <see cref="TransactionViewModel"/> to which this belongs.
	/// </summary>
	public TransactionViewModel Transaction => this.parent;

	/// <inheritdoc cref="TransactionViewModel.ThisAccount"/>
	public AccountViewModel ThisAccount => this.parent.ThisAccount;

	/// <inheritdoc cref="TransactionEntry.OfxFitId"/>
	public string? OfxFitId
	{
		get => this.ofxFitId;
		set => this.SetProperty(ref this.ofxFitId, value);
	}

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
	public IEnumerable<AccountViewModel?> AvailableTransactionTargets
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
		this.Model.Amount = this.NegateAmountIfAppropriate(this.Amount);
		this.Model.AssetId = this.Asset.Id;
		this.Model.Cleared = this.Cleared;
		this.Model.OfxFitId = this.OfxFitId;
	}

	protected override void CopyFromCore()
	{
		this.Memo = this.Model.Memo;
		this.Account = this.Model.AccountId == 0 ? null : this.DocumentViewModel.GetAccount(this.Model.AccountId);
		this.Amount = this.NegateAmountIfAppropriate(this.Model.Amount);
		this.Asset = this.DocumentViewModel.GetAsset(this.Model.AssetId);
		this.Cleared = this.Model.Cleared;
		this.OfxFitId = this.Model.OfxFitId;
	}

	/// <summary>
	/// Negates an amount if this transaction entry is foreign to the account that is viewing it.
	/// </summary>
	/// <param name="amount">An amount to possibly negate.</param>
	/// <returns>The amount, possibly negated.</returns>
	/// <remarks>
	/// Given these 3 transaction entries:
	/// [Checking] -500
	/// [House]     400
	/// Interest    100
	/// When viewed from the Checking account, they should appear like this:
	/// "Mortgage"  -500
	/// - [House]   -400
	/// - Interest  -100
	/// And when viewed from the House account, they should appear like this:
	/// "Mortgage"   400
	/// - [Checking] 500
	/// - Interest  -100
	/// Notice how the 500 amount is negative when viewed from its own account, and positive when viewed from another.
	/// Notice how the 400 amount is positive when viewed from its own account, and negative when viewed from another.
	/// Interest is negative from both ledgers, but would be positive in a report.
	/// From this we deduce that a TransactionEntry should flip its Amount between model and view model
	/// iff <see cref="Account"/> (the account it applies to) is set to any account other than <see cref="ThisAccount"/>.
	/// </remarks>
	private decimal NegateAmountIfAppropriate(decimal amount) => this.Account == this.ThisAccount ? amount : -amount;
}
