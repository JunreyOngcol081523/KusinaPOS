using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Services
{
    public class InventoryReportService
    {
        private readonly SQLiteAsyncConnection _db;

        public InventoryReportService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }
        public async Task InitializeAsync() { 
            await _db.CreateTableAsync<Models.InventoryItem>();
            await _db.CreateTableAsync<Models.InventoryTransaction>();
        }
        // Get Total Expenses (Sum of StockIn value)
        public async Task<decimal> GetTotalInventoryExpensesAsync(DateTime from, DateTime to)
        {
            await InitializeAsync();
            var start = from.Date;
            var end = to.Date.AddDays(1).AddTicks(-1);

            // Using your View_InventoryHistory
            // We sum 'TransactionValue' where Reason is 'StockIn'
            var result = await _db.ExecuteScalarAsync<decimal>(@"
                    SELECT SUM(TransactionValue) 
                    FROM View_InventoryHistory 
                    WHERE Reason = 'StockIn' 
                    AND QuantityChange > 0
                    AND TransactionDate BETWEEN ? AND ?", start, end);

            return result;
        }

        public async Task<List<InventoryHistoryDto>> GetInventoryReportAsync(DateTime from, DateTime to, string? reason = null)
        {
            await InitializeAsync();
            var start = from.Date;
            var end = to.Date.AddDays(1).AddTicks(-1);

            // If reason is null, '? IS NULL' becomes true and returns everything
            // If reason has a value, 'Reason = ?' performs the specific filter
            string sql = @"
                SELECT * FROM View_InventoryHistory 
                WHERE (TransactionDate BETWEEN ? AND ?)
                AND (? IS NULL OR Reason = ?)
                ORDER BY TransactionDate DESC";

            return await _db.QueryAsync<InventoryHistoryDto>(sql, start, end, reason, reason);
        }
    }

    // Helper DTOs
    public class TransactionValueDto { public decimal Total { get; set; } }
    public class InventoryHistoryDto
    {
        public string ItemName { get; set; }
        public decimal QuantityChange { get; set; }
        public decimal TransactionValue { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Remarks { get; set; }
    }
}
