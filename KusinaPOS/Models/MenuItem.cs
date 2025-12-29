using CommunityToolkit.Mvvm.ComponentModel;
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

        [NotNull]
        public string Type { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        [Ignore]
        public string StatusText => IsActive ? "Active" : "Inactive";
        [Ignore]
        public string IngredientsText { get; set; }
        [Ignore]
        public List<MenuItemIngredient> Ingredients { get; set; }
    }
}