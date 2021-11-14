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
public class SplitTransactionViewModel : EntityViewModel<Transaction>
{
	private decimal amount;
	private string? memo;
	private ITransactionTarget? categoryOrTransfer;

	public SplitTransactionViewModel(TransactionViewModel parent, Transaction? splitTransaction)
		: base(parent.MoneyFile)
	{
		this.ParentTransaction = parent;
		this.AutoSave = true;

		if (splitTransaction is object)
		{
			this.CopyFrom(splitTransaction);
		}
	}

	/// <summary>
	/// Gets the account this transaction was created to be displayed within.
	/// </summary>
	public AccountViewModel ThisAccount => this.ParentTransaction.ThisAccount;

	public TransactionViewModel ParentTransaction { get; }

	public decimal Amount
	{
		get => this.amount;
		set => this.SetProperty(ref this.amount, value);
	}

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

		split.ParentTransactionId = this.ParentTransaction.Id ?? throw new InvalidOperationException("Cannot save a split before its parent transaction.");
		split.Amount = Math.Abs(this.Amount);
		split.Memo = this.Memo;
		split.CategoryId = (this.CategoryOrTransfer as CategoryViewModel)?.Id;

		if (this.Amount < 0)
		{
			split.DebitAccountId = this.ThisAccount.Id;
			split.CreditAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
		}
		else
		{
			split.CreditAccountId = this.ThisAccount.Id;
			split.DebitAccountId = (this.CategoryOrTransfer as AccountViewModel)?.Id;
		}
	}

	protected override void CopyFromCore(Transaction split)
	{
		Requires.NotNull(split, nameof(split));

		this.Amount = split.CreditAccountId == this.ThisAccount.Id ? split.Amount : -split.Amount;
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
