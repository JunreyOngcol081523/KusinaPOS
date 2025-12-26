using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using KusinaPOS.Views;
using Syncfusion.Maui.Core.Internals;
using System.Collections.ObjectModel;

namespace KusinaPOS.ViewModel
{
    public partial class InventoryItemViewModel : ObservableObject
    {

        // Observable Properties
        [ObservableProperty]
        private ObservableCollection<InventoryItem> inventoryItems = new();

        [ObservableProperty]
        private InventoryItem? selectedItem;

        // Editing Item - Individual properties for two-way binding
        [ObservableProperty]
        private int editingId;

        [ObservableProperty]
        private string editingName = string.Empty;

        [ObservableProperty]
        private string editingUnit = string.Empty;

        [ObservableProperty]
        private decimal editingQuantityOnHand;

        [ObservableProperty]
        private decimal editingCostPerUnit;

        [ObservableProperty]
        private decimal editingReOrderLevel;

        [ObservableProperty]
        private bool editingIsActive = true;

        [ObservableProperty]
        private bool isEditPanelVisible = false;

        [ObservableProperty]
        private string editPanelTitle = "Edit Inventory Item";

        [ObservableProperty]
        private string searchText = string.Empty;

        private CancellationTokenSource? _searchCts;
        public ObservableCollection<InventoryItem> FilteredItems { get; } = new();
        private InventoryItemService _inventoryService;

        [ObservableProperty]
        private string _loggedInUserName = string.Empty;

        private readonly IDateTimeService _dateTimeService;
        [ObservableProperty]
        private string _currentDateTime;
        [ObservableProperty]
        private string storeName;
        // Constructor
        public InventoryItemViewModel(InventoryItemService inventoryItemService, IDateTimeService dateTimeService)
        {
            // Initialize
            _inventoryService = inventoryItemService;
            _=LoadInventoryItems();
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey,string.Empty);
            _dateTimeService = dateTimeService;
            _dateTimeService.DateTimeChanged += OnDateTimeChanged;
            CurrentDateTime = _dateTimeService.CurrentDateTime;
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

        }
        private void OnDateTimeChanged(object? sender, string dateTime)
        {
            CurrentDateTime = dateTime;
        }
        // Commands
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

            EditPanelTitle = "Add Inventory Item";
            IsEditPanelVisible = true;
        }

        [RelayCommand]
        private void EditItem(InventoryItem item)
        {
            if (item == null) return;

            // Load item properties into editing fields
            EditingId = item.Id;
            EditingName = item.Name;
            EditingUnit = item.Unit;
            EditingQuantityOnHand = item.QuantityOnHand;
            EditingCostPerUnit = item.CostPerUnit;
            EditingReOrderLevel = item.ReOrderLevel;
            EditingIsActive = item.IsActive;

            EditPanelTitle = $"Edit Inventory Item: {item.Name}";
            IsEditPanelVisible = true;
        }

        [RelayCommand]
        private void SelectItem(InventoryItem item)
        {
            if (item == null) return;
            SelectedItem = item;
            EditItem(item);
        }

        [RelayCommand]
        private async Task DeleteItem(InventoryItem item)
        {
            if (item == null) return;


            bool confirm = await PageHelper.DisplayConfirmAsync(
                "Delete Item",
                $"Are you sure you want to delete {item.Name}?",
                "Yes", "No");
            if (confirm)
            {
                InventoryItems.Remove(item);

                // TODO: Delete from database
                await _inventoryService.DeactivateInventoryItemAsync(item.Id);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            IsEditPanelVisible = false;
            EditingId = 0;
            EditingName = string.Empty;
            EditingUnit = string.Empty;
            EditingQuantityOnHand = 0;
            EditingCostPerUnit = 0;
            EditingReOrderLevel = 0;
            EditingIsActive = true;
            SelectedItem = null;
        }

        [RelayCommand]
        private async Task SaveChanges()
        {
            // Validate
            if (string.IsNullOrWhiteSpace(EditingName))
            {
                
                await PageHelper.DisplayAlertAsync("Error", "Name is required", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingUnit))
            {
                await PageHelper.DisplayAlertAsync("Error", "Unit is required", "OK");
                
                return;
            }

            if (EditingQuantityOnHand < 0)
            {
                await PageHelper.DisplayAlertAsync("Error", "Quantity cannot be negative", "OK");
                //await App.Current.MainPage.DisplayAlert("Error", "Quantity cannot be negative", "OK");
                return;
            }

            if (EditingCostPerUnit < 0)
            {
                await PageHelper.DisplayAlertAsync("Error", "Cost cannot be negative", "OK");
                //await App.Current.MainPage.DisplayAlert("Error", "Cost cannot be negative", "OK");
                return;
            }

            try
            {
                if (EditingId == 0)
                {
                    // Adding new item
                    var newItem = new InventoryItem
                    {
                        Name = EditingName,
                        Unit = EditingUnit,
                        QuantityOnHand = EditingQuantityOnHand,
                        CostPerUnit = EditingCostPerUnit,
                        ReOrderLevel = EditingReOrderLevel,
                        IsActive = EditingIsActive
                    };

                    // Save to database first to get generated ID
                    await _inventoryService.AddInventoryItemAsync(newItem);

                    // Add to collection after getting ID from database
                    InventoryItems.Add(newItem);
                }
                else
                {
                    // Updating existing item - replace the entire item in the collection
                    var existingItem = InventoryItems.FirstOrDefault(i => i.Id == EditingId);
                    if (existingItem != null)
                    {
                        var updatedItem = new InventoryItem
                        {
                            Id = EditingId,
                            Name = EditingName,
                            Unit = EditingUnit,
                            QuantityOnHand = EditingQuantityOnHand,
                            CostPerUnit = EditingCostPerUnit,
                            ReOrderLevel = EditingReOrderLevel,
                            IsActive = EditingIsActive
                        };

                        var index = InventoryItems.IndexOf(existingItem);
                        InventoryItems[index] = updatedItem;

                        // Update in database
                        await _inventoryService.UpdateInventoryItemAsync(updatedItem);
                    }
                }

                // Close panel
                Cancel();
            }
            catch (Exception ex)
            {

                await PageHelper.DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
            }
            finally { 
                await LoadInventoryItems();
            }
        }

        // Optional: Load data method
        public async Task LoadInventoryItems(string filter = "")
        {
            var items = await _inventoryService.GetAllInventoryItemsAsync();

            // Perform filtering logic
            var filtered = string.IsNullOrWhiteSpace(filter)
                ? items
                : items.Where(i => i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            // Update the existing collection on the UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                InventoryItems.Clear();
                foreach (var item in filtered)
                {
                    InventoryItems.Add(item);
                }
            });
        }
        [RelayCommand]
        public async Task RefreshInventoryAsync() { 
            await LoadInventoryItems();
        }
        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync(nameof(DashboardPage));

        }
        //search functionality with debounce
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
                    await LoadInventoryItems(value); // Pass the search text here
                }
                catch (OperationCanceledException) { }
            }, token);
        }

    }
}