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

        [ObservableProperty]
        private bool isProcessing = false;

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

        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public POSTerminalViewModel(
            CategoryService categoryService,
            MenuItemService menuItemService,
            MenuItemIngredientService menuItemIngredientService,
            SalesService salesService,
            IDateTimeService dateTimeService)
        {
            try
            {
                Debug.WriteLine("=== POSTerminalViewModel Constructor Started ===");

                this.categoryService = categoryService;
                this.menuItemService = menuItemService;
                this.menuItemIngredientService = menuItemIngredientService;
                this.salesService = salesService;
                _dateTimeService = dateTimeService;

                // Initialize with default values immediately
                MenuCategories = new ObservableCollection<Category>
                {
                    new Category { Id = 0, Name = "All", IsSelected = true }
                };

                AllMenuItems = new ObservableCollection<MenuItem>();
                FilteredMenuItems = new ObservableCollection<MenuItem>();
                OrderItems = new ObservableCollection<OrderItem>();

                // Load user preferences immediately (fast operation)
                LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
                LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
                StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

                // Subscribe to datetime updates
                _dateTimeService.DateTimeChanged += OnDateTimeChanged;
                CurrentDateTime = _dateTimeService.CurrentDateTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                Debug.WriteLine("=== POSTerminalViewModel Constructor Completed ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in POSTerminalViewModel constructor: {ex.Message}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// Call this from the page's OnAppearing to initialize data
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                Debug.WriteLine("=== Starting POSTerminalViewModel InitializeAsync ===");
                await InitializeCollectionsAsync();
                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
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

                // Load data in background thread
                await Task.Run(async () =>
                {
                    try
                    {
                        // Load categories
                        var categoryList = await categoryService.GetAllCategoriesAsync();
                        Debug.WriteLine($"=== Loaded {categoryList?.Count() ?? 0} categories ===");

                        // Load menu items
                        var menuItems = await menuItemService.GetAllMenuItemsAsync();
                        Debug.WriteLine($"=== Loaded {menuItems?.Count() ?? 0} menu items ===");

                        // Update UI on main thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            // Clear and rebuild categories
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

                            // Load menu items
                            AllMenuItems.Clear();
                            if (menuItems != null)
                            {
                                foreach (var item in menuItems)
                                {
                                    AllMenuItems.Add(item);
                                }
                            }

                            // Apply initial filter
                            FilterMenuItems();
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"=== Error in background loading: {ex.Message} ===");
                        throw;
                    }
                });

                Debug.WriteLine("=== InitializeCollectionsAsync completed successfully ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== Error loading collections: {ex.Message} ===");
                Debug.WriteLine($"=== Stack trace: {ex.StackTrace} ===");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await PageHelper.DisplayAlertAsync("Error",
                        $"Failed to load data: {ex.Message}", "OK");
                });
            }
            finally
            {
                IsLoading = false;
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
            try
            {
                var items = await Task.Run(async () =>
                    await menuItemService.GetAllMenuItemsAsync());

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AllMenuItems.Clear();
                    foreach (var item in items)
                    {
                        AllMenuItems.Add(item);
                    }

                    FilterMenuItems(); // Apply initial filter
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading menu items: {ex.Message}");
            }
        }

        [RelayCommand]
        private void SelectCategory(Category category)
        {
            try
            {
                Debug.WriteLine($"=== Category selected: {category?.Name} ===");

                // Setting this property triggers OnSelectedCategoryNameChanged
                SelectedCategoryName = category?.Name ?? "All";

                // Update selection state for the UI
                foreach (var cat in MenuCategories)
                {
                    cat.IsSelected = (category != null && cat.Id == category.Id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting category: {ex.Message}");
            }
        }

        partial void OnSelectedCategoryNameChanged(string value)
        {
            // We launch a background task here to ensure the UI remains fluid
            // while FilterMenuItems does its work.
            Task.Run(() =>
            {
                FilterMenuItems();
            });
        }

        private void FilterMenuItems()
        {
            try
            {
                // 1. Start Loading (Ensure this happens on MainThread for UI binding)
                MainThread.BeginInvokeOnMainThread(() => IsLoading = true);

                if (AllMenuItems == null || AllMenuItems.Count == 0)
                {
                    MainThread.BeginInvokeOnMainThread(() => FilteredMenuItems.Clear());
                    return;
                }

                // 2. Perform the actual data filtering on the CURRENT thread (Background)
                List<MenuItem> results;
                if (SelectedCategoryName == "All")
                {
                    results = AllMenuItems.ToList();
                }
                else
                {
                    results = AllMenuItems.Where(m => m.Category == SelectedCategoryName).ToList();
                }

                // 3. Update the UI Collection on the Main Thread
                // This is the only part that MUST be on the Main Thread.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FilteredMenuItems.Clear();
                    foreach (var item in results)
                    {
                        FilteredMenuItems.Add(item);
                    }
                    Debug.WriteLine($"=== Filtered items count: {FilteredMenuItems.Count} ===");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering menu items: {ex.Message}");
            }
            finally
            {
                // 4. Stop Loading
                MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
            }
        }

        // ======================
        // ORDER MANAGEMENT
        // ======================
        [RelayCommand]
        private async Task AddToOrder(MenuItem menuItem)
        {
            if (menuItem == null || IsProcessing) return;

            IsProcessing = true;
            try
            {
                Debug.WriteLine($"=== Checking ingredients for: {menuItem.Name} ===");

                // Check ingredients in background
                var ingredients = await Task.Run(async () =>
                    await menuItemIngredientService.GetByMenuItemIdAsync(menuItem.Id));

                if (ingredients == null || ingredients.Count == 0)
                {
                    await PageHelper.DisplayAlertAsync(
                        "No Ingredients",
                        $"'{menuItem.Name}' has no ingredients configured. Please set up ingredients before adding to order.",
                        "OK");
                    return;
                }

                Debug.WriteLine($"=== Found {ingredients.Count} ingredients for {menuItem.Name} ===");

                // Update order on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
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
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== Error adding to order: {ex.Message} ===");
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to add item to order: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void IncreaseOrderItemQuantity(OrderItem orderItem)
        {
            try
            {
                if (orderItem != null)
                {
                    orderItem.Quantity++;
                    CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error increasing quantity: {ex.Message}");
            }
        }

        [RelayCommand]
        private void DecreaseOrderItemQuantity(OrderItem orderItem)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decreasing quantity: {ex.Message}");
            }
        }

        [RelayCommand]
        private void RemoveOrderItem(OrderItem orderItem)
        {
            try
            {
                if (orderItem != null)
                {
                    OrderItems.Remove(orderItem);
                    CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing order item: {ex.Message}");
            }
        }

        private void CalculateTotals()
        {
            try
            {
                subtotal = OrderItems.Sum(o => o.Subtotal);
                SubtotalAmount = $"₱{subtotal:F2}";

                // Calculate change if cash tendered
                CalculateChange();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating totals: {ex.Message}");
            }
        }

        partial void OnCashTenderedAmountChanged(string value)
        {
            try
            {
                CalculateChange();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error on cash tendered change: {ex.Message}");
            }
        }

        private void CalculateChange()
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating change: {ex.Message}");
                ChangeAmount = "₱0.00";
            }
        }

        [RelayCommand]
        private async Task CompleteOrder()
        {
            if (IsProcessing) return;

            IsProcessing = true;
            try
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

                Debug.WriteLine("=== Processing order completion ===");

                // Process sale in background
                var success = await Task.Run(async () =>
                {
                    try
                    {
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

                        return result > 0 ? sale.ReceiptNo : null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"=== Error in background sale processing: {ex.Message} ===");
                        return null;
                    }
                });

                if (success != null)
                {
                    // Update UI on main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OrderItems.Clear();
                        CashTenderedAmount = string.Empty;
                        CalculateTotals();
                    });

                    await PageHelper.DisplayAlertAsync(
                        "Success",
                        $"Order completed successfully!\nSales No: {success}",
                        "OK");
                }
                else
                {
                    await PageHelper.DisplayAlertAsync(
                        "Error",
                        "Failed to complete order. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== Error completing order: {ex.Message} ===");
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    "Failed to complete order. Please try again.",
                    "OK");
            }
            finally
            {
                IsProcessing = false;
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

        /// <summary>
        /// Cleanup method - call from page OnDisappearing
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (_dateTimeService != null)
                {
                    _dateTimeService.DateTimeChanged -= OnDateTimeChanged;
                }
                Debug.WriteLine("=== POSTerminalViewModel cleaned up ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in cleanup: {ex.Message}");
            }
        }
    }
}