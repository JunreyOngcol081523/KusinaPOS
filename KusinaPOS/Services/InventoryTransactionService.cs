using KusinaPOS.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Services
{
    public class InventoryTransactionService
    {
        private readonly SQLiteAsyncConnection _db;
        public InventoryTransactionService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }
        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<InventoryTransaction>();
        }
        //=============================//
        // 🔹 Add Inventory Transaction//
        //=============================//
        public async Task AddInventoryTransactionAsync(InventoryTransaction transaction)
        {
            await InitializeAsync();
            await _db.InsertAsync(transaction);
        }
        //=======================================//
        // 🔹 Get Transactions by Inventory Item//
        //======================================//
        public async Task<List<InventoryTransaction>> GetTransactionsByInventoryItemAsync(int inventoryItemId)
        {
            await InitializeAsync();
            return await _db.Table<InventoryTransaction>()
                            .Where(t => t.InventoryItemId == inventoryItemId)
                            .OrderByDescending(t => t.TransactionDate)
                            .ToListAsync();
        }
    }
}
