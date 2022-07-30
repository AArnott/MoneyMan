// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Nerdbank.MoneyManagement.ViewModels;

namespace Nerdbank.MoneyManagement;

internal class UniqueAttribute : ValidationAttribute
{
	public UniqueAttribute()
		: base("Value must be unique amongst other records of its type.")
	{
	}

	public override bool RequiresValidationContext => true;

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		switch (validationContext.ObjectInstance)
		{
			case AccountViewModel accountViewModel when validationContext.MemberName == nameof(AccountViewModel.Name):
				if (accountViewModel.DocumentViewModel.AccountsPanel.Accounts.Any(a => a.Id != accountViewModel.Id && a.Name == (string?)value))
				{
					return new ValidationResult(this.ErrorMessage);
				}

				return ValidationResult.Success;
			default:
				throw new NotImplementedException();
		}
	}
}
