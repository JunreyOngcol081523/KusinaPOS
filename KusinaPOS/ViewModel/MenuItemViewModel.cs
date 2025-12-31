using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.Models;
using System.Collections.ObjectModel;
using MenuItem = KusinaPOS.Models.MenuItem;
using System.Diagnostics;

namespace KusinaPOS.ViewModel
{
    public partial class MenuItemViewModel : ObservableObject
    {
        #region Services
        private readonly CategoryService _categoryService;
        private readonly MenuItemService _menuItemService;
        private readonly IDateTimeService _dateTimeService;
        #endregion

        #region Constructor
        public MenuItemViewModel(CategoryService categoryService, MenuItemService menuItemService, IDateTimeService dateTimeService)
        {
            _categoryService = categoryService;
            _menuItemService = menuItemService;
            _dateTimeService = dateTimeService;

            LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

            _dateTimeService.DateTimeChanged += OnDateTimeChanged;
            CurrentDateTime = _dateTimeService.CurrentDateTime;

            _ = SafeInitializeAsync();
             //_=SeedMenuItemsAsync();
        }
        #endregion

        #region Properties

        // Date & Time
        [ObservableProperty]
        private string _currentDateTime;

        // User info
        [ObservableProperty]
        private string loggedInUserName = string.Empty;
        [ObservableProperty]
        private string loggedInUserId = string.Empty;

        // Store info
        [ObservableProperty]
        private string storeName;

        // Categories
        [ObservableProperty]
        private List<string> categories = new();
        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> categoriesWithMenuItems = new();

        [ObservableProperty]
        private string selectedCategory = string.Empty;
        [ObservableProperty]
        private string newCategoryName = string.Empty;

        // Menu types
        [ObservableProperty]
        private ObservableCollection<string> menuTypes;
        [ObservableProperty]
        private string selectedMenuType = string.Empty;

        // Menu item fields
        [ObservableProperty]
        private int selectedMenuItemId;
        [ObservableProperty]
        private string menuItemName = string.Empty;
        [ObservableProperty]
        private string menuDescription = string.Empty;
        [ObservableProperty]
        private decimal menuItemPrice;
        [ObservableProperty]
        private bool isActive = true;

        // UI & Image
        [ObservableProperty]
        private bool isBorderVisible = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MenuImageSource))]
        [NotifyPropertyChangedFor(nameof(ImageLabel))]
        private string imagePath;

        public ImageSource MenuImageSource
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ImagePath))
                        return "placeholder_food.png";

                    if (!File.Exists(ImagePath))
                        return "placeholder_food.png";

                    return ImageSource.FromFile(ImagePath);
                }
                catch
                {
                    return "placeholder_food.png";
                }
            }
        }


        public string ImageLabel => string.IsNullOrWhiteSpace(ImagePath) ? "Click to upload" : Path.GetFileName(ImagePath);

        [ObservableProperty]
        private string labelText = string.Empty;

        #endregion

        #region Event Handlers
        private void OnDateTimeChanged(object? sender, string dateTime)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentDateTime = dateTime;
            });
        }
        #endregion

        #region Initialization
        private async Task InitializeAsync()
        {
            await LoadCategoriesWithMenuItems();
            IsBorderVisible = false;
            MenuTypes = new ObservableCollection<string> { "Unit-Based", "Recipe-Based" };
        }
        private async Task SafeInitializeAsync()
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MenuItemViewModel Init Error: {ex}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                    PageHelper.DisplayAlertAsync("Init Error", ex.Message, "OK"));
            }
        }

        #endregion

        #region CRUD: Categories
        [RelayCommand]
        public async Task LoadCategories()
        {
            var categoryList = await _categoryService.GetActiveCategoriesAsync();
            Categories = new List<string>();
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
                    categoryVM.FilteredMenuItems = new ObservableCollection<MenuItem>(categoryMenuItems);

                    newCollection.Add(categoryVM);
                }

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
        #endregion

        #region CRUD: Menu Items
        [RelayCommand]
        public async Task AddMenuItem()
        {
            SelectedMenuItemId = 0;
            MenuItemName = MenuDescription = SelectedCategory = SelectedMenuType = ImagePath = string.Empty;
            MenuItemPrice = 0;
            IsActive = false;
            LabelText = "Add New Menu Item";
            ShowBorder();
        }

        [RelayCommand]
        public async Task EditMenuItem(MenuItem menuItem)
        {
            SelectedMenuItemId = menuItem.Id;
            MenuItemName = menuItem.Name;
            MenuDescription = menuItem.Description;
            MenuItemPrice = menuItem.Price;
            IsActive = menuItem.IsActive;
            SelectedCategory = menuItem.Category;
            SelectedMenuType = menuItem.Type;
            ImagePath = string.IsNullOrWhiteSpace(menuItem.ImagePath) ? string.Empty : menuItem.ImagePath;
            LabelText = $"Edit Menu Item: {MenuItemName}";
            ShowBorder();
        }

        [RelayCommand]
        public async Task DeleteMenuItemAsync(MenuItem menuItem)
        {
            bool confirm = await PageHelper.DisplayConfirmAsync(
                "Confirm Delete",
                $"Are you sure you want to delete '{menuItem.Name}'?",
                "Yes", "No");

            if (!confirm) return;

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

        [RelayCommand]
        public async Task ToggleMenuItemStatus(MenuItem menuItem)
        {
            try
            {
                menuItem.IsActive = !menuItem.IsActive;
                await _menuItemService.UpdateMenuItemAsync(menuItem);
                await LoadCategoriesWithMenuItems();
            }
            catch
            {
                menuItem.IsActive = !menuItem.IsActive;
                await PageHelper.DisplayAlertAsync("Error", "Failed to update status", "OK");
            }
        }

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
                SelectedMenuItemId == 0 ? "Add this new menu item?" : "Update this menu item?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                if (SelectedMenuItemId == 0)
                    await _menuItemService.AddMenuItemAsync(menuItem);
                else
                    await _menuItemService.UpdateMenuItemAsync(menuItem);

                await LoadCategoriesWithMenuItems();
                HideBorder();
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to save menu item: {ex.Message}", "OK");
            }
        }
        #endregion

        #region UI Helpers
        [RelayCommand]
        private void ShowBorder() => IsBorderVisible = true;

        [RelayCommand]
        private void HideBorder()
        {
            SelectedMenuItemId = 0;
            MenuItemName = MenuDescription = SelectedCategory = SelectedMenuType = ImagePath = string.Empty;
            MenuItemPrice = 0;
            IsActive = false;
            IsBorderVisible = false;
        }

        [RelayCommand]
        public async Task ShowMenuTypeDialogAsync()
        {
            string message = "Unit-Based Menu Item: An item that is stocked and sold in the same unit. " +
                             "Recipe-Based Menu Item: An item made from ingredients in inventory. " +
                             "Tracks ingredient usage per serving.";

            await PageHelper.DisplayAlertAsync("Menu Item Types", message, "OK");
        }

        [RelayCommand]
        public async Task UploadImageAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MenuItemName))
                {
                    await PageHelper.DisplayAlertAsync("Validation", "Please enter the menu item name before uploading an image.", "OK");
                    return;
                }

                var results = await MediaPicker.PickPhotosAsync(new MediaPickerOptions { Title = "Select Menu Item Image", SelectionLimit = 1 });
                var result = results?.FirstOrDefault();
                if (result == null) return;

                var imagesDir = Path.Combine(FileSystem.AppDataDirectory, "menu_images");
                if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);

                var fileName = $"{MenuItemName.Replace(" ", "")}{Path.GetExtension(result.FileName)}";
                var destinationPath = Path.Combine(imagesDir, fileName);

                using var sourceStream = await result.OpenReadAsync();
                using var destinationStream = File.Create(destinationPath);
                await sourceStream.CopyToAsync(destinationStream);

                if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
                    File.Delete(ImagePath);

                ImagePath = destinationPath;
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Image upload failed: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
        #endregion

        public async Task SeedMenuItemsAsync()
        {
            await InitializeAsync();
            var menuItems = new List<MenuItem>
            {
                // Main Courses
                new MenuItem { Name = "Pork Sisig", Description = "Sizzling chopped pork with onions and chili", Category = "Main Courses", Price = 180, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Chicken Adobo", Description = "Classic Filipino braised chicken in soy and vinegar", Category = "Main Courses", Price = 150, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Beef Kare-Kare", Description = "Oxtail and vegetables in peanut sauce", Category = "Main Courses", Price = 220, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Grilled Bangus", Description = "Grilled milkfish stuffed with tomatoes and onions", Category = "Main Courses", Price = 170, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Crispy Pata", Description = "Deep-fried pork knuckles until crispy", Category = "Main Courses", Price = 380, Type = "Recipe-Based", IsActive = true },
    
                // Appetizers
                new MenuItem { Name = "Lumpia Shanghai", Description = "Crispy spring rolls filled with pork and vegetables", Category = "Appetizers", Price = 120, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Calamares", Description = "Crispy fried squid rings with garlic mayo", Category = "Appetizers", Price = 140, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Chicken Wings", Description = "Buffalo-style or garlic parmesan chicken wings", Category = "Appetizers", Price = 160, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Tokwa't Baboy", Description = "Fried tofu and pork with soy-vinegar dressing", Category = "Appetizers", Price = 110, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Dynamite Lumpia", Description = "Cheese-stuffed chili peppers wrapped in spring roll", Category = "Appetizers", Price = 130, Type = "Recipe-Based", IsActive = true },
    
                // Desserts
                new MenuItem { Name = "Leche Flan", Description = "Creamy caramel custard", Category = "Desserts", Price = 80, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Halo-Halo", Description = "Mixed shaved ice with beans, fruits, and ube ice cream", Category = "Desserts", Price = 95, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Ube Cake", Description = "Purple yam cake with cream cheese frosting", Category = "Desserts", Price = 120, Type = "Unit-Based", IsActive = true },
                new MenuItem { Name = "Turon", Description = "Fried banana spring rolls with jackfruit", Category = "Desserts", Price = 70, Type = "Recipe-Based", IsActive = true },
                new MenuItem { Name = "Buko Pandan", Description = "Young coconut and pandan-flavored gelatin dessert", Category = "Desserts", Price = 65, Type = "Recipe-Based", IsActive = true },
    
                // Drinks
                new MenuItem { Name = "Iced Tea", Description = "Refreshing house-brewed iced tea", Category = "Drinks", Price = 40, Type = "Unit-Based", IsActive = true },
                new MenuItem { Name = "Calamansi Juice", Description = "Fresh Philippine lime juice", Category = "Drinks", Price = 50, Type = "Unit-Based", IsActive = true },
                new MenuItem { Name = "Mango Shake", Description = "Creamy fresh mango smoothie", Category = "Drinks", Price = 85, Type = "Unit-Based", IsActive = true },
                new MenuItem { Name = "Sago't Gulaman", Description = "Sweet tapioca pearls and gelatin drink", Category = "Drinks", Price = 45, Type = "Unit-Based", IsActive = true },
                new MenuItem { Name = "Buko Juice", Description = "Fresh young coconut water", Category = "Drinks", Price = 60, Type = "Unit-Based", IsActive = true }
            };
            var categories = new List<Category> { 
                new Category { Name = "Main Courses", IsActive = true },
                new Category { Name = "Appetizers", IsActive = true },
                new Category { Name = "Desserts", IsActive = true },
                new Category { Name = "Drinks", IsActive = true }
            };

            await _categoryService.AddAllCategoriesAsync(categories);
            await _menuItemService.AddAllMenuItemAsync(menuItems);
        }
        
    }
}
