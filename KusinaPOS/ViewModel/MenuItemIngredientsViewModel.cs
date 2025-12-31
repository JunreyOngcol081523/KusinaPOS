using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.ViewModel
{
    public partial class MenuItemIngredientsViewModel : ObservableObject
    {
        // ======================
        // SERVICES
        // ======================
        private readonly MenuItemIngredientService menuItemIngredientService;
        private readonly MenuItemService menuItemService;
        private readonly InventoryItemService inventoryItemService;
        private readonly CategoryService categoryService;

        // ======================
        // DEBOUNCE TOKENS
        // ======================
        private CancellationTokenSource? _searchDebounceCts;
        private const int SearchDebounceDelayMs = 300;
        private CancellationTokenSource? _debounceCts;

        // ======================
        // MASTER COLLECTIONS (Full Data)
        // ======================
        [ObservableProperty] private ObservableCollection<MenuItem> menuItems = [];
        [ObservableProperty] private ObservableCollection<InventoryItem> inventoryItems = [];
        [ObservableProperty] private List<Category> categories = [];

        // ======================
        // UI-FACING COLLECTIONS
        // ======================
        [ObservableProperty] private ObservableCollection<MenuItem> filteredMenuItems = [];
        [ObservableProperty] private ObservableCollection<MenuItemIngredient> menuItemIngredients = new();

        // ======================
        // SELECTION STATE / UI BINDINGS
        // ======================
        [ObservableProperty] private CategoryViewModel selectedCategoryVM;
        [ObservableProperty] private string selectedCategoryName = "All";
        [ObservableProperty] private string selectedMenuItemName;
        [ObservableProperty] private MenuItem selectedMenuItem;
        [ObservableProperty] private string searchText;
        [ObservableProperty] private string storeName;
        [ObservableProperty] private InventoryItem selectedInventoryItem;

        // ======================
        // CONSTRUCTOR
        // ======================
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

            try
            {
                StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "KusinaPOS");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading preferences: {ex.Message}");
                StoreName = "KusinaPOS";
            }

            _ = InitializeCollectionsAsync();
        }

        // ======================
        // INITIALIZATION
        // ======================
        private async Task InitializeCollectionsAsync()
        {
            try
            {
                // Load all data in parallel for better performance
                var menuItemsTask = menuItemService.GetAllMenuItemsAsync();
                var inventoryItemsTask = inventoryItemService.GetAllInventoryItemsAsync();
                var categoriesTask = categoryService.GetAllCategoriesAsync();

                await Task.WhenAll(menuItemsTask, inventoryItemsTask, categoriesTask);

                var menuItemsList = menuItemsTask.Result.ToList();

                // Load ingredients for all menu items in parallel
                var ingredientTasks = menuItemsList.Select(async menuItem => new
                {
                    MenuItem = menuItem,
                    Ingredients = await menuItemIngredientService.GetByMenuItemIdAsync(menuItem.Id)
                }).ToList();

                var results = await Task.WhenAll(ingredientTasks);

                // Populate IngredientsText for each menu item
                foreach (var result in results)
                {
                    if (result.Ingredients.Any())
                    {
                        result.MenuItem.IngredientsText = string.Join(", ",
                            result.Ingredients.Select(i => $"{i.InventoryItemName} ({i.QuantityPerMenu:F2} {i.UnitOfMeasurement})"));
                    }
                    else
                    {
                        result.MenuItem.IngredientsText = "No ingredients";
                    }
                }

                MenuItems = new ObservableCollection<MenuItem>(menuItemsList);
                InventoryItems = new ObservableCollection<InventoryItem>(inventoryItemsTask.Result);

                var categoryList = categoriesTask.Result.ToList();
                Categories = new List<Category>(new[] { new Category { Name = "All" } }.Concat(categoryList));

                FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);

                Debug.WriteLine($"Initialized {MenuItems.Count} menu items with ingredients");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing collections: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "Failed to load data. Please restart the application.", "OK");
            }
        }

        // ======================
        // REFRESH INGREDIENTS TEXT
        // ======================
        private async Task RefreshMenuItemIngredientsText(int menuItemId)
        {
            try
            {
                var menuItem = MenuItems.FirstOrDefault(m => m.Id == menuItemId);
                if (menuItem == null) return;

                var ingredients = await menuItemIngredientService.GetByMenuItemIdAsync(menuItemId);

                if (ingredients.Any())
                {
                    menuItem.IngredientsText = string.Join(",\n ",
                        ingredients.Select(i => $"{i.InventoryItemName} ({i.QuantityPerMenu:F2} {i.UnitOfMeasurement})"));
                }
                else
                {
                    menuItem.IngredientsText = "No ingredients";
                }

                OnPropertyChanged(nameof(FilteredMenuItems));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing ingredients text: {ex.Message}");
            }
        }

        // ======================
        // CATEGORY SELECTION
        // ======================
        [RelayCommand]
        private void SelectCategory(Category category)
        {
            try
            {
                if (category == null) return;

                SelectedCategoryName = category.Name;
                SelectedCategoryVM = null;
                SearchText = string.Empty;

                if (string.Equals(category.Name, "All", StringComparison.OrdinalIgnoreCase))
                {
                    FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);
                    Debug.WriteLine("Category: All");
                    Debug.WriteLine($"Items found: {FilteredMenuItems.Count}");
                    return;
                }

                var filtered = MenuItems
                    .Where(m => string.Equals(m.Category?.Trim(), category.Name?.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                FilteredMenuItems = new ObservableCollection<MenuItem>(filtered);
                Debug.WriteLine($"Category: {category.Name}");
                Debug.WriteLine($"Items found: {FilteredMenuItems.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting category: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ShowAllCategories()
        {
            try
            {
                SelectedCategoryVM = null;
                FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing all categories: {ex.Message}");
            }
        }

        // ======================
        // MENU ITEM SELECTION
        // ======================
        partial void OnSelectedMenuItemChanged(MenuItem value)
        {
            try
            {
                Debug.WriteLine($"Selected MenuItem Changed: {value?.Name ?? "NULL"}");
                SelectedMenuItemName = value?.Name;
                SelectedInventoryItem = null;
                _ = LoadIngredientsForSelectedMenuItem();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSelectedMenuItemChanged: {ex.Message}");
            }
        }

        private async Task LoadIngredientsForSelectedMenuItem()
        {
            try
            {
                if (SelectedMenuItem == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => MenuItemIngredients.Clear());
                    return;
                }

                var ingredients = await menuItemIngredientService.GetByMenuItemIdAsync(SelectedMenuItem.Id);

                Debug.WriteLine($"Loading ingredients for: {SelectedMenuItem.Name}, Found: {ingredients.Count}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MenuItemIngredients.Clear();
                    foreach (var ing in ingredients)
                    {
                        MenuItemIngredients.Add(ing);
                        Debug.WriteLine($"Loaded Ingredient: {ing.InventoryItemName} - QtyPerMenu: {ing.QuantityPerMenu}");
                    }

                    OnPropertyChanged(nameof(MenuItemIngredients));
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading ingredients: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "Failed to load ingredients for the selected menu item.", "OK");
            }
        }

        // ======================
        // SEARCH (WITH DEBOUNCE)
        // ======================
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
            try
            {
                var filtered = MenuItems
                    .Where(m =>
                        (string.Equals(SelectedCategoryName, "All", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(m.Category?.Trim(), SelectedCategoryName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        && (string.IsNullOrWhiteSpace(SearchText) || m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    )
                    .ToList();

                FilteredMenuItems = new ObservableCollection<MenuItem>(filtered);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying search text: {ex.Message}");
            }
        }

        private void DebounceSearch()
        {
            try
            {
                _searchDebounceCts?.Cancel();
                _searchDebounceCts = new CancellationTokenSource();
                var token = _searchDebounceCts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(SearchDebounceDelayMs, token);
                        if (!token.IsCancellationRequested)
                        {
                            await MainThread.InvokeOnMainThreadAsync(() => ApplySearchText());
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in debounced search: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up debounced search: {ex.Message}");
            }
        }

        private void ApplyCategoryFilterOnly()
        {
            try
            {
                if (string.Equals(SelectedCategoryName, "All", StringComparison.OrdinalIgnoreCase))
                {
                    FilteredMenuItems = new ObservableCollection<MenuItem>(MenuItems);
                    return;
                }

                FilteredMenuItems = new ObservableCollection<MenuItem>(
                    MenuItems.Where(m => string.Equals(m.Category?.Trim(), SelectedCategoryName?.Trim(), StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying category filter: {ex.Message}");
            }
        }

        // ======================
        // ADD INGREDIENT
        // ======================
        [RelayCommand]
        private async Task AddInventoryItemServingAsync()
        {
            try
            {
                if (SelectedMenuItem == null || SelectedInventoryItem == null)
                {
                    await PageHelper.DisplayAlertAsync("Selection Required", "Please select both a menu item and an inventory item before adding an ingredient.", "OK");
                    return;
                }

                if (MenuItemIngredients.Any(i => i.InventoryItemId == SelectedInventoryItem.Id))
                {
                    await PageHelper.DisplayAlertAsync("Duplicate Ingredient", "This inventory item is already added as an ingredient for the selected menu item.", "OK");
                    return;
                }

                var newIngredient = new MenuItemIngredient
                {
                    MenuItemId = SelectedMenuItem.Id,
                    InventoryItemId = SelectedInventoryItem.Id,
                    InventoryItemName = SelectedInventoryItem.Name,
                    UnitOfMeasurement = SelectedInventoryItem.Unit,
                    QuantityPerMenu = 0
                };

                int result = await menuItemIngredientService.AddAsync(newIngredient);
                if (result > 0)
                {
                    await PageHelper.DisplayAlertAsync("Success", "Ingredient added successfully.", "OK");
                    await LoadIngredientsForSelectedMenuItem();
                    await RefreshMenuItemIngredientsText(SelectedMenuItem.Id);
                }
                else
                {
                    await PageHelper.DisplayAlertAsync("Error", "Failed to add ingredient.", "OK");
                }

                SelectedInventoryItem = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding ingredient: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "An error occurred while adding the ingredient.", "OK");
            }
        }

        // ======================
        // UPDATE QTY PER MENU (DEBOUNCED)
        // ======================
        [RelayCommand]
        private async Task DebouncedQuantityChangedAsync(MenuItemIngredient ingredient)
        {
            if (ingredient == null) return;

            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await menuItemIngredientService.UpdateQuantityAsync(
                        ingredient.MenuItemId,
                        ingredient.InventoryItemId,
                        ingredient.QuantityPerMenu
                    );

                    // Refresh the ingredients text in the list
                    await RefreshMenuItemIngredientsText(ingredient.MenuItemId);
                    await PageHelper.DisplayAlertAsync("Success", "Ingredient quantity updated.", "OK");
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating quantity: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "Failed to update ingredient quantity.", "OK");
            }
        }

        // ======================
        // REMOVE INGREDIENT
        // ======================
        [RelayCommand]
        private async Task RemoveIngredientAsync(MenuItemIngredient ingredient)
        {
            try
            {
                if (ingredient == null) return;

                bool confirm = await PageHelper.DisplayConfirmAsync(
                    "Remove Ingredient",
                    $"Are you sure you want to remove {ingredient.InventoryItemName}?",
                    "Yes",
                    "No");

                if (!confirm) return;

                int result = await menuItemIngredientService.DeleteAsync(ingredient);
                if (result > 0)
                {
                    MenuItemIngredients.Remove(ingredient);
                    await PageHelper.DisplayAlertAsync("Success", "Ingredient removed successfully.", "OK");
                    await RefreshMenuItemIngredientsText(ingredient.MenuItemId);
                }
                else
                {
                    await PageHelper.DisplayAlertAsync("Error", "Failed to remove ingredient.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing ingredient: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "An error occurred while removing the ingredient.", "OK");
            }
        }

        // ======================
        // NAVIGATION
        // ======================
        [RelayCommand]
        private async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating back: {ex.Message}");
            }
        }
    }
}