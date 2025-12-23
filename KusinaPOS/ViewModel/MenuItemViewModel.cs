using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.Models;
using System.Collections.ObjectModel;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.ViewModel
{
    public partial class MenuItemViewModel : ObservableObject
    {
        private readonly CategoryService _categoryService;
        private readonly MenuItemService _menuItemService;

        public MenuItemViewModel(CategoryService categoryService, MenuItemService menuItemService)
        {
            _categoryService = categoryService;
            _menuItemService = menuItemService;

            LoggedInUserId = Preferences.Get("LoggedInUserId", string.Empty);
            LoggedInUserName = Preferences.Get("LoggedInUserName", string.Empty);

            _ = InitializeAsync();
        }

        [ObservableProperty]
        private List<string> categories = [];

        [ObservableProperty]
        private string selectedCategory = string.Empty;

        [ObservableProperty]
        private string loggedInUserName = string.Empty;

        [ObservableProperty]
        private string loggedInUserId = string.Empty;

        [ObservableProperty]
        private string newCategoryName = string.Empty;



        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> categoriesWithMenuItems = [];

        private async Task InitializeAsync()
        {
            //await CreateSampleDataAsync();
            await LoadCategoriesWithMenuItems();
        }

        [RelayCommand]
        public async Task CreateSampleDataAsync()
        {
            try
            {
                // Check if we already have data
                var existingCategories = await _categoryService.GetAllCategoriesAsync();
                if (existingCategories.Count > 0)
                    return;

                // Create sample categories
                await _categoryService.AddCategoryAsync("Appetizers");
                await _categoryService.AddCategoryAsync("Main Courses");
                await _categoryService.AddCategoryAsync("Desserts");

                // Create sample menu items
                var sampleMenuItems = new List<MenuItem>
                {
                    // Appetizers
                    new MenuItem
                    {
                        Name = "Grilled Salmon",
                        Description = "Fresh Atlantic salmon grilled to perfection",
                        Category = "Appetizers",
                        Price = 299.99m,
                        Type = "simple",
                        IsActive = true
                    },
                    new MenuItem
                    {
                        Name = "Ribeye Steak",
                        Description = "Premium 12oz ribeye steak",
                        Category = "Appetizers",
                        Price = 599.99m,
                        Type = "simple",
                        IsActive = true
                    },
                    // Main Courses
                    new MenuItem
                    {
                        Name = "Caesar Salad",
                        Description = "Classic Caesar salad with fresh romaine lettuce",
                        Category = "Main Courses",
                        Price = 149.99m,
                        Type = "simple",
                        IsActive = true
                    },
                    new MenuItem
                    {
                        Name = "Margherita Pizza",
                        Description = "Traditional Italian pizza with fresh mozzarella",
                        Category = "Main Courses",
                        Price = 399.99m,
                        Type = "simple",
                        IsActive = true
                    },
                    // Desserts
                    new MenuItem
                    {
                        Name = "Tiramisu",
                        Description = "Classic Italian dessert with espresso and mascarpone",
                        Category = "Desserts",
                        Price = 199.99m,
                        Type = "simple",
                        IsActive = true
                    },
                    new MenuItem
                    {
                        Name = "Chocolate Lava Cake",
                        Description = "Warm chocolate cake with molten center",
                        Category = "Desserts",
                        Price = 249.99m,
                        Type = "simple",
                        IsActive = false // One inactive item for testing
                    }
                };

                foreach (var item in sampleMenuItems)
                {
                    await _menuItemService.AddMenuItemAsync(item);
                }
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to create sample data: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task LoadCategories()
        {
            var categoryList = await _categoryService.GetActiveCategoriesAsync();
            Categories = [];
            foreach (var category in categoryList)
            {
                Categories.Add(category.Name);
            }
        }

        [RelayCommand]
        public async Task LoadCategoriesWithMenuItems()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                var allMenuItems = await _menuItemService.GetAllMenuItemsAsync();

                var newCollection = new ObservableCollection<CategoryViewModel>();

                foreach (var category in categories)
                {
                    var categoryVM = new CategoryViewModel(category);

                    var categoryMenuItems = allMenuItems
                        .Where(m => m.Category == category.Name)
                        .ToList();

                    categoryVM.MenuItems = new ObservableCollection<MenuItem>(categoryMenuItems);
                    newCollection.Add(categoryVM);
                }

                // Replace the entire collection instead of clearing
                CategoriesWithMenuItems = newCollection;
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to load categories: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task AddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                await PageHelper.DisplayAlertAsync("Validation", "Please enter a category name", "OK");
                return;
            }

            try
            {
                await _categoryService.AddCategoryAsync(NewCategoryName.Trim());
                await LoadCategories();
                await LoadCategoriesWithMenuItems();

                NewCategoryName = string.Empty;
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to add category: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task EditMenuItem(MenuItem menuItem)
        {
            var parameters = new Dictionary<string, object>
            {
                { "MenuItem", menuItem }
            };
            await Shell.Current.GoToAsync("editmenuitem", parameters);
        }

        [RelayCommand]
        public async Task DeleteMenuItem(MenuItem menuItem)
        {
            bool confirm = await PageHelper.DisplayConfirmAsync(
                "Confirm Delete",
                $"Are you sure you want to delete '{menuItem.Name}'?",
                "Yes",
                "No");

            if (confirm)
            {
                try
                {
                    await _menuItemService.DeleteMenuItemAsync(menuItem.Id);
                    await LoadCategoriesWithMenuItems();
                }
                catch (Exception ex)
                {
                    await PageHelper.DisplayAlertAsync("Error", $"Failed to delete item: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        public async Task ToggleMenuItemStatus(Models.MenuItem menuItem)
        {
            try
            {
                menuItem.IsActive = !menuItem.IsActive;
                await _menuItemService.UpdateMenuItemAsync(menuItem);
                await LoadCategoriesWithMenuItems();
            }
            catch (Exception ex)
            {
                menuItem.IsActive = !menuItem.IsActive;
                await PageHelper.DisplayAlertAsync("Error", $"Failed to update status: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task EditCategory(CategoryViewModel category)
        {
            string? result = await Application.Current?.MainPage?.DisplayPromptAsync(
                "Edit Category",
                "Enter new category name:",
                initialValue: category.Name,
                maxLength: 50,
                keyboard: Keyboard.Text);

            if (!string.IsNullOrWhiteSpace(result) && result != category.Name)
            {
                try
                {
                    var oldName = category.Name;
                    var categoryModel = category.ToModel();
                    categoryModel.Name = result.Trim();
                    await _categoryService.UpdateCategoryAsync(categoryModel);

                    var menuItemsToUpdate = await _menuItemService.GetMenuItemsByCategoryAsync(oldName);
                    foreach (var menuItem in menuItemsToUpdate)
                    {
                        menuItem.Category = result.Trim();
                        await _menuItemService.UpdateMenuItemAsync(menuItem);
                    }

                    await LoadCategories();
                    await LoadCategoriesWithMenuItems();
                }
                catch (Exception ex)
                {
                    await PageHelper.DisplayAlertAsync("Error", $"Failed to update category: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        public async Task DeleteCategory(CategoryViewModel category)
        {
            if (category.MenuItems.Count > 0)
            {
                await PageHelper.DisplayAlertAsync(
                    "Cannot Delete",
                    "Please remove all menu items from this category before deleting it.",
                    "OK");
                return;
            }

            bool confirm = await PageHelper.DisplayConfirmAsync(
                "Confirm Delete",
                $"Are you sure you want to delete the category '{category.Name}'?",
                "Yes",
                "No");

            if (confirm)
            {
                try
                {
                    await _categoryService.DeactivateCategoryAsync(category.Id);
                    await LoadCategories();
                    await LoadCategoriesWithMenuItems();
                }
                catch (Exception ex)
                {
                    await PageHelper.DisplayAlertAsync("Error", $"Failed to delete category: {ex.Message}", "OK");
                }
            }
        }
    }
}