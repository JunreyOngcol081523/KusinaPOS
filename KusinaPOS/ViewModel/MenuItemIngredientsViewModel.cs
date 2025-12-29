
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
        //=======================
        // DEBOUNCE SEARCH
        //=======================
        private CancellationTokenSource? _searchDebounceCts;
        private const int SearchDebounceDelayMs = 300; // 300ms delay for debounce


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
        [ObservableProperty] private string selectedCategoryName="All";
        [ObservableProperty] private string selectedMenuItemName;
        [ObservableProperty] private MenuItem selectedMenuItem;
        [ObservableProperty] private string searchText;
        // ======================
        // INITIALIZATION
        // ======================

        private async Task InitializeCollectionsAsync()
        {
            MenuItems = new ObservableCollection<MenuItem>(await menuItemService.GetAllMenuItemsAsync());
            InventoryItems = await inventoryItemService.GetAllInventoryItemsAsync();
            var categories = await categoryService.GetAllCategoriesAsync();
            Categories = new List<Category>(
                                new[] { new Category { Name = "All" } }.Concat(categories)
                            );

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

            SelectedCategoryName = category.Name;

            // Reset selection / search UI state
            SelectedCategoryVM = null;
            SearchText = string.Empty;

            // 🟢 HANDLE "ALL"
            if (string.Equals(category.Name, "All", StringComparison.OrdinalIgnoreCase))
            {
                FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);

                Debug.WriteLine("Category: All");
                Debug.WriteLine($"Items found: {FilteredMenuItems.Count}");
                return;
            }

            // 🔵 NORMAL CATEGORY FILTER
            var filtered = MenuItems
                .Where(m => string.Equals(
                    m.Category?.Trim(),
                    category.Name?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredMenuItems = new ObservableCollection<MenuItem>(filtered);

            Debug.WriteLine($"Category: {category.Name}");
            Debug.WriteLine($"Items found: {FilteredMenuItems.Count}");
        }

        [RelayCommand]
        private async Task SelectMenuItemAsync(MenuItem menuItem)
        {
            this.SelectedMenuItem = menuItem;
            this.SelectedMenuItemName = menuItem?.Name;
        }

        [RelayCommand]
        private void ShowAllCategories()
        {
            SelectedCategoryVM = null;
            FilteredMenuItems = MenuItems;
        }
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                ApplyCategoryFilterOnly();
                return;
            }

            DebounceSearch();
        }
        private void ApplySearchText()
        {
            var filtered = MenuItems
                .Where(m =>
                    // CATEGORY FILTER
                    (string.Equals(SelectedCategoryName, "All", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(m.Category?.Trim(),
                                   SelectedCategoryName?.Trim(),
                                   StringComparison.OrdinalIgnoreCase))

                    // AND NAME SEARCH
                    && (string.IsNullOrWhiteSpace(SearchText) ||
                        m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                )
                .ToList();
            FilteredMenuItems = new ObservableCollection<MenuItem>(filtered);
        }
        private async void DebounceSearch()
        {
            // Cancel previous debounce if user is still typing
            _searchDebounceCts?.Cancel();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            try
            {
                // Wait for the delay
                await Task.Delay(SearchDebounceDelayMs, token);

                // If cancelled, exit
                if (token.IsCancellationRequested)
                    return;

                // Apply actual search
                ApplySearchText();
            }
            catch (TaskCanceledException)
            {
                // Expected when typing continues
            }
        }
        private void ApplyCategoryFilterOnly()
        {
            if (string.Equals(SelectedCategoryName, "All", StringComparison.OrdinalIgnoreCase))
            {
                FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);
                return;
            }

            FilteredMenuItems = new ObservableCollection<MenuItem>(
                MenuItems.Where(m =>
                    string.Equals(m.Category?.Trim(),
                                  SelectedCategoryName?.Trim(),
                                  StringComparison.OrdinalIgnoreCase)));
        }

    }
}
