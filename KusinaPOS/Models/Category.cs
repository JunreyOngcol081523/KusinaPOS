using SQLite;

using NotNullAttribute = SQLite.NotNullAttribute;

namespace KusinaPOS.Models
{
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string CategoryName { get; set; } = string.Empty;
        
    }
}
