using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using KusinaPOS.Views;
using System.Collections.ObjectModel;

namespace KusinaPOS.ViewModel
{
    public partial class InventoryItemViewModel : ObservableObject
    {
        private readonly InventoryItemService _inventoryService;
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
        [ObservableProperty] private string selectedReason = string.Empty;
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
        [ObservableProperty] private string quantityChanged = "0";

        // =============================
        // CONSTRUCTOR
        // =============================
        public InventoryItemViewModel(
            InventoryItemService inventoryItemService,
            IDateTimeService dateTimeService)
        {
            _inventoryService = inventoryItemService;
            _dateTimeService = dateTimeService;

            _ = LoadInventoryItems();

            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

            Reasons = new() { "Void", "Stock In", "Stock Out", "Adjustment" };

            _dateTimeService.DateTimeChanged += (_, dt) => CurrentDateTime = dt;
            CurrentDateTime = _dateTimeService.CurrentDateTime;
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
            QuantityChanged = "0";

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
            QuantityChanged = "0";

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

            QuantityChanged = "0";
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

                await _inventoryService.UpdateInventoryItemAsync(item);
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

            QuantityChanged = difference switch
            {
                0 => "0",
                > 0 => $"+{difference}",
                _ => difference.ToString()
            };
        }
    }
}
