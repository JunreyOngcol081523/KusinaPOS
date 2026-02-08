using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics; // Added for Debug.WriteLine

namespace KusinaPOS.Services
{
    public class InventoryReportService
    {
        private readonly SQLiteAsyncConnection _db;

        public InventoryReportService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _db.CreateTableAsync<Models.InventoryItem>();
                await _db.CreateTableAsync<Models.InventoryTransaction>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InventoryService] Init Error: {ex.Message}");
            }
        }

        // Get Total Expenses (Sum of StockIn value)
        public async Task<decimal> GetTotalInventoryExpensesAsync(DateTime from, DateTime to)
        {
            try
            {
                await InitializeAsync();
                var start = from.Date;
                var end = to.Date.AddDays(1).AddTicks(-1);

                // Using your View_InventoryHistory
                // We sum 'TransactionValue' where Reason is 'StockIn'
                var result = await _db.ExecuteScalarAsync<decimal>(@"
                    SELECT SUM(TransactionValue) 
                    FROM View_InventoryHistory 
                    WHERE Reason = 'Stock In' 
                    AND QuantityChange > 0
                    AND TransactionDate BETWEEN ? AND ?", start, end);

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InventoryService] GetTotalInventoryExpensesAsync Error: {ex.Message}");
                return 0; // Return 0 on error to prevent crash
            }
        }

        // Get total value of wasted items (Sum of StockOut value)
        public async Task<decimal> GetTotalWastedValueAsync(DateTime from, DateTime to)
        {
            try
            {
                await InitializeAsync();
                var start = from.Date;
                var end = to.Date.AddDays(1).AddTicks(-1);

                // Using your View_InventoryHistory
                // We sum 'TransactionValue' where Reason is 'Waste' and QuantityChange < 0
                // Note: If QuantityChange is stored as negative, ensure TransactionValue is positive magnitude
                // or adjust logic depending on how you calculate Value.
                var result = await _db.ExecuteScalarAsync<decimal>(@"
                    SELECT ABS(SUM(TransactionValue)) 
                    FROM View_InventoryHistory 
                    WHERE Reason = 'Waste' 
                    AND QuantityChange < 0
                    AND TransactionDate BETWEEN ? AND ?", start, end);

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InventoryService] GetTotalWastedValueAsync Error: {ex.Message}");
                return 0; // Return 0 on error
            }
        }

        public async Task<List<InventoryHistoryDto>> GetInventoryReportAsync(DateTime from, DateTime to, string? reason = null)
        {
            try
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

                var result = await _db.QueryAsync<InventoryHistoryDto>(sql, start, end, reason, reason);

                return result ?? new List<InventoryHistoryDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InventoryService] GetInventoryReportAsync Error: {ex.Message}");
                return new List<InventoryHistoryDto>(); // Return empty list on error
            }
        }
    }

    // Helper DTOs
    public class TransactionValueDto { public decimal Total { get; set; } }

    public class InventoryHistoryDto
    {
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public decimal QuantityChange { get; set; }
        public decimal TransactionValue { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Reason { get; set; }
        public string Remarks { get; set; }
    }
}