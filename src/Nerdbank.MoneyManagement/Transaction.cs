namespace Nerdbank.MoneyManagement
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using SQLite;
    using Validation;

    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTimeOffset When { get; set; }

        /// <summary>
        /// Gets the amount of the transaction. Always non-negative.
        /// </summary>
        public decimal Amount { get; set; }

        public int? CreditAccountId { get; set; }

        public int? DebitAccountId { get; set; }

        private void Validate()
        {
            Assumes.True(this.Amount >= 0, "Amount must be non-negative.");
        }
    }
}
