// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

/// <summary>
/// Represents a <see cref="Transaction"/> that is a member of a split transaction,
/// as it is represented in its "home" account, such that it only appears nested under the parent transaction.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class SplitTransactionViewModel : TransactionViewModel
{
	private decimal amount;
	private string? memo;
	private ITransactionTarget? categoryOrTransfer;

	public SplitTransactionViewModel(BankingTransactionViewModel parent, Transaction? splitTransaction)
		: base(parent.ThisAccount)
	{
		this.RegisterDependentProperty(nameof(this.Amount), nameof(this.AmountFormatted));

		this.ParentTransaction = parent;
		this.AutoSave = true;

		if (splitTransaction is object)
		{
			this.CopyFrom(splitTransaction);
		}
	}

	/// <inheritdoc cref="TransactionViewModel.ThisAccount"/>
	public new BankingAccountViewModel ThisAccount => (BankingAccountViewModel)base.ThisAccount;

	public BankingTransactionViewModel ParentTransaction { get; }

	public decimal Amount
	{
		get => this.amount;
		set => this.SetProperty(ref this.amount, value);
	}

	public string? AmountFormatted => this.ThisAccount?.CurrencyAsset?.Format(this.Amount);

	public string? Memo
	{
		get => this.memo;
		set => this.SetProperty(ref this.memo, value);
	}

	public ITransactionTarget? CategoryOrTransfer
	{
		get => this.categoryOrTransfer;
		set => this.SetProperty(ref this.categoryOrTransfer, value);
	}

	public IEnumerable<ITransactionTarget> AvailableTransactionTargets => this.ParentTransaction.AvailableTransactionTargets;

	private string DebuggerDisplay => $"Split ({this.Id}): {this.Memo} {this.CategoryOrTransfer?.Name} {this.Amount}";

	protected override void ApplyToCore(Transaction split)
	{
		Requires.NotNull(split, nameof(split));

		if (!this.ParentTransaction.IsPersisted)
		{
			throw new InvalidOperationException("Cannot save a split before its parent transaction.");
		}

		split.ParentTransactionId = this.ParentTransaction.Id;
		split.Memo = this.Memo;
		split.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;

		if (this.Amount < 0)
		{
			split.DebitAccountId = this.ThisAccount.Id;
			split.DebitAmount = -this.Amount;
			split.DebitAssetId = this.ThisAccount.CurrencyAsset?.Id;
			if (this.CategoryOrTransfer is AccountViewModel xfer)
			{
				split.CreditAccountId = xfer.Id;
				split.CreditAmount = -this.Amount;
				split.CreditAssetId = this.ThisAccount.CurrencyAsset?.Id;
			}
			else
			{
				split.CreditAccountId = null;
				split.CreditAmount = null;
				split.CreditAssetId = null;
			}
		}
		else
		{
			split.CreditAccountId = this.ThisAccount.Id;
			split.CreditAmount = this.Amount;
			split.CreditAssetId = this.ThisAccount.CurrencyAsset?.Id;
			if (this.CategoryOrTransfer is AccountViewModel xfer)
			{
				split.DebitAccountId = xfer.Id;
				split.DebitAmount = this.Amount;
				split.DebitAssetId = this.ThisAccount.CurrencyAsset?.Id;
			}
			else
			{
				split.DebitAccountId = null;
				split.DebitAmount = null;
				split.DebitAssetId = null;
			}
		}
	}

	protected override void CopyFromCore(Transaction split)
	{
		Requires.NotNull(split, nameof(split));

		this.Amount =
			split.CreditAccountId == this.ThisAccount.Id ? (split.CreditAmount ?? 0) :
			split.DebitAccountId == this.ThisAccount.Id ? (-split.DebitAmount ?? 0) :
			0;
		this.Memo = split.Memo;

		if (split.CategoryId is int categoryId)
		{
			this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetCategory(categoryId) ?? throw new InvalidOperationException();
		}
		else if (split.CreditAccountId is int creditId && this.ThisAccount.Id != creditId)
		{
			this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(creditId) ?? throw new InvalidOperationException();
		}
		else if (split.DebitAccountId is int debitId && this.ThisAccount.Id != debitId)
		{
			this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(debitId) ?? throw new InvalidOperationException();
		}
		else
		{
			this.CategoryOrTransfer = null;
		}
	}
}
