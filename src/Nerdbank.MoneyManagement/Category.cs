namespace Nerdbank.MoneyManagement
{
    using SQLite;

    /// <summary>
    /// A category that is assignable to a transaction.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Gets or sets the primary key for this entity.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of this category.
        /// </summary>
        [NotNull]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the optional parent category for this category.
        /// </summary>
        public int ParentCategoryId { get; set; }
    }
}
