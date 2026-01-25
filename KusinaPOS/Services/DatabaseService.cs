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
            await _database.ExecuteAsync(@"
            CREATE VIEW IF NOT EXISTS View_SaleItemsWithMenuName AS
            SELECT
                si.Id,
                si.SaleId,
                si.MenuItemId,
                mi.Name AS MenuItemName,
                si.Quantity,
                si.UnitPrice,
                (si.Quantity * si.UnitPrice) AS LineTotal
            FROM SaleItem si
            INNER JOIN MenuItem mi ON mi.Id = si.MenuItemId;
        ");
            await _database.ExecuteAsync(@"
            CREATE VIEW IF NOT EXISTS vwSaleItemsWithDateMenuItem AS
            SELECT
                si.Id AS SaleItemId,
                si.SaleId,
                s.ReceiptNo,
                s.SaleDate,
                si.MenuItemId,
                m.Name AS MenuItemName,
                m.Category,
                m.ImagePath,
                si.Quantity,
                si.UnitPrice,
                (si.Quantity * si.UnitPrice) AS LineTotal
            FROM SaleItem si
            INNER JOIN Sale s
                ON si.SaleId = s.Id
            INNER JOIN MenuItem m
                ON si.MenuItemId = m.Id;

            ");
        }
    }

}
