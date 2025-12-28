using SQLite;
using KusinaPOS.Models;

namespace KusinaPOS.Services
{
    public class MenuItemIngredientService
    {
        private readonly SQLiteAsyncConnection _db;

        public MenuItemIngredientService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();

        }
        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<InventoryItem>();
        }
        // =====================
        // Create
        // =====================
        public async Task AddAsync(MenuItemIngredient item)
        {
            await InitializeAsync();
            await _db.InsertAsync(item);
        }

        // =====================
        // Update
        // =====================
        public async Task UpdateAsync(MenuItemIngredient item)
        {
            await InitializeAsync();
            await _db.UpdateAsync(item);
        }

        // =====================
        // Delete
        // =====================
        public async Task DeleteAsync(int id)
        {
            await InitializeAsync();
            await _db.DeleteAsync(id);
        }

        // =====================
        // Queries
        // =====================
        public Task<MenuItemIngredient?> GetByIdAsync(int id)
        {
            return _db.Table<MenuItemIngredient>()
                      .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<List<MenuItemIngredient>> GetByMenuItemIdAsync(int menuItemId)
        {
            return _db.Table<MenuItemIngredient>()
                      .Where(x => x.MenuItemId == menuItemId)
                      .ToListAsync();
        }

        public Task<List<MenuItemIngredient>> GetByInventoryItemIdAsync(int inventoryItemId)
        {
            return _db.Table<MenuItemIngredient>()
                      .Where(x => x.InventoryItemId == inventoryItemId)
                      .ToListAsync();
        }
    }
}
