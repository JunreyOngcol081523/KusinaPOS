using SQLite;

using NotNullAttribute = SQLite.NotNullAttribute;

namespace KusinaPOS.Models
{
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull, Unique]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
