namespace Nerdbank.MoneyManagement
{
    using SQLite;

    public class Payee
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Name { get; set; }
    }
}
