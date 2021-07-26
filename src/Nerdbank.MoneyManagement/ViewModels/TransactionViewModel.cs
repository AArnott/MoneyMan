// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
    using System;
    using PCLCommandBase;
    using Validation;

    public class TransactionViewModel : BindableBase
    {
        private DateTime when;
        private decimal amount;
        private AccountViewModel? transferAccount;
        private PayeeViewModel? payee;

        public DateTime When
        {
            get => this.when;
            set => this.SetProperty(ref this.when, value);
        }

        public decimal Amount
        {
            get => this.amount;
            set => this.SetProperty(ref this.amount, value);
        }

        public AccountViewModel? Transfer
        {
            get => this.transferAccount;
            set => this.SetProperty(ref this.transferAccount, value);
        }

        public PayeeViewModel? Payee
        {
            get => this.payee;
            set => this.SetProperty(ref this.payee, value);
        }

        public void ApplyTo(Transaction transaction)
        {
            Requires.NotNull(transaction, nameof(transaction));

            transaction.When = this.When;
            transaction.Amount = this.Amount;
        }

        public void CopyFrom(Transaction transaction)
        {
            Requires.NotNull(transaction, nameof(transaction));

            this.When = transaction.When;
            this.Amount = transaction.Amount;
        }
    }
}
