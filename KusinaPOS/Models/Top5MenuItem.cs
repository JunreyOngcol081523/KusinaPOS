using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace KusinaPOS.Models
{
    public class Top5MenuItem : ObservableObject
    {
        [PrimaryKey] // We can use MenuItemName as primary for view
        public string MenuItemName { get; set; } = string.Empty;

        public decimal TotalSales { get; set; }
    }
}
