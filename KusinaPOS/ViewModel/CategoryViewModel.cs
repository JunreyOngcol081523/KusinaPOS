using CommunityToolkit.Mvvm.ComponentModel;
using KusinaPOS.Models;
using System.Collections.ObjectModel;
using System.Linq;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.ViewModel
{
    public partial class CategoryViewModel : ObservableObject
    {
        public CategoryViewModel(Category model)
        {
            Id = model.Id;
            Name = model.Name;
            MenuItems = new ObservableCollection<MenuItem>();
            FilteredMenuItems = new ObservableCollection<MenuItem>();
        }

        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private ObservableCollection<Models.MenuItem> menuItems;

        [ObservableProperty]
        private ObservableCollection<MenuItem> filteredMenuItems;

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private string searchText;

        partial void OnSearchTextChanged(string value)
        {
            FilterMenuItems();
        }

        private void FilterMenuItems()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);
            }
            else
            {
                var filtered = MenuItems
                    .Where(mi => mi.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase));
                FilteredMenuItems = new ObservableCollection<MenuItem>(filtered);
            }
        }
        public Category ToModel() => new Category
        {
            Id = this.Id,
            Name = this.Name
        };
    }
}
