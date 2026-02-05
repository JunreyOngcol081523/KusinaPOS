using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using KusinaPOS.Models;

namespace KusinaPOS.Services
{
    public class MenuReportService
    {
        private readonly SQLiteAsyncConnection _db;

        public MenuReportService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        public async Task<List<Top5MenuItem>> GetTopIncomeGeneratingMenuItemsAsync(string category, DateTime fromDate, DateTime toDate)
        {
            // 1. Base query with Status filter
            string sql = @"SELECT MenuItemName, SUM(UnitPrice * Quantity) AS TotalSales
                       FROM vwSaleItemsWithDateMenuItem
                       WHERE SaleDate BETWEEN ? AND ? 
                       AND Status = 'Completed' ";

            // 2. Safe filtering for Category
            if (category != "All")
            {
                sql += " AND Category = ? ";
                sql += " GROUP BY MenuItemName ORDER BY TotalSales DESC LIMIT 5;";
                return await _db.QueryAsync<Top5MenuItem>(sql, fromDate, toDate, category);
            }

            sql += " GROUP BY MenuItemName ORDER BY TotalSales DESC LIMIT 5;";
            return await _db.QueryAsync<Top5MenuItem>(sql, fromDate, toDate);
        }

        public Task<List<AllMenuItemByCategory>> GetTopMenuBySoldQty(string category, DateTime fromDate, DateTime toDate)
        {
            // Added 'AND Status = 'Completed'' to the base query
            string sql = @"SELECT MenuItemName, Category, SUM(Quantity) AS QuantitySold
                   FROM vwSaleItemsWithDateMenuItem
                   WHERE (SaleDate BETWEEN ? AND ?)
                   AND Status = 'Completed'
                   {0}
                   GROUP BY MenuItemName, Category
                   ORDER BY QuantitySold DESC LIMIT 5;";

            if (category != "All")
            {
                // Use a placeholder {0} for the extra condition, 
                // but use a parameter '?' for the actual value to be safe.
                sql = string.Format(sql, "AND Category = ?");
                return _db.QueryAsync<AllMenuItemByCategory>(sql, fromDate, toDate, category);
            }

            // If "All", just remove the placeholder
            sql = string.Format(sql, "");
            return _db.QueryAsync<AllMenuItemByCategory>(sql, fromDate, toDate);
        }
        // In MenuReportService.cs

        public async Task<List<AllMenuItemByCategory>> GetAllMenuSalesForExportAsync(string category, DateTime fromDate, DateTime toDate)
        {
            string sql = @"SELECT MenuItemName, Category, SUM(Quantity) AS QuantitySold
                       FROM vwSaleItemsWithDateMenuItem
                       WHERE SaleDate BETWEEN ? AND ?
                       AND Status = 'Completed' ";

            if (category != "All")
            {
                sql += " AND Category = ? GROUP BY MenuItemName, Category ORDER BY Category ASC, QuantitySold DESC;";
                return await _db.QueryAsync<AllMenuItemByCategory>(sql, fromDate, toDate, category);
            }

            sql += " GROUP BY MenuItemName, Category ORDER BY Category ASC, QuantitySold DESC;";
            return await _db.QueryAsync<AllMenuItemByCategory>(sql, fromDate, toDate);
        }
        public Task<List<Top5MenuItem>> GetAllMenuSalesRankingsAsync(string category, DateTime fromDate, DateTime toDate)
        {
            // Added 'AND Status = 'Completed''
            // This ensures only successful, non-refunded, non-voided sales are ranked.
            string sql = @"SELECT MenuItemName, SUM(UnitPrice * Quantity) AS TotalSales
                   FROM vwSaleItemsWithDateMenuItem
                   WHERE (SaleDate BETWEEN ? AND ?)
                   AND Status = 'Completed'
                   {0}
                   GROUP BY MenuItemName
                   ORDER BY TotalSales DESC;";

            // Using a safer parameter check for category
            if (category != "All")
            {
                sql = string.Format(sql, "AND Category = ?");
                return _db.QueryAsync<Top5MenuItem>(sql, fromDate, toDate, category);
            }

            sql = string.Format(sql, "");
            return _db.QueryAsync<Top5MenuItem>(sql, fromDate, toDate);
        }
    }
}
