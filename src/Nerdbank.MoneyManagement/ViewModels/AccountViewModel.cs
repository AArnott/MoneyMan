﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using PCLCommandBase;
	using Validation;

	public class AccountViewModel : EntityViewModel<Account>
	{
		private string? name;
		private bool isClosed;

		public string? Name
		{
			get => this.name;
			set => this.SetProperty(ref this.name, value);
		}

		public bool IsClosed
		{
			get => this.isClosed;
			set => this.SetProperty(ref this.isClosed, value);
		}

		public override void ApplyTo(Account account)
		{
			Requires.NotNull(account, nameof(account));

			account.Name = this.name;
			account.IsClosed = this.IsClosed;
		}

		public override void CopyFrom(Account account)
		{
			Requires.NotNull(account, nameof(account));

			this.Name = account.Name;
			this.IsClosed = account.IsClosed;
		}
	}
}
