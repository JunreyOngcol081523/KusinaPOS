using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace KusinaPOS.Models
{
    public class AllMenuItemByCategory : ObservableObject
    {
        [PrimaryKey] // MenuItemName can be unique in view
        public string MenuItemName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int QuantitySold { get; set; }
    }
}
