using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using SQLite;
using Syncfusion.Maui.Buttons;
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
        private readonly InventoryItemService _inventoryItemService;
        private readonly MenuItemIngredientService _menuItemIngredientService;
        private readonly InventoryTransactionService _inventoryTransactionService;
        private readonly IDateTimeService _dateTimeService;
        private readonly SQLiteAsyncConnection _db;
        #endregion

        #region Paging
        private int _currentPage = 0;
        private const int PageSize = 20;
        private bool _hasMoreItems = true;
        #endregion

        #region UI State
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isBorderVisible;
        [ObservableProperty] private bool isUnitBasedVisible;
        #endregion

        #region Header
        [ObservableProperty] private string currentDateTime = string.Empty;
        [ObservableProperty] private string loggedInUserName = string.Empty;
        [ObservableProperty] private string loggedInUserId = string.Empty;
        [ObservableProperty] private string storeName = "Kusina POS";
        #endregion

        #region Collections
        [ObservableProperty] private ObservableCollection<Category> categories = new();
        [ObservableProperty] private ObservableCollection<MenuItem> filteredMenuItems = new();
        #endregion

        #region Selection
        [ObservableProperty] private int selectedCategoryIndex;
        // Only for SegmentedControl filtering
        [ObservableProperty]
        private Category? selectedSegmentCategory;

        // Only for ComboBox editing
        [ObservableProperty]
        private Category? selectedCategoryForEdit;

        #endregion

        #region Form Fields
        [ObservableProperty] private int selectedMenuItemId;
        [ObservableProperty] private string menuItemName = string.Empty;
        [ObservableProperty] private string menuDescription = string.Empty;
        [ObservableProperty] private decimal menuItemPrice;
        [ObservableProperty] private bool isActive = true;
        [ObservableProperty] private string selectedMenuType = string.Empty;
        [ObservableProperty] private string selectedUnit = string.Empty;
        [ObservableProperty] private decimal initialStock;
        [ObservableProperty] private decimal costPerUnit;
        [ObservableProperty] private decimal reOrderLevel;

        [ObservableProperty] private string labelText = "Add New Menu Item";
        [ObservableProperty] private List<SfSegmentItem> segmentItems = new();

        #endregion

        #region Dropdown Sources
        [ObservableProperty]
        private ObservableCollection<string> menuTypes =
            new() { "Unit-Based", "Recipe-Based" };

        [ObservableProperty] private List<string> unitMeasurements = new();
        #endregion

        #region Image
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MenuImageSource))]
        [NotifyPropertyChangedFor(nameof(ImageLabel))]
        private string imagePath = string.Empty;

        public ImageSource MenuImageSource =>
            string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath)
                ? "kusinaposlogo.png"
                : ImageSource.FromFile(ImagePath);

        public string ImageLabel =>
            string.IsNullOrWhiteSpace(ImagePath) ? "Click to upload" : Path.GetFileName(ImagePath);
        #endregion

        public MenuItemViewModel(
            CategoryService categoryService,
            MenuItemService menuItemService,
            InventoryItemService inventoryItemService,
            MenuItemIngredientService menuItemIngredientService,
            InventoryTransactionService inventoryTransactionService,
            IDateTimeService dateTimeService,
            IDatabaseService databaseService)
        {
            _categoryService = categoryService;
            _menuItemService = menuItemService;
            _inventoryItemService = inventoryItemService;
            _menuItemIngredientService = menuItemIngredientService;
            _inventoryTransactionService = inventoryTransactionService;
            _dateTimeService = dateTimeService;
            _db = databaseService.GetConnection();

            LoadPreferences();
            UnitMeasurements = UnitMeasurementService.AllUnits;

            _dateTimeService.DateTimeChanged += (s, e) => CurrentDateTime = e;
            _ = InitializeAsync();
        }

        private void LoadPreferences()
        {
            LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
            CurrentDateTime = _dateTimeService.CurrentDateTime;
        }

        private async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await ReloadMenuItemsAsync();
        }

        #region Categories
        private void UpdateSegmentItems()
        {
            SegmentItems.Clear();

            foreach (var cat in Categories)
            {
                SegmentItems.Add(new SfSegmentItem
                {
                    Text = cat.Name
                    // Optional: add icon with ImageSource if you want
                });
            }

            OnPropertyChanged(nameof(SegmentItems));
        }

        [RelayCommand]
        public async Task LoadCategoriesAsync()
        {
            var list = await _categoryService.GetActiveCategoriesAsync();

            // Ensure UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Categories = new ObservableCollection<Category>(list);

                if (Categories.Any() && SelectedSegmentCategory == null)
                {
                    SelectedSegmentCategory = Categories[0];
                }
            });
            UpdateSegmentItems();
        }


        partial void OnSelectedCategoryIndexChanged(int value)
        {
            if (value >= 0 && value < Categories.Count)
            {
                SelectedSegmentCategory = Categories[value];
               // _ = ReloadMenuItemsAsync(); // Only reload menu items for the segment
            }
        }
        partial void OnSelectedSegmentCategoryChanged(Category? value)
        {
            Debug.WriteLine($"SelectedSegmentCategory changed to: {value?.Name}");
            if (value != null)
            {
                _ = ReloadMenuItemsAsync();
            }
        }


        #endregion

        #region Menu Items Paging
        private async Task ReloadMenuItemsAsync()
        {
            _currentPage = 0;
            _hasMoreItems = true;
            FilteredMenuItems.Clear();
            await LoadMoreMenuItemsAsync();
        }

        [RelayCommand]
        public async Task LoadMoreMenuItemsAsync()
        {
            if (IsBusy || !_hasMoreItems || SelectedSegmentCategory == null) return;

            IsBusy = true;

            var items = await _menuItemService.GetMenuItemsByCategoryPagedAsync(
                SelectedSegmentCategory.Name,
                _currentPage,
                PageSize);

            foreach (var item in items)
                FilteredMenuItems.Add(item);

            if (items.Count < PageSize)
                _hasMoreItems = false;

            _currentPage++;
            IsBusy = false;
        }
        #endregion

        #region CRUD

        [RelayCommand]
        public async Task SaveMenuItemAsync()
        {
            if (!ValidateMenuItem()) return;

            bool confirm = await PageHelper.DisplayConfirmAsync(
                "Confirm",
                SelectedMenuItemId == 0 ? "Add new menu item?" : "Update menu item?",
                "Yes", "No");

            if (!confirm) return;

            await _db.RunInTransactionAsync(tran =>
            {
                var item = new MenuItem
                {
                    Id = SelectedMenuItemId,
                    Name = MenuItemName.Trim(),
                    Description = MenuDescription?.Trim(),
                    Category = SelectedCategoryForEdit!.Name,
                    Price = MenuItemPrice,
                    Type = SelectedMenuType,
                    ImagePath = ImagePath,
                    IsActive = IsActive
                };

                if (SelectedMenuItemId == 0)
                {
                    tran.Insert(item);

                    if (SelectedMenuType == "Unit-Based")
                        HandleUnitBasedInventory(tran, item);
                }
                else
                {
                    tran.Update(item);
                }
            });

            HideBorder();
            await ReloadMenuItemsAsync();
        }

        private void HandleUnitBasedInventory(SQLiteConnection tran, MenuItem item)
        {
            var inv = new InventoryItem
            {
                Name = item.Name,
                Unit = SelectedUnit,
                QuantityOnHand = InitialStock,
                CostPerUnit = CostPerUnit,
                ReOrderLevel = ReOrderLevel,
                IsActive = true
            };

            tran.Insert(inv);

            tran.Insert(new MenuItemIngredient
            {
                MenuItemId = item.Id,
                InventoryItemId = inv.Id,
                InventoryItemName = inv.Name,
                UnitOfMeasurement = SelectedUnit,
                QuantityPerMenu = 1
            });

            if (InitialStock > 0)
            {
                tran.Insert(new InventoryTransaction
                {
                    InventoryItemId = inv.Id,
                    QuantityChange = InitialStock,
                    Reason = "Initial Stock",
                    TransactionDate = DateTime.Now
                });
            }
        }

        [RelayCommand]
        public void EditMenuItem(MenuItem item)
        {
            if (item == null) return;

            SelectedMenuItemId = item.Id;
            MenuItemName = item.Name;
            MenuDescription = item.Description;
            MenuItemPrice = item.Price;
            IsActive = item.IsActive;

            SelectedCategoryForEdit = Categories
                .FirstOrDefault(c =>
                    string.Equals(c.Name?.Trim(),
                                  item.Category?.Trim(),
                                  StringComparison.OrdinalIgnoreCase))
                ?? Categories.FirstOrDefault();

            SelectedMenuType = item.Type;
            ImagePath = item.ImagePath ?? string.Empty;
            LabelText = $"Edit: {item.Name}";

            ShowBorder();
        }

        [RelayCommand]
        public void AddMenuItem()
        {
            // Clear all fields
            SelectedMenuItemId = 0;
            MenuItemName = string.Empty;
            MenuDescription = string.Empty;
            MenuItemPrice = 0;
            SelectedMenuType = string.Empty;
            SelectedCategoryForEdit = Categories.FirstOrDefault();
            SelectedUnit = string.Empty;
            InitialStock = 0;
            CostPerUnit = 0;
            ReOrderLevel = 0;
            ImagePath = string.Empty;
            ShowBorder();
            LabelText = "Add New Menu Item";
            //IsBorderVisible = true;
        }

        [RelayCommand]
        public async Task DeleteMenuItemAsync(MenuItem item)
        {
            bool confirm = await PageHelper.DisplayConfirmAsync(
                "Delete",
                $"Delete {item.Name}?",
                "Yes", "No");

            if (!confirm) return;

            await _menuItemService.DeleteMenuItemAsync(item.Id);

            if (!string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath))
                File.Delete(item.ImagePath);

            await ReloadMenuItemsAsync();
        }
        #endregion

        #region UI Helpers
        [RelayCommand] private void ShowBorder() => IsBorderVisible = true;

        [RelayCommand]
        public void HideBorder()
        {
            SelectedMenuItemId = 0;
            MenuItemName = MenuDescription = SelectedMenuType = ImagePath = string.Empty;
            MenuItemPrice = InitialStock = CostPerUnit = ReOrderLevel = 0;
            IsBorderVisible = false;
            LabelText = "Add New Menu Item";
        }

        partial void OnSelectedMenuTypeChanged(string value)
            => IsUnitBasedVisible = value == "Unit-Based";
        #endregion

        #region Image Upload
        [RelayCommand]
        public async Task UploadImageAsync()
        {
            if (string.IsNullOrWhiteSpace(MenuItemName))
            {
                await PageHelper.DisplayAlertAsync("Info", "Enter item name first.", "OK");
                return;
            }

            var result = await MediaPicker.PickPhotoAsync();
            if (result == null) return;

            var dir = Path.Combine(FileSystem.AppDataDirectory, "menu_images");
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, $"{Guid.NewGuid()}{Path.GetExtension(result.FileName)}");

            using var src = await result.OpenReadAsync();
            using var dest = File.Create(path);
            await src.CopyToAsync(dest);

            ImagePath = path;
        }
        #endregion

        private bool ValidateMenuItem()
        {
            if (string.IsNullOrWhiteSpace(MenuItemName) ||
                SelectedCategoryForEdit == null ||
                MenuItemPrice <= 0)
            {
                _ = PageHelper.DisplayAlertAsync("Validation", "Please complete required fields.", "OK");
                return false;
            }
            return true;
        }
        public void ResetPaging()
        {
            _currentPage = 0;
            FilteredMenuItems.Clear();
        }
        //go back command
        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
        [RelayCommand]
        public async Task ShowMenuTypeDialogAsync()
        {
            await PageHelper.DisplayAlertAsync("Menu Types",
                "Unit-Based: Items sold by unit (e.g., pieces, bottles).\n\n" +
                "Recipe-Based: Items made from ingredients (e.g., dishes, meals).",
                "OK");
        }
    }
}
