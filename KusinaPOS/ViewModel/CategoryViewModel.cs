
using CommunityToolkit.Mvvm.ComponentModel;
using KusinaPOS.Models;
using System.Collections.ObjectModel;

namespace KusinaPOS.ViewModel
{
    public partial class CategoryViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private bool _isActive;

        [ObservableProperty]
        private bool _isExpanded = false;

        [ObservableProperty]
        private ObservableCollection<Models.MenuItem> _menuItems = [];

        public CategoryViewModel()
        {
        }

        public CategoryViewModel(Category category)
        {
            Id = category.Id;
            Name = category.Name;
            IsActive = category.IsActive;
            MenuItems = [];
        }

        public Category ToModel()
        {
            return new Category
            {
                Id = Id,
                Name = Name,
                IsActive = IsActive
            };
        }
    }
}