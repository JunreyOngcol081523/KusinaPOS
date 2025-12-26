using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.Models;
using System.Collections.ObjectModel;
using MenuItem = KusinaPOS.Models.MenuItem;
using KusinaPOS.Views;

namespace KusinaPOS.ViewModel
{
    public partial class MenuItemViewModel : ObservableObject
    {

        private readonly CategoryService _categoryService;
        private readonly MenuItemService _menuItemService;
        private readonly IDateTimeService _dateTimeService;
        public MenuItemViewModel(CategoryService categoryService, MenuItemService menuItemService, IDateTimeService dateTimeService)
        {
            _categoryService = categoryService;
            _menuItemService = menuItemService;
            _dateTimeService = dateTimeService;
            LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, string.Empty);
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            _dateTimeService.DateTimeChanged += OnDateTimeChanged;
            CurrentDateTime = _dateTimeService.CurrentDateTime;
            _ = InitializeAsync();
        }
        private void OnDateTimeChanged(object? sender, string dateTime)
        {
            CurrentDateTime = dateTime;
        }
        [ObservableProperty]
        private string _currentDateTime;
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
        private bool isBorderVisible = false;   

        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> categoriesWithMenuItems = [];

        [ObservableProperty]
        private string selectedMenuType = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> menuTypes;

        [ObservableProperty]
        private int selectedMenuItemId;

        [ObservableProperty]
        private string menuItemName = string.Empty;

        [ObservableProperty]
        private string menuDescription = string.Empty;

        [ObservableProperty]
        private decimal menuItemPrice;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MenuImageSource))]
        [NotifyPropertyChangedFor(nameof(ImageLabel))]
        private string imagePath;

        public ImageSource MenuImageSource =>
            string.IsNullOrWhiteSpace(ImagePath)
                ? "placeholder_food.png"
                : ImageSource.FromFile(ImagePath);
        public string ImageLabel => string.IsNullOrWhiteSpace(ImagePath) ? "Click to upload" : Path.GetFileName(ImagePath);

        [ObservableProperty]
        private string labelText = string.Empty;
        [ObservableProperty]
        private bool isActive = true;
        private async Task InitializeAsync()
        {
            //await CreateSampleDataAsync();
            await LoadCategoriesWithMenuItems();
            IsBorderVisible = false;
            MenuTypes = new ObservableCollection<string> { "Simple", "Composite" };
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
            this.SelectedMenuItemId = menuItem.Id;
            this.MenuItemName = menuItem.Name;
            this.MenuDescription = menuItem.Description;
            this.MenuItemPrice = menuItem.Price;
            this.IsActive = menuItem.IsActive;
            this.SelectedCategory = menuItem.Category;
            this.SelectedMenuType = menuItem.Type;
            this.LabelText = $"Edit Menu Item: {MenuItemName}";
            ImagePath = string.IsNullOrWhiteSpace(menuItem.ImagePath)
                ? string.Empty
                : menuItem.ImagePath;
            ShowBorder();
        }
        [RelayCommand]
        public async Task AddMenuItem()
        {
            this.LabelText = "Add New Menu Item";
            this.SelectedMenuItemId = 0;
            this.MenuItemName = string.Empty;
            this.MenuDescription = string.Empty;
            this.MenuItemPrice = 0;
            this.IsActive = false;
            this.SelectedCategory = string.Empty;
            this.SelectedMenuType = string.Empty;
            this.ImagePath = string.Empty;
            ShowBorder();
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
        [RelayCommand]
        public async Task GoBackAsync() {
            await Shell.Current.GoToAsync(nameof(DashboardPage));

        }
        [RelayCommand]
        private void ShowBorder() => IsBorderVisible = true;
        [RelayCommand]
        private void HideBorder()
        {
            this.SelectedMenuItemId = 0;
            this.MenuItemName = string.Empty;
            this.MenuDescription = string.Empty;
            this.MenuItemPrice = 0;
            this.IsActive = false;
            this.SelectedCategory = string.Empty;
            this.SelectedMenuType = string.Empty;
            this.ImagePath = string.Empty;
            IsBorderVisible = false;
        } 
        [RelayCommand]
        private async Task UploadImageAsync()
        {
            try
            {
                if (this.MenuItemName == null || this.MenuItemName.Trim() == "")
                {
                    await PageHelper.DisplayAlertAsync("Validation", "Please enter the menu item name before uploading an image.", "OK");
                    return;
                }
                // Pick photo(s) from gallery (now supports multiple selections)
                var results = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
                {
                    Title = "Select Menu Item Image",
                    SelectionLimit = 1 // Only allow one image for menu item
                });

                var result = results?.FirstOrDefault();
                if (result == null)
                    return;

                // Create folder for menu images if it doesn't exist
                var imagesDir = Path.Combine(FileSystem.AppDataDirectory, "menu_images");
                if (!Directory.Exists(imagesDir))
                    Directory.CreateDirectory(imagesDir);

                // Generate a unique file name
                var fileName = $"{this.MenuItemName.Replace(" ","")}{Path.GetExtension(result.FileName)}";
                var destinationPath = Path.Combine(imagesDir, fileName);

                // Copy the picked image to app storage
                using var sourceStream = await result.OpenReadAsync();
                using var destinationStream = File.Create(destinationPath);
                await sourceStream.CopyToAsync(destinationStream);

                // Optional: delete old image if replacing
                if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
                    File.Delete(ImagePath);

                // Update bound property (UI will refresh)
                ImagePath = destinationPath;

                // Save the path to SQLite here (if you want immediate save)
                // await _menuItemService.UpdateMenuItemImageAsync(MenuItemId, ImagePath);
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Image upload failed: {ex.Message}", "OK");
            }
        }
        //add new menu item or update existing
        [RelayCommand]
        public async Task SaveMenuItemAsync()
        {
            if (string.IsNullOrWhiteSpace(MenuItemName) ||
                string.IsNullOrWhiteSpace(SelectedCategory) ||
                string.IsNullOrWhiteSpace(SelectedMenuType) ||
                MenuItemPrice <= 0)
            {
                await PageHelper.DisplayAlertAsync("Validation", "Please fill in all required fields with valid data.", "OK");
                return;
            }
            try
            {
                var menuItem = new MenuItem
                {
                    Id = SelectedMenuItemId,
                    Name = MenuItemName.Trim(),
                    Description = MenuDescription.Trim(),
                    Category = SelectedCategory,
                    Price = MenuItemPrice,
                    Type = SelectedMenuType,
                    ImagePath = ImagePath,
                    IsActive = IsActive
                };
                bool confirm = await PageHelper.DisplayConfirmAsync(
                    "Confirm Save",
                    SelectedMenuItemId == 0 ? "Are you sure you want to add this new menu item?" : "Are you sure you want to update this menu item?",
                    "Yes",
                    "No");
                if (confirm)
                {
                    if (SelectedMenuItemId == 0)
                    {
                        // New item
                        await _menuItemService.AddMenuItemAsync(menuItem);
                    }
                    else
                    {
                        // Existing item
                        await _menuItemService.UpdateMenuItemAsync(menuItem);
                    }
                }
                else
                {
                    return;
                }
                    await LoadCategoriesWithMenuItems();
                HideBorder();
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to save menu item: {ex.Message}", "OK");
            }
        }
    }
}