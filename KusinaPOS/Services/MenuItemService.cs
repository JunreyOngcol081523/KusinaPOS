using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using KusinaPOS.Models;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.Services
{
    public class MenuItemService
    {
        private readonly SQLiteAsyncConnection _db;

        //constructor
        public MenuItemService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<Models.MenuItem>();
        }

        public async Task<List<Models.MenuItem>> GetAllMenuItemsAsync()
        {
            await InitializeAsync();
            return await _db.Table<Models.MenuItem>().ToListAsync();
        }

        public async Task<List<Models.MenuItem>> GetMenuItemsByCategoryAsync(string categoryName)
        {
            await InitializeAsync();
            return await _db.Table<Models.MenuItem>()
                .Where(m => m.Category == categoryName)
                .ToListAsync();
        }

        public async Task AddMenuItemAsync(Models.MenuItem menuItem)
        {
            await InitializeAsync();
            await _db.InsertAsync(menuItem);
        }

        public async Task UpdateMenuItemAsync(Models.MenuItem menuItem)
        {
            await InitializeAsync();
            await _db.UpdateAsync(menuItem);
        }

        public async Task DeleteMenuItemAsync(int id)
        {
            await InitializeAsync();
            await _db.DeleteAsync<Models.MenuItem>(id);
        }
        public async Task AddAllMenuItemAsync(List<Models.MenuItem> menuItems)
        {
            await InitializeAsync();
            await _db.InsertAllAsync(menuItems);
        }
        //delete table
        public async Task DeleteAllMenuItemsAsync()
        {
            await InitializeAsync();
            await _db.DeleteAllAsync<Models.MenuItem>();
        }
        public async Task<List<MenuItem>> GetMenuItemsByCategoryPagedAsync(
            string category,
            int pageIndex,
            int pageSize)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<MenuItem>();

            return await _db.Table<MenuItem>()
                .Where(x => x.Category == category)
                .OrderBy(x => x.Name)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}