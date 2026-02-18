using KusinaPOS.Models;
using SQLite;
using System.Diagnostics;

namespace KusinaPOS.Services
{
    public class InventoryItemService
    {
        private readonly SQLiteAsyncConnection _db;

        public InventoryItemService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<InventoryItem>();
        }

        // CREATE
        public async Task AddInventoryItemAsync(InventoryItem item)
        {
            await InitializeAsync();
            await _db.InsertAsync(item);
        }

        // READ
        public async Task<List<InventoryItem>> GetAllInventoryItemsAsync()
        {
            await InitializeAsync();
            return await _db
                .Table<InventoryItem>()
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
        public async Task<List<InventoryItem>> GetFilteredInventoryItemsAsync(string filter)
        {
            await InitializeAsync();
            if (string.IsNullOrWhiteSpace(filter))
                return await GetAllInventoryItemsAsync();

            return await _db.Table<InventoryItem>()
                .Where(i => i.Name.ToLower().Contains(filter.ToLower()))
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
        public async Task<InventoryItem?> GetInventoryItemByIdAsync(int id)
        {
            await InitializeAsync();
            return await _db
                .Table<InventoryItem>()
                .FirstOrDefaultAsync(i => i.Id == id);
        }
        public async Task<bool> GetInventoryStatusById(int id)
        {
            await InitializeAsync();
            var item = await GetInventoryItemByIdAsync(id);
            return item != null && item.IsActive;
        }
        // UPDATE
        public async Task UpdateInventoryItemAsync(InventoryItem item)
        {
            await InitializeAsync();
            await _db.UpdateAsync(item);
        }

        // SOFT DELETE
        public async Task DeactivateInventoryItemAsync(int id)
        {
            await InitializeAsync();
            var item = await GetInventoryItemByIdAsync(id);
            if (item != null)
            {
                item.IsActive = false;
                await _db.UpdateAsync(item);
            }
        }

        // STOCK UPDATE
        public async Task UpdateQuantityAsync(int id, decimal newQuantity)
        {
            await InitializeAsync();
            var item = await GetInventoryItemByIdAsync(id);
            if (item != null)
            {
                item.QuantityOnHand = newQuantity;
                await _db.UpdateAsync(item);
            }
        }
        // In InventoryItemService.cs

        public async Task ApplyBulkInventoryChangesAsync(List<InventoryItem> modifiedItems, string reason = "Stock In", string remarks = "Bulk update")
        {
            try
            {
                await InitializeAsync();

                // Run in a single transaction for speed and data safety
                await _db.RunInTransactionAsync(tran =>
                {
                    foreach (var updatedItem in modifiedItems)
                    {
                        // 1. Fetch the original item using its Primary Key (No FirstOrDefault needed!)
                        var originalItem = tran.Find<InventoryItem>(updatedItem.Id);

                        if (originalItem != null)
                        {
                            // 2. Calculate the difference (New minus Old)
                            decimal qtyDifference = updatedItem.QuantityOnHand - originalItem.QuantityOnHand;

                            // 3. If the quantity actually changed, record the transaction
                            if (qtyDifference != 0)
                            {
                                var transaction = new InventoryTransaction
                                {
                                    InventoryItemId = updatedItem.Id,
                                    QuantityChange = qtyDifference,
                                    CostAtTransaction = updatedItem.CostPerUnit,
                                    Reason = reason,
                                    Remarks = remarks,
                                    TransactionDate = DateTime.Now
                                };

                                tran.Insert(transaction);
                            }

                            // 4. Update the actual Inventory Item (Qty, Cost, and Reorder Level)
                            tran.Update(updatedItem);
                        }
                    }
                });
            }
            catch (Exception ex)
            {

                System.Console.WriteLine($"Error:ApplyBulkInventoryChangesAsync -- {ex.Message}");
            }
        }
    }
}
