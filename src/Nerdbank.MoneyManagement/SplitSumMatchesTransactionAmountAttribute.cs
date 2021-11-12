// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Nerdbank.MoneyManagement.ViewModels;

namespace Nerdbank.MoneyManagement;

/// <summary>
/// Verifies that a transaction's amount matches the sum of its splits.
/// </summary>
internal class SplitSumMatchesTransactionAmountAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		var splits = (IReadOnlyCollection<SplitTransactionViewModel>)(value ?? throw new ArgumentNullException(nameof(value)));
		if (splits.Count == 0)
		{
			return ValidationResult.Success;
		}

		var transaction = (TransactionViewModel)(validationContext.ObjectInstance ?? throw new ArgumentException("Transaction state required.", nameof(validationContext)));
		decimal splitSum = splits.Sum(split => split.Amount);
		return splitSum == transaction.Amount
			? ValidationResult.Success
			: new ValidationResult($"Splits sum is {splitSum:C} but transaction amount is {transaction.Amount:C}.");
	}
}
