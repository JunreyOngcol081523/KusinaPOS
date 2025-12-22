using SQLite;

using NotNullAttribute = SQLite.NotNullAttribute;

namespace KusinaPOS.Models
{
    public class MenuItem
    {
        [PrimaryKey, AutoIncrement] 
        public int Id { get; set; }

        [NotNull]
        public string Name { get; set; } = string.Empty;
        [NotNull]
        public string Description { get; set; } = string.Empty;
        [NotNull]
        public string Category { get; set; } = string.Empty;

        [NotNull]
        public decimal Price { get; set; }

        // "simple" or "composite"
        [NotNull]
        public string Type { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
