namespace Nerdbank.MoneyManagement
{
    using SQLite;

    public class Account
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
