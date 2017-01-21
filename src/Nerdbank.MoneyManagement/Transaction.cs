namespace Nerdbank.MoneyManagement
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using SQLite;
    using Validation;

    /// <summary>
    /// Describes a deposit, withdrawal, or transfer regarding one or two accounts.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Gets or sets the primary key of this database entity.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// The date the transaction is to be sorted by.
        /// </summary>
        /// <remarks>
        /// The time component and timezone components are to be ignored.
        /// We don't want a change in the user's timezone to change the date that is displayed for a transaction.
        /// </remarks>
        [NotNull]
        public DateTime When { get; set; }

        /// <summary>
        /// Gets the amount of the transaction. Always non-negative.
        /// </summary>
        [NotNull]
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Payee.Id"/> of the <see cref="Payee"/> receiving or funding this transaction.
        /// </summary>
        public int? PayeeId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Account.Id"/> of the account to be credited the <see cref="Amount"/> of this <see cref="Transaction"/>.
        /// </summary>
        public int? CreditAccountId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Account.Id"/> of the account to be debited the <see cref="Amount"/> of this <see cref="Transaction"/>.
        /// </summary>
        public int? DebitAccountId { get; set; }

        private void Validate()
        {
            Assumes.True(this.Amount >= 0, "Amount must be non-negative.");
        }
    }
}
