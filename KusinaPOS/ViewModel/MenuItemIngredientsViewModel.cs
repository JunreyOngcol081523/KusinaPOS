
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Models;
using KusinaPOS.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.ViewModel
{
    public partial class MenuItemIngredientsViewModel : ObservableObject
    {
        private readonly MenuItemIngredientService menuItemIngredientService;
        private readonly MenuItemService menuItemService;
        private readonly InventoryItemService inventoryItemService;
        private readonly CategoryService categoryService;

        public MenuItemIngredientsViewModel(
            MenuItemIngredientService menuItemIngredientService,
            MenuItemService menuItemService,
            InventoryItemService inventoryItemService,
            CategoryService categoryService)
        {
            this.menuItemIngredientService = menuItemIngredientService;
            this.menuItemService = menuItemService;
            this.inventoryItemService = inventoryItemService;
            this.categoryService = categoryService;

            _ = InitializeCollectionsAsync();
        }

        // ======================
        // MASTER COLLECTIONS
        // ======================

        [ObservableProperty]
        private ObservableCollection<MenuItem> menuItems = [];

        [ObservableProperty]
        private List<InventoryItem> inventoryItems = [];

        [ObservableProperty]
        private List<Category> categories = [];

        // ======================
        // UI-FACING COLLECTION
        // ======================

        [ObservableProperty]
        private ObservableCollection<MenuItem> filteredMenuItems = [];

        // ======================
        // SELECTION STATE
        // ======================

        [ObservableProperty]
        private CategoryViewModel selectedCategoryVM;

        // ======================
        // INITIALIZATION
        // ======================

        private async Task InitializeCollectionsAsync()
        {
            MenuItems = new ObservableCollection<MenuItem>(await menuItemService.GetAllMenuItemsAsync());
            InventoryItems = await inventoryItemService.GetAllInventoryItemsAsync();
            Categories = await categoryService.GetAllCategoriesAsync();

            // Default view = show all menu items
            // optional: show all at start
            FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);
        }

        // ======================
        // COMMANDS
        // ======================

        [RelayCommand]
        private void SelectCategory(Category category)
        {
            if (category == null) return;

            // Optional: reset selected category
            SelectedCategoryVM = null;

            // Filter menu items by matching Category string
            var filtered = MenuItems
                .Where(m => string.Equals(m.Category?.Trim(), category.Name?.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredMenuItems = new ObservableCollection<MenuItem>(filtered);

            Debug.WriteLine($"Category: {category.Name}");
            Debug.WriteLine($"Items found: {FilteredMenuItems.Count}");
        }



        [RelayCommand]
        private void ShowAllCategories()
        {
            SelectedCategoryVM = null;
            FilteredMenuItems = MenuItems;
        }
    }
}
