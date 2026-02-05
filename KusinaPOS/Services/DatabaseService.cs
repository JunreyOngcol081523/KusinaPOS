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
        //Task ResetDatabaseAsync();
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
        //drop all tables and recreate
        public async Task ResetDatabaseAsync()
        {
            // Drop all tables
            await _database.DropTableAsync<SaleItem>();
            await _database.DropTableAsync<Sale>();
            await _database.DropTableAsync<MenuItemIngredient>();
            await _database.DropTableAsync<InventoryItem>();
            await _database.DropTableAsync<MenuItem>();
            await _database.DropTableAsync<InventoryTransaction>();
            await _database.DropTableAsync<Category>();
            // Recreate all tables
            await InitializeAsync();
        }
        public async Task InitializeAsync()
        {

            await _database.CreateTableAsync<MenuItem>();
            await _database.CreateTableAsync<InventoryItem>();
            await _database.CreateTableAsync<MenuItemIngredient>();
            await _database.CreateTableAsync<Sale>();
            await _database.CreateTableAsync<SaleItem>();
            await _database.CreateTableAsync<InventoryTransaction>();
            await _database.CreateTableAsync<Category>();
            // =========================================================
            // VIEW 1: Basic Item Details
            // =========================================================
            // 1. Drop old to ensure clean recreation
            await _database.ExecuteAsync("DROP VIEW IF EXISTS View_SaleItemsWithMenuName");

            // 2. Create without status filter
            await _database.ExecuteAsync(@"
                    CREATE VIEW View_SaleItemsWithMenuName AS
                    SELECT 
                        si.Id,
                        si.SaleId, 
                        si.MenuItemId, 
                        mi.Name AS MenuItemName, 
                        si.Quantity, 
                        si.UnitPrice, 
                        (si.Quantity * si.UnitPrice) AS LineTotal
                    FROM SaleItem si
                    INNER JOIN MenuItem mi ON mi.Id = si.MenuItemId
                    INNER JOIN Sale s ON s.Id = si.SaleId; 
                ");

            // =========================================================
            // VIEW 2: Detailed Reporting View
            // =========================================================
            // 1. Drop the old view
            await _database.ExecuteAsync("DROP VIEW IF EXISTS vwSaleItemsWithDateMenuItem");

            // 2. Create without status filter
            await _database.ExecuteAsync(@"
                    CREATE VIEW vwSaleItemsWithDateMenuItem AS
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
                    INNER JOIN Sale s ON si.SaleId = s.Id
                    INNER JOIN MenuItem m ON si.MenuItemId = m.Id;
                ");
        }
    }

}
