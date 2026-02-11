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
    public partial class POSTerminalViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Category> menuCategories = new();

        [ObservableProperty]
        private bool isLoading = true;
        [ObservableProperty]
        private string gcashRefNumber;
        [ObservableProperty]
        private bool isProcessing = false;
        [ObservableProperty]
        private bool isCompleteOrderPopupOpen = false;

        private readonly CategoryService categoryService;
        private readonly MenuItemService menuItemService;
        private readonly MenuItemIngredientService menuItemIngredientService;
        private readonly SalesService salesService;
        private readonly InventoryItemService inventoryItemService;
        [ObservableProperty]
        private string selectedCategoryName = "All";

        [ObservableProperty]
        private ObservableCollection<MenuItem> allMenuItems = new();

        [ObservableProperty]
        private ObservableCollection<MenuItem> filteredMenuItems = new();
        [ObservableProperty]
        private ImageSource? gcashQrSource;
        // ORDER MANAGEMENT
        [ObservableProperty]
        private ObservableCollection<OrderItem> orderItems = new();

        [ObservableProperty]
        private string subtotalAmount = "0.00";

        [ObservableProperty]
        private string discountedAmount = "0.00";

        [ObservableProperty]
        private string vATAmount = "0.00";

        [ObservableProperty]
        private string totalPayableAmount = "0.00";

        [ObservableProperty]
        private string cashTenderedAmount = string.Empty;

        [ObservableProperty]
        private string changeAmount = "0.00";

        private decimal subtotal = 0;
        private decimal discountValue = 0;
        private decimal vatValue = 0;
        private decimal totalPayable = 0;

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
            InventoryItemService inventoryItemService)
        {
            try
            {
                Debug.WriteLine("=== POSTerminalViewModel Constructor Started ===");

                this.categoryService = categoryService;
                this.menuItemService = menuItemService;
                this.menuItemIngredientService = menuItemIngredientService;
                this.salesService = salesService;

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

                LoadSavedQrCode();
                Debug.WriteLine("=== POSTerminalViewModel Constructor Completed ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in POSTerminalViewModel constructor: {ex.Message}");
                IsLoading = false;
            }

            this.inventoryItemService = inventoryItemService;
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
        private void LoadSavedQrCode()
        {
            var savedPath = Preferences.Get(SettingsConstants.GCashQRCodeKey, string.Empty);
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                GcashQrSource = ImageSource.FromFile(savedPath);
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
                var ingredients = await menuItemIngredientService
                    .GetByMenuItemIdAsync(menuItem.Id);

                if (ingredients == null || ingredients.Count == 0)
                {
                    await PageHelper.DisplayAlertAsync(
                        "No Ingredients",
                        $"'{menuItem.Name}' has no ingredients configured.",
                        "OK");
                    return;
                }

                var existingItem = OrderItems
                    .FirstOrDefault(o => o.MenuItemId == menuItem.Id);

                var totalOrderQty =
                    menuItem.Quantity + (existingItem?.Quantity ?? 0);

                // 🔒 STOCK VALIDATION
                foreach (var ingredient in ingredients)
                {
                    var inventoryItem = await inventoryItemService
                        .GetInventoryItemByIdAsync(ingredient.InventoryItemId);

                    if (inventoryItem == null)
                    {
                        await PageHelper.DisplayAlertAsync(
                            "Inventory Missing",
                            $"Inventory record missing for '{ingredient.InventoryItemName}'.",
                            "OK");
                        return;
                    }

                    var requiredQty =
                        ingredient.QuantityPerMenu * totalOrderQty;

                    var reservedQty = await GetReservedInventoryQtyAsync(
                        ingredient.InventoryItemId,
                        menuItem.Id);

                    var availableQty =
                        inventoryItem.QuantityOnHand - reservedQty;

                    if (availableQty < requiredQty)
                    {
                        await PageHelper.DisplayAlertAsync(
                            "Insufficient Stock",
                            $"Not enough {ingredient.InventoryItemName}.\n\n" +
                            $"Required: {requiredQty}\n" +
                            $"Available: {availableQty}",
                            "OK");

                        return; // 🚫 BLOCK
                    }
                }

                // ✅ PASSED → UPDATE ORDER
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (existingItem != null)
                    {
                        existingItem.Quantity += menuItem.Quantity;
                    }
                    else
                    {
                        OrderItems.Add(new OrderItem
                        {
                            MenuItemId = menuItem.Id,
                            Name = menuItem.Name,
                            Price = menuItem.Price,
                            Quantity = menuItem.Quantity,
                            ImagePath = menuItem.ImagePath
                        });
                    }

                    menuItem.Quantity = 1;
                    CalculateTotals();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddToOrder error: {ex.Message}");

                await PageHelper.DisplayAlertAsync(
                    "Error",
                    ex.Message,
                    "OK");
            }
            finally
            {
                IsProcessing = false;
            }
        }


        private async Task<decimal> GetReservedInventoryQtyAsync(
            int inventoryItemId,
            int excludeMenuItemId = 0)
        {
            decimal reserved = 0;

            foreach (var item in OrderItems)
            {
                if (item.MenuItemId == excludeMenuItemId)
                    continue;

                var ingredients = await menuItemIngredientService
                    .GetByMenuItemIdAsync(item.MenuItemId);

                var ingredient = ingredients?
                    .FirstOrDefault(i => i.InventoryItemId == inventoryItemId);

                if (ingredient != null)
                {
                    reserved += ingredient.QuantityPerMenu * item.Quantity;
                }
            }

            return reserved;
        }
        [RelayCommand]
        private async Task IncreaseOrderItemQuantityAsync(OrderItem orderItem)
        {
            if (orderItem == null || IsProcessing) return;

            IsProcessing = true;

            try
            {
                var ingredients = await menuItemIngredientService
                    .GetByMenuItemIdAsync(orderItem.MenuItemId);

                if (ingredients == null || ingredients.Count == 0)
                {
                    await PageHelper.DisplayAlertAsync(
                        "No Ingredients",
                        $"Ingredients not configured for '{orderItem.Name}'.",
                        "OK");
                    return;
                }

                var nextQty = orderItem.Quantity + 1;

                foreach (var ingredient in ingredients)
                {
                    var inventoryItem = await inventoryItemService
                        .GetInventoryItemByIdAsync(ingredient.InventoryItemId);

                    if (inventoryItem == null)
                    {
                        await PageHelper.DisplayAlertAsync(
                            "Inventory Missing",
                            $"Inventory record missing for '{ingredient.InventoryItemName}'.",
                            "OK");
                        return;
                    }

                    var requiredQty =
                        ingredient.QuantityPerMenu * nextQty;

                    var reservedQty = await GetReservedInventoryQtyAsync(
                        ingredient.InventoryItemId,
                        orderItem.MenuItemId);

                    var availableQty =
                        inventoryItem.QuantityOnHand - reservedQty;

                    if (availableQty < requiredQty)
                    {
                        await PageHelper.DisplayAlertAsync(
                            "Insufficient Stock",
                            $"Not enough {ingredient.InventoryItemName} to increase quantity.\n\n" +
                            $"Required: {requiredQty}\n" +
                            $"Available: {availableQty}",
                            "OK");

                        return; // 🚫 BLOCK
                    }
                }

                // ✅ PASSED
                orderItem.Quantity++;
                CalculateTotals();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Increase quantity error: {ex.Message}");

                await PageHelper.DisplayAlertAsync(
                    "Error",
                    ex.Message,
                    "OK");
            }
            finally
            {
                IsProcessing = false;
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
                // 1. Calculate Subtotal
                subtotal = OrderItems.Sum(o => o.Subtotal);
                SubtotalAmount = subtotal.ToString();

                // 2. Get Discount Settings from Preferences
                bool allowDiscount = Preferences.Get(SettingsConstants.AllowDiscountKey, false);
                bool isDiscountFixedAmount = Preferences.Get(SettingsConstants.IsDiscountFixedAmountKey, false);
                bool isDiscountPercentage = Preferences.Get(SettingsConstants.IsDiscountPercentageKey, false);
                decimal discountSettingValue = (decimal)Preferences.Get(SettingsConstants.DiscountValueKey, 0.0);

                // 3. Calculate Discount
                decimal discountAmount = 0;
                if (allowDiscount)
                {
                    if (isDiscountFixedAmount)
                    {
                        // Fixed amount discount
                        discountAmount = discountSettingValue;
                    }
                    else if (isDiscountPercentage)
                    {
                        // Percentage discount
                        discountAmount = subtotal * (discountSettingValue / 100);
                    }
                }

                // 4. Calculate Discounted Amount
                discountValue = subtotal - discountAmount;
                DiscountedAmount = discountValue.ToString();

                // 5. Get VAT Settings from Preferences
                bool allowVAT = Preferences.Get(SettingsConstants.AllowVATKey, false);
                decimal vatSettingValue = (decimal)Preferences.Get(SettingsConstants.VATValueKey, 0.0);

                // 6. Calculate VAT
                decimal vatAmount = 0;
                if (allowVAT)
                {
                    vatAmount = discountValue * (vatSettingValue / 100);
                }
                vatValue = vatAmount;
                VATAmount = vatValue.ToString();

                // 7. Calculate Total Payable
                totalPayable = discountValue + vatValue;
                TotalPayableAmount = totalPayable.ToString();

                // 8. Calculate change if cash tendered
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
                    var change = cashTendered - totalPayable;
                    ChangeAmount = change >= 0 ? change.ToString() : "0.00";
                }
                else
                {
                    ChangeAmount = "0.00";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating change: {ex.Message}");
                ChangeAmount = "0.00";
            }
        }
        [RelayCommand]
        private async Task CompleteOrderAsync()
        {
            if (OrderItems.Count == 0)
            {
                await PageHelper.DisplayAlertAsync(
                    "Empty Order",
                    "Please add items to the order before checkout.",
                    "OK");
                return;
            }
            IsCompleteOrderPopupOpen = true;
        }
        [RelayCommand]
        private async Task ConfirmPaymentAsync()
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
                    cashTendered < totalPayable)
                {
                    await PageHelper.DisplayAlertAsync(
                        "Insufficient Payment",
                        "Please enter a valid cash amount.",
                        "OK");
                    return;
                }
                bool confirm = await PageHelper.DisplayConfirmAsync(
                    "Confirm Checkout",
                    "Are you sure you want to complete this order?",
                    "Yes",
                    "No");
                if (!confirm) return;
                Debug.WriteLine("=== Final inventory validation before checkout ===");

                // 🔒 FINAL INVENTORY CHECK (ORDER-WIDE)
                foreach (var orderItem in OrderItems)
                {
                    var ingredients = await menuItemIngredientService
                        .GetByMenuItemIdAsync(orderItem.MenuItemId);

                    if (ingredients == null || ingredients.Count == 0)
                    {
                        await PageHelper.DisplayAlertAsync(
                            "Configuration Error",
                            $"Ingredients not configured for '{orderItem.Name}'.",
                            "OK");
                        return;
                    }

                    foreach (var ingredient in ingredients)
                    {
                        var inventoryItem = await inventoryItemService
                            .GetInventoryItemByIdAsync(ingredient.InventoryItemId);

                        if (inventoryItem == null)
                        {
                            await PageHelper.DisplayAlertAsync(
                                "Inventory Missing",
                                $"Inventory record missing for '{ingredient.InventoryItemName}'.",
                                "OK");
                            return;
                        }

                        // Total required for THIS ingredient across ENTIRE ORDER
                        var totalRequiredQty =
                            ingredient.QuantityPerMenu *
                            OrderItems
                                .Where(o => o.MenuItemId == orderItem.MenuItemId)
                                .Sum(o => o.Quantity);

                        // Plus other menu items using same ingredient
                        foreach (var otherItem in OrderItems.Where(o => o.MenuItemId != orderItem.MenuItemId))
                        {
                            var otherIngredients = await menuItemIngredientService
                                .GetByMenuItemIdAsync(otherItem.MenuItemId);

                            var sharedIngredient = otherIngredients?
                                .FirstOrDefault(i => i.InventoryItemId == ingredient.InventoryItemId);

                            if (sharedIngredient != null)
                            {
                                totalRequiredQty +=
                                    sharedIngredient.QuantityPerMenu * otherItem.Quantity;
                            }
                        }

                        if (inventoryItem.QuantityOnHand < totalRequiredQty)
                        {
                            await PageHelper.DisplayAlertAsync(
                                "Insufficient Stock",
                                $"Stock changed while ordering.\n\n" +
                                $"Ingredient: {ingredient.InventoryItemName}\n" +
                                $"Required: {totalRequiredQty}\n" +
                                $"Available: {inventoryItem.QuantityOnHand}",
                                "OK");

                            Debug.WriteLine(
                                $"=== FINAL BLOCK: {ingredient.InventoryItemName} | Required: {totalRequiredQty}, Available: {inventoryItem.QuantityOnHand} ===");

                            return; // 🚫 HARD STOP
                        }
                    }
                }

                Debug.WriteLine("=== Inventory validated. Proceeding to sale ===");

                // 🧾 PROCESS SALE (BACKGROUND)
                var receiptNo = await Task.Run(async () =>
                {
                    try
                    {
                        decimal change = cashTendered - totalPayable;
                        // Generate the new ID
                        var newReceiptNo = await salesService.GenerateReceiptNoAsync();
                        // Calculate discount amount for saving to database
                        decimal discountAmountForDB = subtotal - discountValue;

                        var sale = new Sale
                        {
                            SaleDate = DateTime.Now,
                            ReceiptNo = newReceiptNo,
                            SubTotal = subtotal,
                            Discount = discountAmountForDB,
                            Tax = vatValue,
                            TotalAmount = totalPayable,
                            AmountPaid = cashTendered,
                            ChangeAmount = change,
                            PaymentMethod = "Cash"
                        };

                        var saleItems = OrderItemMapper.ToSaleItems(OrderItems);

                        int result = await salesService
                            .CompleteSaleAsync(sale, saleItems);

                        return result > 0 ? sale.ReceiptNo : null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"=== Error during sale commit: {ex.Message} ===");
                        return null;
                    }
                });

                if (receiptNo != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OrderItems.Clear();
                        CashTenderedAmount = string.Empty;
                        CalculateTotals();
                    });

                    await PageHelper.DisplayAlertAsync(
                        "Success",
                        $"Order completed successfully!\nSales No: {receiptNo}",
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
                IsCompleteOrderPopupOpen = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmGcashPaymentAsync()
        {
            if (IsProcessing) return;

            // 1. GCash Specific Validation
            if (string.IsNullOrWhiteSpace(GcashRefNumber) || GcashRefNumber.Length < 13)
            {
                await PageHelper.DisplayAlertAsync("Invalid Reference",
                    "Please enter a valid 13-digit GCash reference number.", "OK");
                return;
            }

            // 2. Call the shared processing logic, passing "GCash" as the method
            await ProcessOrderCheckoutAsync(paymentMethod: "GCash", reference: GcashRefNumber);
        }
        private async Task ProcessOrderCheckoutAsync(string paymentMethod, string reference = "")
        {
            IsProcessing = true;
            try
            {
                // --- 1. PRE-CHECK ---
                if (OrderItems.Count == 0) return;

                bool confirm = await PageHelper.DisplayConfirmAsync("Confirm Checkout",
                    $"Complete this {paymentMethod} order?", "Yes", "No");
                if (!confirm) return;

                // --- 2. INVENTORY VALIDATION (Copy your existing foreach loops here) ---
                // [Insert your existing 'foreach (var orderItem in OrderItems)' block here]
                // This ensures inventory is checked exactly the same way for GCash.

                // --- 3. PROCESS SALE ---
                var receiptNo = await Task.Run(async () =>
                {
                    try
                    {
                        var newReceiptNo = await salesService.GenerateReceiptNoAsync();

                        var sale = new Sale
                        {
                            SaleDate = DateTime.Now,
                            ReceiptNo = newReceiptNo,
                            SubTotal = subtotal,
                            Discount = subtotal - discountValue, // Based on your math
                            Tax = vatValue,
                            TotalAmount = totalPayable,

                            // GCash Logic: Paid is always Total, Change is always 0
                            AmountPaid = paymentMethod == "GCash" ? totalPayable : decimal.Parse(CashTenderedAmount),
                            ChangeAmount = paymentMethod == "GCash" ? 0 : (decimal.Parse(CashTenderedAmount) - totalPayable),

                            PaymentMethod = paymentMethod,
                            CashLessReference = reference // Add this property to your Sale model if not there
                        };

                        var saleItems = OrderItemMapper.ToSaleItems(OrderItems);
                        int result = await salesService.CompleteSaleAsync(sale, saleItems);

                        return result > 0 ? sale.ReceiptNo : null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        return null;
                    }
                });

                // --- 4. SUCCESS UI UPDATE ---
                if (receiptNo != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OrderItems.Clear();
                        CashTenderedAmount = string.Empty;
                        GcashRefNumber = string.Empty; // Clear GCash specific field
                        CalculateTotals();
                    });

                    await PageHelper.DisplayAlertAsync("Success", $"Order completed!\nReceipt: {receiptNo}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                IsCompleteOrderPopupOpen = false;
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


        [RelayCommand]
        private void NumberPad(string number)
        {
            if (string.IsNullOrEmpty(CashTenderedAmount))
            {
                CashTenderedAmount = number;
            }
            else
            {
                CashTenderedAmount += number;
            }
        }
        [RelayCommand]
        private void Backspace()
        {
            if (!string.IsNullOrEmpty(CashTenderedAmount) && CashTenderedAmount.Length > 0)
            {
                CashTenderedAmount = CashTenderedAmount.Substring(0, CashTenderedAmount.Length - 1);
            }
        }
    }
}
