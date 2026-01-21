using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.ViewModel
{
    public partial class POSTerminalViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Category> menuCategories = new();

        [ObservableProperty]
        private bool isLoading = true;

        private readonly CategoryService categoryService;
        private readonly MenuItemService menuItemService;
        private readonly MenuItemIngredientService menuItemIngredientService; 
        private readonly SalesService salesService;
        private readonly IDateTimeService _dateTimeService;
        [ObservableProperty]
        private string selectedCategoryName = "All";

        [ObservableProperty]
        private ObservableCollection<MenuItem> allMenuItems = new();

        [ObservableProperty]
        private ObservableCollection<MenuItem> filteredMenuItems = new();

        // ORDER MANAGEMENT
        [ObservableProperty]
        private ObservableCollection<OrderItem> orderItems = new();

        [ObservableProperty]
        private string subtotalAmount = "₱0.00";

        [ObservableProperty]
        private string cashTenderedAmount = string.Empty;

        [ObservableProperty]
        private string changeAmount = "₱0.00";

        private decimal subtotal = 0;
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
        // SINGLE CONSTRUCTOR - removed duplicate
        public POSTerminalViewModel(
            CategoryService categoryService,
            MenuItemService menuItemService,
            MenuItemIngredientService menuItemIngredientService,
            SalesService salesService,
            IDateTimeService dateTimeService)
        {
            this.categoryService = categoryService;
            this.menuItemService = menuItemService;
            this.menuItemIngredientService = menuItemIngredientService;
            this.salesService = salesService;
            _dateTimeService = dateTimeService;
            // Initialize with empty collection first
            MenuCategories = new ObservableCollection<Category>
            {
                new Category { Id = 0, Name = "All", IsSelected = true },
                new Category { Id = 1, Name = "Meals", IsSelected = false },
                new Category { Id = 2, Name = "Drinks", IsSelected = false },
                new Category { Id = 3, Name = "Desserts", IsSelected = false }
            };

            Debug.WriteLine("=== POSTerminalViewModel Constructor ===");

            try
            {
                LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
                LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
                StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

                _dateTimeService.DateTimeChanged += OnDateTimeChanged;
                CurrentDateTime = _dateTimeService.CurrentDateTime;

                _ = InitializeCollectionsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MenuItemViewModel constructor: {ex.Message}");
            }

            
        }

        // ======================
        // INITIALIZATION
        // ======================
        private async Task InitializeCollectionsAsync()
        {
            try
            {
                Debug.WriteLine("=== Starting InitializeCollectionsAsync ===");
                IsLoading = true;

                // Load categories
                var categoryList = await categoryService.GetAllCategoriesAsync();
                Debug.WriteLine($"=== Loaded {categoryList?.Count() ?? 0} categories ===");

                // Clear and add to existing collection
                MenuCategories.Clear();

                // Add "All" category first
                MenuCategories.Add(new Category
                {
                    Id = 0,
                    Name = "All",
                    IsSelected = true
                });

                // Add rest of categories
                if (categoryList != null)
                {
                    foreach (var category in categoryList)
                    {
                        category.IsSelected = false;
                        MenuCategories.Add(category);
                        Debug.WriteLine($"=== Added category: {category.Name} ===");
                    }
                }

                Debug.WriteLine($"=== Total MenuCategories: {MenuCategories.Count} ===");
                IsLoading = false;
                await LoadMenuItemsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== Error loading categories: {ex.Message} ===");
                Debug.WriteLine($"=== Stack trace: {ex.StackTrace} ===");

                IsLoading = false;
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to load data: {ex.Message}", "OK");
            }
        }
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
        private async Task LoadMenuItemsAsync()
        {
            var items = await menuItemService.GetAllMenuItemsAsync();

            AllMenuItems.Clear();
            foreach (var item in items)
                AllMenuItems.Add(item);

            FilterMenuItems(); // Apply initial filter
        }

        [RelayCommand]
        private void SelectCategory(Category category)
        {
            Debug.WriteLine($"=== Category selected: {category?.Name} ===");
            SelectedCategoryName = category?.Name ?? "All";

            // Deselect all categories
            foreach (var cat in MenuCategories)
            {
                cat.IsSelected = false;
            }

            // Select the clicked category
            if (category != null)
            {
                category.IsSelected = true;
            }
        }

        partial void OnSelectedCategoryNameChanged(string value)
        {
            Debug.WriteLine($"=== Filtering menu items for category: {value} ===");
            FilterMenuItems();
        }

        private void FilterMenuItems()
        {
            if (AllMenuItems == null || AllMenuItems.Count == 0)
                return;

            FilteredMenuItems.Clear();

            if (SelectedCategoryName == "All")
            {
                foreach (var item in AllMenuItems)
                    FilteredMenuItems.Add(item);

                Debug.WriteLine($"=== Showing ALL items: {FilteredMenuItems.Count} ===");
                return;
            }

            var filtered = AllMenuItems
                .Where(m => m.Category == SelectedCategoryName);

            foreach (var item in filtered)
                FilteredMenuItems.Add(item);

            Debug.WriteLine($"=== Filtered items count: {FilteredMenuItems.Count} ===");
        }

        // ======================
        // ORDER MANAGEMENT
        // ======================
        [RelayCommand]
        private async Task AddToOrder(MenuItem menuItem)
        {
            if (menuItem == null) return;

            Debug.WriteLine($"=== Checking ingredients for: {menuItem.Name} ===");

            try
            {
                // Check if menu item has ingredients
                var ingredients = await menuItemIngredientService.GetByMenuItemIdAsync(menuItem.Id);

                if (ingredients == null || ingredients.Count == 0)
                {
                    await PageHelper.DisplayAlertAsync(
                        "No Ingredients",
                        $"'{menuItem.Name}' has no ingredients configured. Please set up ingredients before adding to order.",
                        "OK");
                    return;
                }

                Debug.WriteLine($"=== Found {ingredients.Count} ingredients for {menuItem.Name} ===");
                Debug.WriteLine($"=== Adding to order: {menuItem.Name} x {menuItem.Quantity} ===");

                // Check if item already exists in order
                var existingItem = OrderItems.FirstOrDefault(o => o.MenuItemId == menuItem.Id);

                if (existingItem != null)
                {
                    // Update quantity if already in order
                    existingItem.Quantity += menuItem.Quantity;
                    Debug.WriteLine($"=== Updated existing item. New quantity: {existingItem.Quantity} ===");
                }
                else
                {
                    // Add new item to order
                    var orderItem = new OrderItem
                    {
                        MenuItemId = menuItem.Id,
                        Name = menuItem.Name,
                        Price = menuItem.Price,
                        Quantity = menuItem.Quantity,
                        ImagePath = menuItem.ImagePath
                    };
                    OrderItems.Add(orderItem);
                    Debug.WriteLine($"=== Added new item to order ===");
                }

                // Reset quantity back to 1
                menuItem.Quantity = 1;

                // Recalculate totals
                CalculateTotals();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== Error checking ingredients: {ex.Message} ===");
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to verify ingredients: {ex.Message}",
                    "OK");
            }
        }

        [RelayCommand]
        private void IncreaseOrderItemQuantity(OrderItem orderItem)
        {
            if (orderItem != null)
            {
                orderItem.Quantity++;
                CalculateTotals();
            }
        }

        [RelayCommand]
        private void DecreaseOrderItemQuantity(OrderItem orderItem)
        {
            if (orderItem != null)
            {
                if (orderItem.Quantity > 1)
                {
                    orderItem.Quantity--;
                    CalculateTotals();
                }
                else
                {
                    // Remove item if quantity would go to 0
                    OrderItems.Remove(orderItem);
                    CalculateTotals();
                }
            }
        }

        [RelayCommand]
        private void RemoveOrderItem(OrderItem orderItem)
        {
            if (orderItem != null)
            {
                OrderItems.Remove(orderItem);
                CalculateTotals();
            }
        }

        private void CalculateTotals()
        {
            subtotal = OrderItems.Sum(o => o.Subtotal);
            SubtotalAmount = $"₱{subtotal:F2}";

            // Calculate change if cash tendered
            CalculateChange();
        }

        partial void OnCashTenderedAmountChanged(string value)
        {
            CalculateChange();
        }

        private void CalculateChange()
        {
            if (decimal.TryParse(CashTenderedAmount, out decimal cashTendered))
            {
                var change = cashTendered - subtotal;
                ChangeAmount = change >= 0 ? $"₱{change:F2}" : "₱0.00";
            }
            else
            {
                ChangeAmount = "₱0.00";
            }
        }

        [RelayCommand]
        private async Task CompleteOrder()
        {
            if (OrderItems.Count == 0)
            {
                await PageHelper.DisplayAlertAsync(
                    "Empty Order",
                    "Please add items to the order.",
                    "OK");
                return;
            }

            if (!decimal.TryParse(CashTenderedAmount, out decimal cashTendered) ||
                cashTendered < subtotal)
            {
                await PageHelper.DisplayAlertAsync(
                    "Insufficient Payment",
                    "Please enter a valid cash amount.",
                    "OK");
                return;
            }

            try
            {
                // Use DECIMAL values, not formatted strings
                decimal subTotal = subtotal;
                decimal change = cashTendered - subTotal;

                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    ReceiptNo = GenerateReceiptNo(),
                    SubTotal = subTotal,
                    Discount = 0,
                    Tax = 0,
                    TotalAmount = subTotal,
                    AmountPaid = cashTendered,
                    ChangeAmount = change
                };

                var saleItems = OrderItemMapper.ToSaleItems(OrderItems);

                int result = await salesService.CompleteSaleAsync(sale, saleItems);

                // ✅ ONLY clear & notify AFTER successful save
                OrderItems.Clear();
                CashTenderedAmount = string.Empty;
                CalculateTotals();

                await PageHelper.DisplayAlertAsync(
                    "Success",
                    $"Order completed successfully!\nSales No: {sale.ReceiptNo}",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== Error completing order: {ex} ===");

                await PageHelper.DisplayAlertAsync(
                    "Error",
                    "Failed to complete order. Please try again.",
                    "OK");
            }
        }
        private string GenerateReceiptNo()
        {
            return $"SALESID-{DateTime.Now:yyyyMMddHHmmssfff}";
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
    }
}