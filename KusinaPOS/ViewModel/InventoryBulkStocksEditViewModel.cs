using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace KusinaPOS.ViewModel
{
    public partial class InventoryItemSelection : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private InventoryItem _item;
        public decimal OriginalQuantity { get; private set; }
        public InventoryItemSelection(InventoryItem item)
        {
            Item = item;
            OriginalQuantity = item.QuantityOnHand;
            IsSelected = false;
        }
    }
    public partial class InventoryBulkStocksEditViewModel:ObservableObject
    {
        private CancellationTokenSource? _searchCts;
        private readonly InventoryItemService _inventoryService;
        // For the SearchBar Text="{Binding SearchItemText}"
        [ObservableProperty]
        private string _searchItemText = string.Empty;

        // For the DataGrid ItemsSource="{Binding InventorySelectionList}"
        [ObservableProperty]
        private ObservableCollection<InventoryItemSelection> _inventorySelectionList = new();

        // A master list to keep track of items during filtering
        private List<InventoryItemSelection> _allInventoryItems = new();

        //constructor
        public InventoryBulkStocksEditViewModel(InventoryItemService inventoryService)
        {
            _inventoryService = inventoryService;
            // Load inventory items when the ViewModel is instantiated
            MainThread.BeginInvokeOnMainThread(async () => await LoadInventoryAsync());
        }
        partial void OnSearchItemTextChanged(string value)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, token);

                    // Filter against your master list
                    var filtered = string.IsNullOrWhiteSpace(value)
                        ? _allInventoryItems
                        : _allInventoryItems
                            .Where(x => x.Item.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                    // 2. Update the EXISTING collection on the Main Thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // To prevent the "Collection was modified" error during the refresh,
                        // we clear and repopulate the existing instance.

                        // Optional: Tell the DataGrid to pause updates if using a lot of data
                        // but usually, Clear/Add is enough to stay stable.
                        InventorySelectionList.Clear();

                        foreach (var item in filtered)
                        {
                            InventorySelectionList.Add(item);
                        }
                    });
                }
                catch (OperationCanceledException) { }
            }, token);
        }
        public async Task LoadInventoryAsync()
        {
            try
            {
                // 1. Fetch raw items from your service
                var items = await _inventoryService.GetAllInventoryItemsAsync();

                // 2. Map them to your Selection Wrapper
                var selectionItems = items.Select(item => new InventoryItemSelection(item)).ToList();

                // 3. Store in the master list for searching
                _allInventoryItems = selectionItems;

                // 4. Update the UI collection
                InventorySelectionList = new ObservableCollection<InventoryItemSelection>(selectionItems);
            }
            catch (Exception ex)
            {
                // Handle error (e.g., Log or DisplayAlert)
                await PageHelper.DisplayAlertAsync("Error", $"Failed to load inventory: {ex.Message}", "OK");
            }
        }
        [RelayCommand]
        private async Task ClearSelectionsAsync()
        {
            foreach (var selection in InventorySelectionList)
            {
                selection.IsSelected = false;
            }
        }
        // In InventoryViewModel.cs

        [RelayCommand]
        private async Task SaveBulkChangesAsync()
        {
            // 1. Filter out only the checked wrapper items so we can access OriginalQuantity
            var selectedWrappers = _allInventoryItems
                .Where(x => x.IsSelected)
                .ToList();

            if (!selectedWrappers.Any())
            {
                // If they click save without checking any boxes
                await PageHelper.DisplayAlertAsync("Notice", "Please select at least one item to save.", "OK");
                return;
            }

            // 🟢 2. VALIDATION CHECK: Ensure new qty >= original qty 🟢
            var invalidItems = selectedWrappers
                .Where(x => x.Item.QuantityOnHand < x.OriginalQuantity)
                .ToList();

            if (invalidItems.Any())
            {
                // Extract the names of the invalid items to tell the user exactly what to fix
                string errorNames = string.Join(", ", invalidItems.Select(x => x.Item.Name));

                await PageHelper.DisplayAlertAsync(
                    "Invalid Stock Entry",
                    $"This panel is for Stocks In. The new quantity cannot be lower than the original quantity.\n\nPlease check: {errorNames}",
                    "OK");

                return; // Abort the save process completely
            }

            try
            {
                // Optional: Show a loading indicator if you have an IsBusy property
                // IsBusy = true; 

                // 3. Extract just the models to pass to the service
                var itemsToSave = selectedWrappers.Select(x => x.Item).ToList();

                // 4. Call the service method
                await _inventoryService.ApplyBulkInventoryChangesAsync(
                    itemsToSave,
                    reason: "Stock In",
                    remarks: "Updated via Bulk Stocks In panel");

                await PageHelper.DisplayAlertAsync("Success", $"{itemsToSave.Count} items have been updated.", "OK");

                // 5. Reset the search text and reload fresh data from the database
                SearchItemText = string.Empty;
                
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Save Error", $"An error occurred: {ex.Message}", "OK");
            }
            
        }
    }
}
