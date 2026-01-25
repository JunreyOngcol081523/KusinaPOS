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

        public Task<List<Top5MenuItem>> GetTop5MenuItemsAsync(string category, DateTime fromDate, DateTime toDate)
        {
            // Filter by category and date range
            string sql = @"SELECT MenuItemName, SUM(UnitPrice * Quantity) AS TotalSales
                           FROM vwSaleItemsWithDateMenuItem
                           WHERE SaleDate BETWEEN ? AND ?
                           {0}
                           GROUP BY MenuItemName
                           ORDER BY TotalSales DESC
                           LIMIT 5;";

            string categoryFilter = category != "All" ? "AND Category = '" + category + "'" : "";

            sql = string.Format(sql, categoryFilter);

            return _db.QueryAsync<Top5MenuItem>(sql, fromDate, toDate);
        }

        public Task<List<AllMenuItemByCategory>> GetAllMenuItemsByCategoryAsync(string category, DateTime fromDate, DateTime toDate)
        {
            string sql = @"SELECT MenuItemName, Category, SUM(Quantity) AS QuantitySold
                           FROM vwSaleItemsWithDateMenuItem
                           WHERE SaleDate BETWEEN ? AND ?
                           {0}
                           GROUP BY MenuItemName, Category
                           ORDER BY QuantitySold DESC LIMIT 5;";

            string categoryFilter = category != "All" ? "AND Category = '" + category + "'" : "";

            sql = string.Format(sql, categoryFilter);

            return _db.QueryAsync<AllMenuItemByCategory>(sql, fromDate, toDate);
        }
    }
}
