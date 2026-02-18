using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using KusinaPOS.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace KusinaPOS.ViewModel
{
    /// <summary>
    /// ViewModel responsible for Inventory Item management:
    /// - CRUD operations
    /// - Stock adjustments
    /// - Transaction logging
    /// </summary>
    public partial class InventoryItemViewModel : ObservableObject
    {
        // ======================================================
        // DEPENDENCIES (SERVICES)
        // ======================================================
        private readonly InventoryItemService _inventoryService;
        private readonly InventoryTransactionService _inventoryTransactionService;
        private readonly IDateTimeService _dateTimeService;

        // Used for debounced search
        private CancellationTokenSource? _searchCts;

        // Holds the original quantity before editing (baseline for diff)
        [ObservableProperty] private decimal _originalQuantityOnHand;

        // ======================================================
        // COLLECTIONS
        // ======================================================
        [ObservableProperty]
        private ObservableCollection<InventoryItem> inventoryItems = new();
        [ObservableProperty]
        private ObservableCollection<InventoryTransaction> transactionLogs = new();

        [ObservableProperty]
        private InventoryItem? selectedItem;

        // ======================================================
        // EDITING FIELDS (BOUND TO EDIT PANEL)
        // ======================================================
        [ObservableProperty] private int editingId;
        [ObservableProperty] private string editingName = string.Empty;
        [ObservableProperty] private string editingUnit = string.Empty;
        [ObservableProperty] private decimal editingQuantityOnHand;
        [ObservableProperty] private decimal editingCostPerUnit;
        [ObservableProperty] private decimal editingReOrderLevel;
        [ObservableProperty] private bool editingIsActive = true;
        [ObservableProperty] private string remarks = string.Empty;


        // ======================================================
        // UI STATE
        // ======================================================
        [ObservableProperty] private bool isEditPanelVisible;
        [ObservableProperty] private string editPanelTitle = "Edit Inventory Item";
        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private bool isTransactionPanelVisible;
        [ObservableProperty] private string transactionLogPanelTitle;
        [ObservableProperty] private string costPerUnitLabel = "Cost Per Unit (₱)";

        // =========================
        // Units (Autocomplete)
        // =========================
        [ObservableProperty]
        private List<string> unitMeasurements = new();

        // ======================================================
        // HEADER / SESSION INFO
        // ======================================================
        [ObservableProperty] private string loggedInUserName = string.Empty;
        [ObservableProperty] private string currentDateTime;
        [ObservableProperty] private string storeName;

        // ======================================================
        // STOCK ADJUSTMENT / TRANSACTION INFO
        // ======================================================
        [ObservableProperty] private List<string> reasons;
        [ObservableProperty] private string selectedReason = string.Empty;
        [ObservableProperty] private decimal quantityChanged;
        [ObservableProperty] private string displayQuantityChanged = "0";
        public bool IsQuantityChanged => QuantityChanged != 0;


        // ======================================================
        // CONSTRUCTOR
        // ======================================================
        public InventoryItemViewModel(
            InventoryItemService inventoryItemService,
            IDateTimeService dateTimeService,
            InventoryTransactionService inventoryTransactionService)
        {
            _inventoryService = inventoryItemService;
            _dateTimeService = dateTimeService;
            _inventoryTransactionService = inventoryTransactionService;

            try
            {
                // Load initial inventory list
                _ = LoadInventoryItems();

                // Load persisted session values
                LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
                StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

                // Predefined stock adjustment reasons
                Reasons = new() { "Stock In", "Adjustment","Waste" };

                // Live date/time updates
                _dateTimeService.DateTimeChanged += (_, dt) => CurrentDateTime = dt;
                CurrentDateTime = _dateTimeService.CurrentDateTime;

                // Ensure quantity change display is initialized
                InitializeQuantityChange();

                // Load all unit measurements initially
                UnitMeasurements = UnitMeasurementService.AllUnits;

               // _ = SeedInventoryDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InventoryItemViewModel constructor: {ex.Message}");
            }
        }

        // ======================================================
        // ADD INVENTORY ITEM
        // ======================================================
        [RelayCommand]
        private void AddItem()
        {
            try
            {
                // Reset editing fields
                EditingId = 0;
                EditingName = string.Empty;
                EditingUnit = string.Empty;
                EditingQuantityOnHand = 0;
                EditingCostPerUnit = 0;
                EditingReOrderLevel = 0;
                EditingIsActive = true;

                // Reset baseline quantity
                OriginalQuantityOnHand = 0;
                QuantityChanged = 0;

                // Initialize quantity change display
                InitializeQuantityChange();
                SelectedReason = string.Empty;
                Remarks = string.Empty;
                EditPanelTitle = "Add Inventory Item";
                IsEditPanelVisible = true;
                IsTransactionPanelVisible = !IsEditPanelVisible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddItem: {ex.Message}");
            }
        }

        // ======================================================
        // EDIT INVENTORY ITEM
        // ======================================================
        [RelayCommand]
        private void EditItem(InventoryItem item)
        {
            try
            {
                if (item == null) return;

                // Populate editing fields from selected item
                EditingId = item.Id;
                EditingName = item.Name;
                EditingUnit = item.Unit;
                EditingQuantityOnHand = item.QuantityOnHand;
                EditingCostPerUnit = item.CostPerUnit;
                EditingReOrderLevel = item.ReOrderLevel;
                EditingIsActive = item.IsActive;

                // Capture original quantity for diff calculation
                OriginalQuantityOnHand = item.QuantityOnHand;
                QuantityChanged = 0;

                InitializeQuantityChange();
                SelectedReason = string.Empty;
                Remarks = string.Empty;
                EditPanelTitle = $"Edit Inventory Item: {item.Name}";
                IsEditPanelVisible = true;
                IsTransactionPanelVisible = !IsEditPanelVisible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EditItem: {ex.Message}");
            }
        }
        partial void OnQuantityChangedChanged(decimal oldValue, decimal newValue)
        {
            OnPropertyChanged(nameof(IsQuantityChanged));

            // Optional: reset reason when quantity goes back to zero
            if (newValue == 0)
                SelectedReason = string.Empty;
        }

        // ======================================================
        // CANCEL EDITING
        // ======================================================
        [RelayCommand]
        private void Cancel()
        {
            try
            {
                IsEditPanelVisible = false;
                SelectedItem = null;

                // Clear editing state
                EditingId = 0;
                EditingName = string.Empty;
                EditingUnit = string.Empty;
                EditingQuantityOnHand = 0;
                EditingCostPerUnit = 0;
                EditingReOrderLevel = 0;
                EditingIsActive = true;

                QuantityChanged = 0;
                OriginalQuantityOnHand = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Cancel: {ex.Message}");
            }
        }

        // ======================================================
        // SAVE CHANGES (ADD / UPDATE)
        // ======================================================
        [RelayCommand]
        private async Task SaveChanges()
        {
            try
            {
                // Validation checks
                bool confirm = await PageHelper.DisplayConfirmAsync(
                    "Confirm Save",
                    "Are you sure you want to save changes to this inventory item?",
                    "Yes",
                    "No");
                if(!confirm) return;
                if (string.IsNullOrWhiteSpace(EditingName) ||
                    string.IsNullOrWhiteSpace(EditingUnit))
                {
                    await PageHelper.DisplayAlertAsync("Error", "Name and Unit are required", "OK");
                    return;
                }

                if (EditingQuantityOnHand < 0 || EditingCostPerUnit < 0)
                {
                    await PageHelper.DisplayAlertAsync("Error", "Values cannot be negative", "OK");
                    return;
                }

                if (IsQuantityChanged && string.IsNullOrEmpty(SelectedReason))
                {
                    await PageHelper.DisplayAlertAsync(
                        "Error",
                        "Please select a reason for the quantity adjustment",
                        "OK");
                    return;
                }
                var originalItem = InventoryItems.FirstOrDefault(i => i.Id == EditingId);

                if (originalItem != null) // Update Mode
                {
                    bool costChanged = originalItem.CostPerUnit != EditingCostPerUnit;

                    // THE NEW RULE: 
                    // If the cost is different, IsQuantityChanged MUST be true.
                    if (costChanged && !IsQuantityChanged)
                    {
                        await PageHelper.DisplayAlertAsync(
                            "Price Update Required",
                            "To update the Cost Per Unit, you must also record a Quantity Adjustment (Stock In).",
                            "OK");
                        return;
                    }
                }
                // Inside SaveChanges validation block
                if (IsQuantityChanged)
                {
                    // Rule: Stock In must be Positive
                    if (SelectedReason == "Stock In" && QuantityChanged <= 0)
                    {
                        await PageHelper.DisplayAlertAsync("Invalid Quantity",
                            "Stock In must be a positive number. Use 'Adjustment' or 'Waste' for deductions.", "OK");
                        return;
                    }

                    // Rule: Waste/Damage must be Negative (or we force it to be negative)
                    if ((SelectedReason == "Waste" || SelectedReason == "Adjustment") && QuantityChanged > 0)
                    {
                        // Optional: You can either block them or automatically flip the sign
                        await PageHelper.DisplayAlertAsync("Invalid Quantity",
                           "Waste or Adjustments usually represent a decrease in stock.", "OK");
                        return;
                    }
                }

                // ADD MODE
                if (EditingId == 0)
                {
                    var item = new InventoryItem
                    {
                        Name = EditingName,
                        Unit = EditingUnit,
                        QuantityOnHand = EditingQuantityOnHand,
                        CostPerUnit = EditingCostPerUnit,
                        ReOrderLevel = EditingReOrderLevel,
                        IsActive = EditingIsActive
                    };

                    await _inventoryService.AddInventoryItemAsync(item);
                    InventoryItems.Add(item);

                    // Create transaction ONLY if initial quantity exists
                    if (IsQuantityChanged)
                    {
                        var inventoryTransaction = new InventoryTransaction
                        {
                            InventoryItemId = item.Id,
                            QuantityChange = QuantityChanged,
                            CostAtTransaction = EditingCostPerUnit,
                            Reason = SelectedReason,
                            Remarks = Remarks,
                            TransactionDate = DateTime.Now,
                        };

                        await _inventoryTransactionService.AddInventoryTransactionAsync(inventoryTransaction);
                    }
                }

                // UPDATE MODE
                else
                {
                    var item = new InventoryItem
                    {
                        Id = EditingId,
                        Name = EditingName,
                        Unit = EditingUnit,
                        QuantityOnHand = EditingQuantityOnHand,
                        CostPerUnit = EditingCostPerUnit,
                        ReOrderLevel = EditingReOrderLevel,
                        IsActive = EditingIsActive
                    };

                    await _inventoryService.UpdateInventoryItemAsync(item);

                    // Create transaction ONLY if quantity changed
                    if (IsQuantityChanged)
                    {
                        var inventoryTransaction = new InventoryTransaction
                        {
                            InventoryItemId = item.Id,
                            QuantityChange = QuantityChanged,
                            CostAtTransaction = EditingCostPerUnit,
                            Reason = SelectedReason,
                            Remarks = Remarks,
                            TransactionDate = DateTime.Now,
                        };

                        await _inventoryTransactionService.AddInventoryTransactionAsync(inventoryTransaction);
                    }
                }

                await PageHelper.DisplayAlertAsync("Success", "Inventory item saved successfully.", "OK");
                await LoadInventoryItems();
                Cancel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveChanges: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "Failed to save inventory item. Please try again.", "OK");
            }
        }

        // ======================================================
        // LOAD INVENTORY ITEMS
        // ======================================================
        public async Task LoadInventoryItems(string filter = "")
        {
            try
            {
                var items = await _inventoryService.GetAllInventoryItemsAsync();

                var filtered = string.IsNullOrWhiteSpace(filter)
                    ? items
                    : items.Where(i => i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    InventoryItems.Clear();
                    foreach (var item in filtered)
                        InventoryItems.Add(item);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading inventory items: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "Failed to load inventory items.", "OK");
            }
        }

        // ======================================================
        // SEARCH WITH DEBOUNCE
        // ======================================================
        partial void OnSearchTextChanged(string value)
        {
            try
            {
                _searchCts?.Cancel();
                _searchCts = new CancellationTokenSource();
                var token = _searchCts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500, token);
                        await LoadInventoryItems(value);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in debounced search: {ex.Message}");
                    }
                }, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSearchTextChanged: {ex.Message}");
            }
        }

        // ======================================================
        // 🔥 QUANTITY CHANGE CALCULATION (REAL-TIME)
        // ======================================================
        partial void OnEditingQuantityOnHandChanged(decimal newValue)
        {
            try
            {
                var difference = newValue - OriginalQuantityOnHand;

                // Treat unchanged values as zero
                if (Math.Abs(difference) < 0.0001m)
                {
                    QuantityChanged = 0;
                    DisplayQuantityChanged = "0";
                }
                else
                {
                    QuantityChanged = difference;
                    DisplayQuantityChanged = difference > 0
                        ? $"+{difference:N0}"
                        : difference.ToString("N0");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnEditingQuantityOnHandChanged: {ex.Message}");
            }
        }

        //=====================================================
        // INITIALIZE QUANTITY CHANGE DISPLAY
        //=====================================================
        partial void OnEditingUnitChanged(string value)
        {
            try
            {
                this.CostPerUnitLabel = $"Cost Per {value} (₱)";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnEditingUnitChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes quantity change display when entering edit/add mode.
        /// Prevents showing incorrect values before user input.
        /// </summary>
        private void InitializeQuantityChange()
        {
            try
            {
                var difference = EditingQuantityOnHand - OriginalQuantityOnHand;

                if (Math.Abs(difference) < 0.0001m)
                {
                    QuantityChanged = 0;
                    DisplayQuantityChanged = "0";
                }
                else
                {
                    QuantityChanged = difference;
                    DisplayQuantityChanged = difference > 0
                        ? $"+{difference:N0}"
                        : difference.ToString("N0");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeQuantityChange: {ex.Message}");
            }
        }

        // ======================================================
        // NAVIGATION
        // ======================================================
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

        //======================================================
        // VIEW TRANSACTION PANEL
        //======================================================
        [RelayCommand]
        public async Task ViewTransactionsAsync(InventoryItem item)
        {
            try
            {
                if (item == null) return;

                // Hide edit panel, show transaction panel
                IsEditPanelVisible = false;
                IsTransactionPanelVisible = true;
                TransactionLogPanelTitle = $"Transaction Logs: {item.Name}";

                var transactions = await _inventoryTransactionService
                    .GetTransactionsByInventoryItemAsync(item.Id);

                this.TransactionLogs = new ObservableCollection<InventoryTransaction>(transactions);
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error viewing transactions: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", "Failed to load transaction logs.", "OK");
            }
        }
        [RelayCommand]
        private void CloseTransactionPanel()
        {
            try
            {
                IsTransactionPanelVisible = false;
                TransactionLogs.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing transaction panel: {ex.Message}");
            }
        }

    }
}