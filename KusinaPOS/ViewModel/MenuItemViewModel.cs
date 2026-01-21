using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using SQLite;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.ViewModel
{
    public partial class MenuItemViewModel : ObservableObject
    {
        #region Services
        private readonly CategoryService _categoryService;
        private readonly MenuItemService _menuItemService;
        private readonly IDateTimeService _dateTimeService;
        private readonly InventoryItemService _inventoryItemService;
        private readonly MenuItemIngredientService _menuItemIngredientService;
        private readonly InventoryTransactionService _inventoryTransactionService;
        private readonly SQLiteAsyncConnection _db;
        #endregion

        #region Constructor
        public MenuItemViewModel(CategoryService categoryService,
            MenuItemService menuItemService, IDateTimeService dateTimeService,
            InventoryItemService inventoryItemService,
            MenuItemIngredientService menuItemIngredientService,
            InventoryTransactionService inventoryTransactionService, IDatabaseService databaseService)
        {
            _categoryService = categoryService;
            _menuItemService = menuItemService;
            _dateTimeService = dateTimeService;
            _inventoryItemService = inventoryItemService;
            _menuItemIngredientService = menuItemIngredientService;
            _inventoryTransactionService = inventoryTransactionService;
            _db = databaseService.GetConnection();
            try
            {
                LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
                LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
                StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

                _dateTimeService.DateTimeChanged += OnDateTimeChanged;
                CurrentDateTime = _dateTimeService.CurrentDateTime;

                _ = SafeInitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MenuItemViewModel constructor: {ex.Message}");
            }


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
        [ObservableProperty]
        private List<string> unitMeasurements = new();
        [ObservableProperty]
        private string selectedUnit = string.Empty;
        [ObservableProperty]
        private decimal initialStock;
        [ObservableProperty]
        private decimal costPerUnit;
        [ObservableProperty]
        private decimal reOrderLevel;
        // UI & Image
        [ObservableProperty]
        private bool isBorderVisible = false;
        [ObservableProperty]
        private bool isUnitBasedVisible = false;

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
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading menu image: {ex.Message}");
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
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CurrentDateTime = dateTime;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDateTimeChanged: {ex.Message}");
            }
        }
        #endregion

        #region Initialization
        private async Task InitializeAsync()
        {
            await LoadCategoriesWithMenuItems();
            IsBorderVisible = false;
            MenuTypes = new ObservableCollection<string> { "Unit-Based", "Recipe-Based" };
            UnitMeasurements = UnitMeasurementService.AllUnits;
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
            try
            {
                var categoryList = await _categoryService.GetActiveCategoriesAsync();
                Categories = new List<string>();
                foreach (var category in categoryList)
                {
                    Categories.Add(category.Name);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categories: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "Failed to load categories.", "OK");
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
                Debug.WriteLine($"Error loading categories with menu items: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Failed to load categories: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task AddCategoryAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewCategoryName))
                {
                    await PageHelper.DisplayAlertAsync("Validation", "Please enter a category name", "OK");
                    return;
                }

                await _categoryService.AddCategoryAsync(NewCategoryName.Trim());
                await LoadCategories();
                await LoadCategoriesWithMenuItems();
                NewCategoryName = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding category: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Failed to add category: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task EditCategory(CategoryViewModel category)
        {
            try
            {
                string? result = await Application.Current?.MainPage?.DisplayPromptAsync(
                    "Edit Category",
                    "Enter new category name:",
                    initialValue: category.Name,
                    maxLength: 50,
                    keyboard: Keyboard.Text);

                if (!string.IsNullOrWhiteSpace(result) && result != category.Name)
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error editing category: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Failed to update category: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task DeleteCategory(CategoryViewModel category)
        {
            try
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
                    await _categoryService.DeactivateCategoryAsync(category.Id);
                    await LoadCategories();
                    await LoadCategoriesWithMenuItems();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting category: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Failed to delete category: {ex.Message}", "OK");
            }
        }
        #endregion

        #region CRUD: Menu Items
        [RelayCommand]
        public async Task AddMenuItem()
        {
            try
            {
                SelectedMenuItemId = 0;
                MenuItemName = MenuDescription = SelectedCategory = SelectedMenuType = ImagePath = string.Empty;
                MenuItemPrice = 0;
                IsActive = false;
                LabelText = "Add New Menu Item";
                ShowBorder();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddMenuItem: {ex.Message}");
            }
        }
        partial void OnSelectedMenuTypeChanged(string value)
        {
            try
            {
                IsUnitBasedVisible = value == "Unit-Based";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSelectedMenuTypeChanged: {ex.Message}");
            }
        }
        [RelayCommand]
        public async Task EditMenuItem(MenuItem menuItem)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EditMenuItem: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task DeleteMenuItemAsync(MenuItem menuItem)
        {
            try
            {
                bool confirm = await PageHelper.DisplayConfirmAsync(
                    "Confirm Delete",
                    $"Are you sure you want to delete '{menuItem.Name}'?",
                    "Yes", "No");

                if (!confirm) return;

                await _menuItemService.DeleteMenuItemAsync(menuItem.Id);
                await LoadCategoriesWithMenuItems();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting menu item: {ex.Message}");
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling menu item status: {ex.Message}");
                menuItem.IsActive = !menuItem.IsActive;
                await PageHelper.DisplayAlertAsync("Error", "Failed to update status", "OK");
            }
        }

        [RelayCommand]
        public async Task SaveMenuItemAsync()
        {
            try
            {
                // ---------- BASIC VALIDATION ----------
                if (string.IsNullOrWhiteSpace(MenuItemName) ||
                    string.IsNullOrWhiteSpace(SelectedCategory) ||
                    string.IsNullOrWhiteSpace(SelectedMenuType) ||
                    MenuItemPrice <= 0)
                {
                    await PageHelper.DisplayAlertAsync(
                        "Validation",
                        "Please fill in all required fields with valid data.",
                        "OK");
                    return;
                }

                // ---------- UNIT-BASED VALIDATION ----------
                if (SelectedMenuType == "Unit-Based")
                {
                    if (string.IsNullOrWhiteSpace(SelectedUnit) ||
                        InitialStock < 0 ||
                        CostPerUnit <= 0 ||
                        ReOrderLevel < 0)
                    {
                        await PageHelper.DisplayAlertAsync(
                            "Validation",
                            "Please complete all inventory-related fields for unit-based items.",
                            "OK");
                        return;
                    }
                }

                bool confirm = await PageHelper.DisplayConfirmAsync(
                    "Confirm Save",
                    SelectedMenuItemId == 0 ? "Add this new menu item?" : "Update this menu item?",
                    "Yes", "No");

                if (!confirm) return;

                // ---------- TRANSACTION (REAL) ----------
                await _db.RunInTransactionAsync(tran =>
                {
                    var menuItem = new MenuItem
                    {
                        Id = SelectedMenuItemId,
                        Name = MenuItemName.Trim(),
                        Description = MenuDescription?.Trim(),
                        Category = SelectedCategory,
                        Price = MenuItemPrice,
                        Type = SelectedMenuType,
                        ImagePath = ImagePath,
                        IsActive = IsActive
                    };

                    if (SelectedMenuItemId == 0)
                    {
                        // 1️⃣ Insert Menu Item
                        tran.Insert(menuItem);

                        if (menuItem.Id <= 0)
                            throw new Exception("Menu item ID was not generated.");

                        // ---------- UNIT-BASED ----------
                        if (menuItem.Type == "Unit-Based")
                        {
                            // 2️⃣ Inventory Item
                            var inventoryItem = new InventoryItem
                            {
                                Name = menuItem.Name,
                                Unit = SelectedUnit,
                                QuantityOnHand = InitialStock,
                                CostPerUnit = CostPerUnit,
                                ReOrderLevel = ReOrderLevel,
                                IsActive = IsActive
                            };

                            tran.Insert(inventoryItem);

                            if (inventoryItem.Id <= 0)
                                throw new Exception("Inventory item ID was not generated.");

                            // 3️⃣ MenuItemIngredient
                            var ingredient = new MenuItemIngredient
                            {
                                MenuItemId = menuItem.Id,
                                InventoryItemId = inventoryItem.Id,
                                InventoryItemName = inventoryItem.Name,
                                UnitOfMeasurement = SelectedUnit,
                                QuantityPerMenu = 1
                            };

                            tran.Insert(ingredient);

                            // 4️⃣ Inventory Transaction (Initial Stock)
                            if (InitialStock > 0)
                            {
                                var inventoryTransaction = new InventoryTransaction
                                {
                                    InventoryItemId = inventoryItem.Id,
                                    QuantityChange = InitialStock,
                                    Reason = "Initial Stock",
                                    Remarks = "Initial stock upon menu creation",
                                    TransactionDate = DateTime.Now
                                };

                                tran.Insert(inventoryTransaction);
                            }
                        }
                    }
                    else
                    {
                        // ---------- UPDATE ----------
                        tran.Update(menuItem);
                    }
                });

                // ✅ SUCCESS
                await LoadCategoriesWithMenuItems();
                HideBorder();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving menu item: {ex}");
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to save menu item.\n\n{ex.Message}",
                    "OK");
            }
        }


        #endregion

        #region UI Helpers
        [RelayCommand]
        private void ShowBorder()
        {
            try
            {
                IsBorderVisible = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing border: {ex.Message}");
            }
        }

        [RelayCommand]
        private void HideBorder()
        {
            try
            {
                SelectedMenuItemId = 0;
                MenuItemName = MenuDescription = SelectedCategory = SelectedMenuType = ImagePath = string.Empty;
                MenuItemPrice = 0;
                IsActive = false;
                IsBorderVisible = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error hiding border: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ShowMenuTypeDialogAsync()
        {
            try
            {
                string message = "Unit-Based Menu Item: An item that is stocked and sold in the same unit. " +
                                 "Recipe-Based Menu Item: An item made from ingredients in inventory. " +
                                 "Tracks ingredient usage per serving.";

                await PageHelper.DisplayAlertAsync("Menu Item Types", message, "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing menu type dialog: {ex.Message}");
            }
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
                Debug.WriteLine($"Error uploading image: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Image upload failed: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task GoBackAsync()
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
        #endregion

        #region Seeding
        public async Task SeedMenuItemsAsync()
        {
            try
            {
                var existingItems = await _menuItemService.GetAllMenuItemsAsync();
                if (existingItems.Any())
                {
                    Debug.WriteLine("Menu items already seeded. Skipping.");
                    return;
                }

                var categories = new List<Category> {
                    //new Category { Name = "Main Courses", IsActive = true },
                    new Category { Name = "Appetizers", IsActive = true },
                    new Category { Name = "Desserts", IsActive = true },
                    new Category { Name = "Drinks", IsActive = true }
                };

                await _categoryService.AddAllCategoriesAsync(categories);
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

                await _menuItemService.AddAllMenuItemAsync(menuItems);
                Debug.WriteLine("Menu items seeded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error seeding menu items: {ex.Message}");

            }
        }
        #endregion
    }
}