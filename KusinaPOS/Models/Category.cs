using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

using NotNullAttribute = SQLite.NotNullAttribute;

namespace KusinaPOS.Models
{
    public partial class Category : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull, Unique]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        [ObservableProperty]
        private bool isSelected;
        [ObservableProperty]
        private int numberOfMenuUnderThisCategory;
    }
}
