namespace Nerdbank.MoneyManagement
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using SQLite;

    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTimeOffset When { get; set; }

        public decimal Amount { get; set; }
    }
}
