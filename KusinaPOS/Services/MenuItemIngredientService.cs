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
            await _db.CreateTableAsync<MenuItemIngredient>();
        }
        // =====================
        // Create
        // =====================
        public async Task<int> AddAsync(MenuItemIngredient item)
        {
            await InitializeAsync();
            return await _db.InsertAsync(item);
        }

        // =====================
        // Update
        // =====================
        public async Task UpdateAsync(MenuItemIngredient item)
        {
            await InitializeAsync();
            await _db.UpdateAsync(item);
        }
        public async Task UpdateQuantityAsync(int menuItemId, int inventoryItemId, decimal quantityPerMenu)
        {
            // Ensure table exists
            await InitializeAsync();

            // Find the specific MenuItemIngredient in DB
            var ingredient = await _db.Table<MenuItemIngredient>()
                                      .FirstOrDefaultAsync(i => i.MenuItemId == menuItemId &&
                                                               i.InventoryItemId == inventoryItemId);

            if (ingredient == null)
                return; // Nothing to update

            // Update quantity
            ingredient.QuantityPerMenu = quantityPerMenu;

            // Save change to DB
            await _db.UpdateAsync(ingredient);
        }



        // =====================
        // Delete
        // =====================
        public async Task<int> DeleteAsync(MenuItemIngredient item)
        {
            await InitializeAsync();
            return await _db.DeleteAsync(item);
        }

        // =====================
        // Queries
        // =====================
        public Task<MenuItemIngredient?> GetByIdAsync(int id)
        {
            return _db.Table<MenuItemIngredient>()
                      .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<MenuItemIngredient>> GetByMenuItemIdAsync(int menuItemId)
        {
            await InitializeAsync();
            // Get all ingredients for this menu item
            var ingredients = await _db.Table<MenuItemIngredient>()
                                       .Where(x => x.MenuItemId == menuItemId)
                                       .ToListAsync();

            // Optional: if you want to refresh names from InventoryItem table
            foreach (var ing in ingredients)
            {
                var inv = await _db.Table<InventoryItem>()
                                   .FirstOrDefaultAsync(i => i.Id == ing.InventoryItemId);
                if (inv != null)
                {
                    ing.InventoryItemName = inv.Name;
                    ing.UnitOfMeasurement = inv.Unit;
                }
            }

            return ingredients;
        }


        public async Task<List<MenuItemIngredient>> GetByInventoryItemIdAsync(int inventoryItemId)
        {
            await InitializeAsync();
            var ingredient= _db.Table<MenuItemIngredient>()
                      .Where(x => x.InventoryItemId == inventoryItemId)
                      .ToListAsync();
            return await ingredient;
        }
    }
}
