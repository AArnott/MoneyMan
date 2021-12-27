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

	public SplitTransactionViewModel(BankingTransactionViewModel parent, Transaction splitTransaction)
		: base(parent.ThisAccount, splitTransaction)
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

	protected override void ApplyToCore()
	{
		if (!this.ParentTransaction.IsPersisted)
		{
			throw new InvalidOperationException("Cannot save a split before its parent transaction.");
		}

		this.Model.ParentTransactionId = this.ParentTransaction.Id;
		this.Model.Memo = this.Memo;
		this.Model.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;

		if (this.Amount < 0)
		{
			this.Model.DebitAccountId = this.ThisAccount.Id;
			this.Model.DebitAmount = -this.Amount;
			this.Model.DebitAssetId = this.ThisAccount.CurrencyAsset?.Id;
			if (this.CategoryOrTransfer is AccountViewModel xfer)
			{
				this.Model.CreditAccountId = xfer.Id;
				this.Model.CreditAmount = -this.Amount;
				this.Model.CreditAssetId = this.ThisAccount.CurrencyAsset?.Id;
			}
			else
			{
				this.Model.CreditAccountId = null;
				this.Model.CreditAmount = null;
				this.Model.CreditAssetId = null;
			}
		}
		else
		{
			this.Model.CreditAccountId = this.ThisAccount.Id;
			this.Model.CreditAmount = this.Amount;
			this.Model.CreditAssetId = this.ThisAccount.CurrencyAsset?.Id;
			if (this.CategoryOrTransfer is AccountViewModel xfer)
			{
				this.Model.DebitAccountId = xfer.Id;
				this.Model.DebitAmount = this.Amount;
				this.Model.DebitAssetId = this.ThisAccount.CurrencyAsset?.Id;
			}
			else
			{
				this.Model.DebitAccountId = null;
				this.Model.DebitAmount = null;
				this.Model.DebitAssetId = null;
			}
		}
	}

	protected override void CopyFromCore()
	{
		this.Amount =
			this.Model.CreditAccountId == this.ThisAccount.Id ? (this.Model.CreditAmount ?? 0) :
			this.Model.DebitAccountId == this.ThisAccount.Id ? (-this.Model.DebitAmount ?? 0) :
			0;
		this.Memo = this.Model.Memo;

		if (this.Model.CategoryId is int categoryId)
		{
			this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetCategory(categoryId) ?? throw new InvalidOperationException();
		}
		else if (this.Model.CreditAccountId is int creditId && this.ThisAccount.Id != creditId)
		{
			this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(creditId) ?? throw new InvalidOperationException();
		}
		else if (this.Model.DebitAccountId is int debitId && this.ThisAccount.Id != debitId)
		{
			this.CategoryOrTransfer = this.ThisAccount.DocumentViewModel?.GetAccount(debitId) ?? throw new InvalidOperationException();
		}
		else
		{
			this.CategoryOrTransfer = null;
		}
	}
}
