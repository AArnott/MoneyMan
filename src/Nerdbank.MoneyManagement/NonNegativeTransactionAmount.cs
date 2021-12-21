// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Nerdbank.MoneyManagement.ViewModels;

namespace Nerdbank.MoneyManagement;

internal class NonNegativeTransactionAmount : ValidationAttribute
{
	public NonNegativeTransactionAmount()
		: base("This value must be non-negative.")
	{
	}

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		InvestingTransactionViewModel viewModel = (InvestingTransactionViewModel)validationContext.ObjectInstance;
		decimal? decimalValue = (decimal?)value;
		return viewModel.Action == TransactionAction.Transfer || decimalValue is null or >= 0
			? ValidationResult.Success
			: new ValidationResult("This value must be non-negative.");
	}
}
