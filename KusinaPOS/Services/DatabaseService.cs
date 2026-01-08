using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Services
{
    using KusinaPOS.Models;
    using SQLite;
    public interface IDatabaseService
    {
        Task InitializeAsync();
        SQLiteAsyncConnection GetConnection();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService(string dbPath)
        {
            _database = new SQLiteAsyncConnection(
                dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache
            );
        }

        public SQLiteAsyncConnection GetConnection()
        {
            return _database;
        }

        public async Task InitializeAsync()
        {
            // Order matters slightly for clarity, not technically required
            //await _database.DropTableAsync<Sale>();
            await _database.CreateTableAsync<MenuItem>();
            await _database.CreateTableAsync<InventoryItem>();
            await _database.CreateTableAsync<MenuItemIngredient>();
            await _database.CreateTableAsync<Sale>();
            await _database.CreateTableAsync<SaleItem>();
            await _database.CreateTableAsync<InventoryTransaction>();
            await _database.CreateTableAsync<Category>();
        }
    }

}
