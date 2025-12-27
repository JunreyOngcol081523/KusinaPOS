using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using KusinaPOS.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KusinaPOS.ViewModel
{
    public partial class InventoryItemViewModel : ObservableObject
    {
        private readonly InventoryItemService _inventoryService;
        private readonly InventoryTransactionService _inventoryTransactionService;
        private readonly IDateTimeService _dateTimeService;

        private CancellationTokenSource? _searchCts;
        [ObservableProperty] private decimal _originalQuantityOnHand;

        // =============================
        // COLLECTIONS
        // =============================
        [ObservableProperty]
        private ObservableCollection<InventoryItem> inventoryItems = new();

        [ObservableProperty]
        private InventoryItem? selectedItem;

        // =============================
        // EDITING FIELDS
        // =============================
        [ObservableProperty] private int editingId;
        [ObservableProperty] private string editingName = string.Empty;
        [ObservableProperty] private string editingUnit = string.Empty;
        [ObservableProperty] private decimal editingQuantityOnHand;
        [ObservableProperty] private decimal editingCostPerUnit;
        [ObservableProperty] private decimal editingReOrderLevel;
        [ObservableProperty] private bool editingIsActive = true;
        [ObservableProperty] private string remarks = string.Empty;
        // =============================
        // UI STATE
        // =============================
        [ObservableProperty] private bool isEditPanelVisible;
        [ObservableProperty] private string editPanelTitle = "Edit Inventory Item";
        [ObservableProperty] private string searchText = string.Empty;

        // =============================
        // HEADER INFO
        // =============================
        [ObservableProperty] private string loggedInUserName = string.Empty;
        [ObservableProperty] private string currentDateTime;
        [ObservableProperty] private string storeName;

        // =============================
        // STOCK ADJUSTMENT
        // =============================
        [ObservableProperty] private List<string> reasons;
        [ObservableProperty] private string selectedReason = string.Empty;
        [ObservableProperty] private decimal quantityChanged;
        [ObservableProperty] private string displayQuantityChanged = "0";

        // =============================
        // CONSTRUCTOR
        // =============================
        public InventoryItemViewModel(
            InventoryItemService inventoryItemService,
            IDateTimeService dateTimeService,
            InventoryTransactionService inventoryTransactionService)
        {
            _inventoryService = inventoryItemService;
            _dateTimeService = dateTimeService;

            _ = LoadInventoryItems();

            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

            Reasons = new() { "Void", "Stock In", "Stock Out", "Adjustment" };

            _dateTimeService.DateTimeChanged += (_, dt) => CurrentDateTime = dt;
            CurrentDateTime = _dateTimeService.CurrentDateTime;
            _inventoryTransactionService = inventoryTransactionService;
            InitializeQuantityChange();
        }

        // =============================
        // ADD ITEM
        // =============================
        [RelayCommand]
        private void AddItem()
        {
            EditingId = 0;
            EditingName = string.Empty;
            EditingUnit = string.Empty;
            EditingQuantityOnHand = 0;
            EditingCostPerUnit = 0;
            EditingReOrderLevel = 0;
            EditingIsActive = true;

            OriginalQuantityOnHand = 0;
            QuantityChanged = 0;

            // Initialize DisplayQuantityChanged here
            InitializeQuantityChange();

            EditPanelTitle = "Add Inventory Item";
            IsEditPanelVisible = true;
        }


        // =============================
        // EDIT ITEM
        // =============================
        [RelayCommand]
        private void EditItem(InventoryItem item)
        {
            if (item == null) return;

            EditingId = item.Id;
            EditingName = item.Name;
            EditingUnit = item.Unit;
            EditingQuantityOnHand = item.QuantityOnHand;
            EditingCostPerUnit = item.CostPerUnit;
            EditingReOrderLevel = item.ReOrderLevel;
            EditingIsActive = item.IsActive;

            OriginalQuantityOnHand = item.QuantityOnHand;
            QuantityChanged = 0;

            // Initialize DisplayQuantityChanged here
            InitializeQuantityChange();

            EditPanelTitle = $"Edit Inventory Item: {item.Name}";
            IsEditPanelVisible = true;
        }


        // =============================
        // CANCEL
        // =============================
        [RelayCommand]
        private void Cancel()
        {
            IsEditPanelVisible = false;
            SelectedItem = null;

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

        // =============================
        // SAVE
        // =============================
        [RelayCommand]
        private async Task SaveChanges()
        {
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
            if (String.IsNullOrEmpty(SelectedReason)){ 
                await PageHelper.DisplayAlertAsync("Error", "Please select a reason for the adjustment", "OK");
                return;
            }

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
            }
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
                var inventoryTransaction = new InventoryTransaction
                {
                    InventoryItemId = item.Id,
                    QuantityChange = this.QuantityChanged,
                    Reason = this.SelectedReason,
                    Remarks = this.Remarks,
                    TransactionDate = DateTime.Now,
                };
                await _inventoryService.UpdateInventoryItemAsync(item);
                await _inventoryTransactionService.AddInventoryTransactionAsync(inventoryTransaction);
            }

            await LoadInventoryItems();
            Cancel();
        }

        // =============================
        // LOAD ITEMS
        // =============================
        public async Task LoadInventoryItems(string filter = "")
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

        // =============================
        // SEARCH (DEBOUNCE)
        // =============================
        partial void OnSearchTextChanged(string value)
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
            }, token);
        }

        // =============================
        // 🔥 QUANTITY CHANGE LOGIC
        // =============================
        partial void OnEditingQuantityOnHandChanged(decimal newValue)
        {
            var difference = newValue - OriginalQuantityOnHand;

            // Avoid floating-point issues
            if (Math.Abs(difference) < 0.0001m)
            {
                QuantityChanged = 0;
                DisplayQuantityChanged = "0";
            }
            else
            {
                QuantityChanged = difference;
                DisplayQuantityChanged = difference > 0
                    ? $"+{difference:N0}" // N0 formats with commas, no decimals
                    : difference.ToString("N0"); // negative numbers with commas
            }
        }
        // Call this after loading the item into the UI
        private void InitializeQuantityChange()
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
        //================================/
        // VIEW TRANSACTIONS LOGS PER ITEM//
        //================================//
        [RelayCommand]
        public async Task ViewTransactionsAsync(InventoryItem item)
        {
            if (item == null) return;

            var transactions = await _inventoryTransactionService.GetTransactionsByInventoryItemAsync(item.Id);

            // Option 1: Console.WriteLine
            foreach (var t in transactions)
            {
                Console.WriteLine($"Id: {t.Id}, InventoryItemId: {t.InventoryItemId}, QuantityChange: {t.QuantityChange}, Reason: {t.Reason}, Date: {t.TransactionDate}");
            }

            // Option 2: Debug.WriteLine (preferred for MAUI)
            foreach (var t in transactions)
            {
                Debug.WriteLine($"Id: {t.Id}, InventoryItemId: {t.InventoryItemId}, QuantityChange: {t.QuantityChange}, Reason: {t.Reason}, Date: {t.TransactionDate}");
                await PageHelper.DisplayAlertAsync("Transaction Log",
                    $"Id: {t.Id}\nInventoryItemId: {t.InventoryItemId}\nQuantityChange: {t.QuantityChange}\nReason: {t.Reason}\nDate: {t.TransactionDate}",
                    "OK");
            }
        }
        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");

        }
    }
}
