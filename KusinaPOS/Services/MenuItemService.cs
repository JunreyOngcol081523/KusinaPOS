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
            await InitializeAsync();
            if (string.IsNullOrWhiteSpace(category))
                return new List<MenuItem>();

            return await _db.Table<MenuItem>()
                .Where(x => x.Category == category)
                .OrderBy(x => x.Name)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public List<MenuItem> GenerateMenuItems(List<Category> categories)
        {
            var items = new List<MenuItem>();
            var random = new Random();

            var categoryTemplates = new Dictionary<string, (string prefix, string type, decimal minPrice, decimal maxPrice)>
            {
                ["Appetizers"] = ("Appetizer", "Recipe-Based", 120, 280),
                ["Main Courses"] = ("Main Dish", "Recipe-Based", 250, 650),
                ["Beer Products"] = ("Beer", "Unit-Based", 80, 180),
                ["Hard Drinks"] = ("Hard Drink", "Unit-Based", 150, 450),
                ["Pulutan"] = ("Pulutan", "Recipe-Based", 90, 250),
                ["Soup"] = ("Soup", "Recipe-Based", 120, 320),
                ["Pasta"] = ("Pasta", "Recipe-Based", 220, 520),
                ["Snacks"] = ("Snack", "Recipe-Based", 60, 180),
                ["Beverages"] = ("Beverage", "Unit-Based", 50, 160)
            };

            foreach (var category in categories)
            {
                if (!categoryTemplates.ContainsKey(category.Name))
                    continue;

                var template = categoryTemplates[category.Name];

                for (int i = 1; i <= 50; i++)
                {
                    var price = Math.Round(
                        (decimal)(random.NextDouble() *
                        (double)(template.maxPrice - template.minPrice))
                        + template.minPrice, 2);

                    items.Add(new MenuItem
                    {
                        Name = $"{template.prefix} {i}",
                        Description = $"Sample {template.prefix.ToLower()} item {i}",
                        Price = price,
                        Category = category.Name,
                        Type = template.type,
                        IsActive = true
                    });
                }
            }

            return items;
        }

    }
}