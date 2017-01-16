namespace Nerdbank.MoneyManagement
{
    using System;
    using System.Linq;
    using SQLite;
    using Validation;

    public class Account
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public Transaction Withdraw(decimal amount)
        {
            Verify.Operation(this.Id != 0, "This account has not been saved yet.");
            return new Transaction
            {
                When = DateTimeOffset.Now,
                DebitAccountId = this.Id,
                Amount = amount,
            };
        }

        public Transaction Deposit(decimal amount)
        {
            Verify.Operation(this.Id != 0, "This account has not been saved yet.");
            return new Transaction
            {
                When = DateTimeOffset.Now,
                CreditAccountId = this.Id,
                Amount = amount,
            };
        }

        public Transaction Transfer(Account receivingAccount, decimal amount)
        {
            Requires.NotNull(receivingAccount, nameof(receivingAccount));
            Requires.Range(amount >= 0, nameof(amount), "Must be a non-negative amount.");

            var transaction = Withdraw(amount);
            transaction.CreditAccountId = receivingAccount.Id;
            return transaction;
        }
    }
}
