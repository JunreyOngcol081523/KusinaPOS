using KusinaPOS.Models;
using SQLite;

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
    }
}
