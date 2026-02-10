using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Models.SQLViews;
using KusinaPOS.Services;
using SQLite;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KusinaPOS.ViewModel
{
    public partial class ReportViewModel : ObservableObject
    {
        #region Services
        private readonly ReportService _reportService;
        private readonly SalesService _salesService;
        private readonly InventoryTransactionService _inventoryTransactionService;
        private readonly InventoryItemService _inventoryItemService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ExcelExportService _excelExportService;
        private readonly MenuReportService _menuReportService;
        private readonly CategoryService _categoryService;
        private readonly SQLiteAsyncConnection _db;
        private readonly InventoryReportService _inventoryReportService;
        #endregion

        #region Header Properties
        [ObservableProperty] private string currentDateTime = string.Empty;
        [ObservableProperty] private string loggedInUserName = string.Empty;
        [ObservableProperty] private string loggedInUserId = string.Empty;
        [ObservableProperty] private string storeName = "Kusina POS";
        #endregion

        #region Filter Properties
        [ObservableProperty] private DateTime fromDate = DateTime.Today;
        [ObservableProperty] private DateTime toDate = DateTime.Today;
        [ObservableProperty] private string selectedCategory = "All";
        #endregion

        #region Sales Properties & Collections
        [ObservableProperty] private decimal totalSales;
        [ObservableProperty] private int totalTransactions;
        [ObservableProperty] private decimal cashRefunded;
        [ObservableProperty] private decimal averageSale;
        [ObservableProperty] private ObservableCollection<Sale> salesTransactions = new();
        [ObservableProperty] private Sale selectedSaleTransaction;
        [ObservableProperty] private ObservableCollection<SaleItemWithMenuName> saleItems = new();
        #endregion

        #region Menu Properties & Collections
        [ObservableProperty] private ObservableCollection<Top5MenuItem> topIncomeGeneratingMenu;
        [ObservableProperty] private ObservableCollection<AllMenuItemByCategory> topMenuItemsBySoldQty;
        [ObservableProperty] private ObservableCollection<string> categories = new();
        public ObservableCollection<MenuItemReportDto> MenuItemsReport { get; } = new();
        #endregion
        #region Expenses and Inventory Properties & Collections
        [ObservableProperty] private ObservableCollection<InventoryHistoryDto> inventoryHistory;
        [ObservableProperty] private ObservableCollection<InventoryHistoryDto> pieChartStockMovement;
        [ObservableProperty] private decimal totalInventoryExpense;
        [ObservableProperty] private decimal totalValueOfWasteItems;
        [ObservableProperty] private ObservableCollection<TrendDataModel> trendData;
        [ObservableProperty] private ObservableCollection<InventoryItem> lowStockItems;
        #endregion
        #region UI State
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isOpenPopup;
        public event Action? ShowSaleItemsPopupRequested;
        #endregion

        #region Constructor & Initialization
        public ReportViewModel(
            ReportService reportService,
            SalesService salesService,
            InventoryTransactionService inventoryTransactionService,
            InventoryItemService inventoryItemService,
            IDateTimeService dateTimeService,
            ExcelExportService excelExportService,
            IDatabaseService db,
            MenuReportService menuReportService,
            CategoryService categoryService,
            InventoryReportService inventoryReportService)
        {
            _reportService = reportService;
            _salesService = salesService;
            _inventoryTransactionService = inventoryTransactionService;
            _inventoryItemService = inventoryItemService;
            _dateTimeService = dateTimeService;
            _excelExportService = excelExportService;
            _menuReportService = menuReportService;
            _categoryService = categoryService;
            _db = db.GetConnection();

            TopIncomeGeneratingMenu = new ObservableCollection<Top5MenuItem>();
            TopMenuItemsBySoldQty = new ObservableCollection<AllMenuItemByCategory>();
            InventoryHistory = new ObservableCollection<InventoryHistoryDto>();
            TrendData = new ObservableCollection<TrendDataModel>();
            LowStockItems = new ObservableCollection<InventoryItem>();
            PieChartStockMovement = new ObservableCollection<InventoryHistoryDto>();
            LoadPreferences();
            _dateTimeService.DateTimeChanged += (s, e) => CurrentDateTime = e;
            _ = LoadTodayReportAsync();
            _ = LoadMenuCategoriesAsync(categoryService);
            _inventoryReportService = inventoryReportService;
            
        }

        private void LoadPreferences()
        {
            LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
            CurrentDateTime = _dateTimeService.CurrentDateTime;
        }
        #endregion

        #region Navigation
        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
        #endregion

        #region Sales Report - Filter Commands
        [RelayCommand]
        private async Task FilterTodayAsync()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await GenerateReportAsync();
        }

        [RelayCommand]
        private async Task FilterWeekAsync()
        {
            FromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            ToDate = DateTime.Today;
            await GenerateReportAsync();
        }

        [RelayCommand]
        private async Task FilterMonthAsync()
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            await GenerateReportAsync();
        }
        #endregion

        #region Sales Report - Generation
        [RelayCommand]
        private async Task GenerateReportAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (FromDate > ToDate)
                {
                    await PageHelper.DisplayAlertAsync(
                        "Invalid Date Range",
                        "From Date cannot be later than To Date",
                        "OK");
                    return;
                }

                var allSales = await _salesService.GetAllSalesAsync();
                var startOfDay = FromDate.Date;
                var endOfDay = ToDate.Date.AddDays(1).AddTicks(-1);

                var filteredSales = allSales
                    .Where(s =>
                        s.SaleDate >= startOfDay &&
                        s.SaleDate <= endOfDay &&
                        (s.Status == "Completed" || s.Status == "Refunded" || s.Status == "Voided"))
                    .OrderByDescending(s => s.SaleDate)
                    .ToList();

                TotalSales = filteredSales.Sum(s => s.TotalAmount);
                TotalTransactions = await _salesService.GetSalesCountByStatusAsync("Completed",startOfDay, endOfDay);
                CashRefunded = await _salesService.GetRefundTotalByDateAsync(startOfDay, endOfDay);
                AverageSale = TotalTransactions > 0 ? TotalSales / TotalTransactions : 0;

                SalesTransactions = new ObservableCollection<Sale>(filteredSales);
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to generate report: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadTodayReportAsync()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await GenerateReportAsync();
        }
        #endregion

        #region Sales Report - Export
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (!SalesTransactions.Any())
            {
                await PageHelper.DisplayAlertAsync("No Data", "No transactions to export", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                var storeName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
                var allSales = await _salesService.GetAllSalesAsync();
                var startOfDay = FromDate.Date;
                var endOfDay = ToDate.Date.AddDays(1).AddTicks(-1);

                var filteredSales = allSales
                    .Where(s => s.SaleDate >= startOfDay && s.SaleDate <= endOfDay)
                    .OrderByDescending(s => s.SaleDate)
                    .ToList();

                await _excelExportService.ExportSalesReportAsync(filteredSales, FromDate, ToDate, storeName);
                await PageHelper.DisplayAlertAsync("Success", "Sales report exported successfully!", "OK");
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to export: {ex.Message}", "OK");
                Debug.WriteLine($"Export error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Sales Report - Sale Items
        partial void OnSelectedSaleTransactionChanged(Sale value)
        {
            SaleItems = new ObservableCollection<SaleItemWithMenuName>();

            if (value == null)
            {
                SaleItems.Clear();
                return;
            }

            _ = LoadSaleItemsAsync(value.Id);
            IsOpenPopup = true;
        }

        private async Task LoadSaleItemsAsync(int saleId)
        {
            var items = await _salesService.GetSaleItemsWithMenuNameAsync(saleId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SaleItems.Clear();
                foreach (var item in items)
                    SaleItems.Add(item);
            });
        }
        #endregion

        #region Menu Report - Initialization
        public async Task LoadMenuCategoriesAsync(CategoryService categoryService)
        {
            try
            {
                Categories.Clear();
                Categories.Add("All");

                var activeCategories = await categoryService.GetActiveCategoriesAsync();
                foreach (var cat in activeCategories)
                    Categories.Add(cat.Name);

                SelectedCategory = "All";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load menu categories: {ex.Message}");
            }
        }
        #endregion

        #region Menu Report - Filter Commands
        [RelayCommand]
        private async Task FilterMenuTodayAsync()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await GenerateMenuReportAsync();
        }

        [RelayCommand]
        private async Task FilterMenuWeekAsync()
        {
            FromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            ToDate = DateTime.Today;
            await GenerateMenuReportAsync();
        }

        [RelayCommand]
        private async Task FilterMenuMonthAsync()
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            await GenerateMenuReportAsync();
        }
        #endregion

        #region Menu Report - Generation
        [RelayCommand]
        private async Task GenerateMenuReportAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (FromDate > ToDate)
                {
                    await PageHelper.DisplayAlertAsync("Invalid Date Range", "From Date cannot be later than To Date", "OK");
                    return;
                }

                var start = FromDate.Date;
                var end = ToDate.Date.AddDays(1).AddTicks(-1);

                var top5 = await _menuReportService.GetTopIncomeGeneratingMenuItemsAsync(SelectedCategory, start, end);
                TopIncomeGeneratingMenu = new ObservableCollection<Top5MenuItem>(top5);

                var allItems = await _menuReportService.GetTopMenuBySoldQty(SelectedCategory, start, end);
                TopMenuItemsBySoldQty = new ObservableCollection<AllMenuItemByCategory>(allItems);

                await LoadMenuItemsReportAsync();
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to generate menu report: {ex.Message}", "OK");
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadMenuItemsReportAsync()
        {
            MenuItemsReport.Clear();

            const string sql = @"
                            SELECT
                                MenuItemName,
                                Category,
                                SUM(Quantity) AS QuantitySold,
                                SUM(LineTotal) AS TotalSales
                            FROM vwSaleItemsWithDateMenuItem
                            WHERE SaleDate >= ?
                              AND SaleDate < ?
                              AND Status = 'Completed'
                              AND (? IS NULL OR Category = ?)
                            GROUP BY MenuItemId, MenuItemName, Category
                            ORDER BY TotalSales DESC;
                            ";

            // Handle "All" category by passing null to the query parameters
            string? category = SelectedCategory == "All" || string.IsNullOrWhiteSpace(SelectedCategory)
                ? null
                : SelectedCategory;

            try
            {
                var result = await _db.QueryAsync<MenuItemReportDto>(
                    sql,
                    FromDate.Date,       // Ensure we start at 00:00:00
                    ToDate.Date.AddDays(1), // Includes the whole of ToDate up to midnight
                    category,
                    category
                );

                foreach (var item in result)
                    MenuItemsReport.Add(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPORT ERROR] {ex.Message}");
                // Optional: Notify user of the failure
            }
        }
        #endregion
        #region Menu Report - Export
        [RelayCommand]
        private async Task MenuReportExportToExcelAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (FromDate > ToDate)
                {
                    await PageHelper.DisplayAlertAsync("Invalid Date", "From Date cannot be later than To Date", "OK");
                    return;
                }
                var _toDate = ToDate.Date.AddDays(1).AddTicks(-1); // Sets time to 23:59:59.999
                string categoryFilter = SelectedCategory ?? "All";

                // 1. Fetch VOLUME Data (Qty)
                var volumeData = await _menuReportService.GetAllMenuSalesForExportAsync(categoryFilter, FromDate, _toDate);

                // 2. Fetch SALES VALUE Data (Money)
                var salesData = await _menuReportService.GetAllMenuSalesRankingsAsync(categoryFilter, FromDate, _toDate);

                // 3. Check if empty (check both to be safe)
                if ((volumeData == null || !volumeData.Any()) && (salesData == null || !salesData.Any()))
                {
                    await PageHelper.DisplayAlertAsync("No Data", "No sales found for this period.", "OK");
                    return;
                }

                // 4. Export Combined File
                await _excelExportService.ExportMenuPerformanceAsync(volumeData, salesData, FromDate, ToDate);
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Export Failed", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Expenses Report
        #region Expenses Report - Generation
        [RelayCommand]
        private async Task GenerateExpensesReportAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (FromDate > ToDate)
                {
                    await PageHelper.DisplayAlertAsync("Invalid Date Range", "From Date cannot be later than To Date", "OK");
                    return;
                }
                var start = FromDate.Date;
                var end = ToDate.Date.AddDays(1).AddTicks(-1);
                var expenses = await _inventoryReportService.GetInventoryReportAsync(start, end);
                if(expenses == null || !expenses.Any())
                {
                    await PageHelper.DisplayAlertAsync("No Data", "No inventory transactions found for this period.", "OK");
                    InventoryHistory.Clear();
                    TotalInventoryExpense = 0;
                    return;
                }
                InventoryHistory = new ObservableCollection<InventoryHistoryDto>(expenses);
                TotalInventoryExpense = await _inventoryReportService.GetTotalInventoryExpensesAsync(start, end);
                TotalValueOfWasteItems = await _inventoryReportService.GetTotalWastedValueAsync(start, end);
                await LoadStockMovementPieChartAsync();
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to generate expenses report: {ex.Message}", "OK");
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task LoadExpensesReportAsync()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await GenerateExpensesReportAsync();
        }
        public async Task LoadWeeklyChartDataAsync()
        {
            try
            {
                // 1. Calculate the start (Monday) and end (Sunday) of the CURRENT week
                // regardless of the user's DatePicker selection.
                DateTime today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime startOfWeek = today.AddDays(-1 * diff).Date; // Monday
                DateTime endOfWeek = startOfWeek.AddDays(7).AddTicks(-1); // End of Sunday

                // 2. Fetch ONLY "StockIn" data for this fixed week
                // We pass "StockIn" to ensure we don't accidentally chart StockOuts or Wastage.
                var weeklyExpenses = await _inventoryReportService.GetInventoryReportAsync(startOfWeek, endOfWeek, "Stock In");

                // 3. Prepare the buckets for Mon-Sun
                var daysOfWeek = new List<string> { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                var chartPoints = new List<TrendDataModel>();

                for (int i = 0; i < 7; i++)
                {
                    var currentDayDate = startOfWeek.AddDays(i);
                    var dayName = daysOfWeek[i];

                    // 4. Calculate Daily Sum
                    // Since View_InventoryHistory already multiplied Quantity * Cost, we just SUM here.
                    var dailySum = weeklyExpenses
                        .Where(e => e.TransactionDate.Date == currentDayDate)
                        .Sum(e => e.TransactionValue);

                    chartPoints.Add(new TrendDataModel
                    {
                        DateLabel = dayName,
                        Amount = dailySum
                    });
                }

                // 5. Update the UI
                TrendData = new ObservableCollection<TrendDataModel>(chartPoints);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading chart: {ex.Message}");
            }
        }
        public async Task LoadStockMovementPieChartAsync()
        {
            try
            {
                // 1. Get raw transactions
                var transactions = await _inventoryReportService.GetInventoryReportAsync(FromDate, ToDate);

                // 2. Process the data: Filter, Group, Sum, and SORT
                var groupedData = transactions
                    .Where(t => t.Reason == "SALE" || t.Reason == "Waste")
                    .GroupBy(t => t.Reason)
                    .Select(g => new InventoryHistoryDto
                    {
                        Reason = g.Key,
                        QuantityChange = g.Sum(t => Math.Abs(t.QuantityChange))
                    }).OrderBy(x => x.Reason == "SALE" ? 0
                                : x.Reason == "Waste" ? 1
                                : 2).ToList();

                // 4. Update the UI
                PieChartStockMovement.Clear();
                foreach (var item in groupedData)
                {
                    PieChartStockMovement.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading pie chart: {ex.Message}");
            }
        }
        #endregion
        #region Expenses Report - Filter Commands
        [RelayCommand]
        private async Task FilterExpensesTodayAsync()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await GenerateExpensesReportAsync();
        }

        [RelayCommand]
        private async Task FilterExpensesWeekAsync()
        {
            FromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            ToDate = DateTime.Today;
            await GenerateExpensesReportAsync();
        }

        [RelayCommand]
        private async Task FilterExpensesMonthAsync()
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            await GenerateExpensesReportAsync();
        }
        #endregion
        #region Expenses Report - Low Stocks
        public async Task LoadLowStockItemsAsync()
        {
            try
            {
                var allitems = await _inventoryItemService.GetAllInventoryItemsAsync();
                var lowStock = allitems.Where(i => i.IsLowStock).ToList();
                LowStockItems = new ObservableCollection<InventoryItem>(lowStock);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load low stock items: {ex.Message}");
            }
        }
        #endregion
        #region Expenses Report - Excel Export
        [RelayCommand]
        private async Task ExportInventoryToExcelAsync()
        {
            // 1. Check if there is data to export (Optional: check your UI collection)
            if (InventoryHistory == null || !InventoryHistory.Any())
            {
                await PageHelper.DisplayAlertAsync("No Data", "No inventory records to export.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // 2. Get Store Name
                var storeName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

                // 3. Fetch the exact data for the report
                // We re-fetch here to ensure the export matches the Date Range and Filter selected
                // Note: passing 'SelectedFilterReason' (e.g., "Waste", "Stock In" or null)
                var reportData = await _inventoryReportService.GetInventoryReportAsync(FromDate, ToDate);

                if (!reportData.Any())
                {
                    await PageHelper.DisplayAlertAsync("No Data", "No records found for the selected range.", "OK");
                    return;
                }

                // 4. Generate and Open the Excel file
                await _excelExportService.ExportInventoryReportAsync(
                    reportData,
                    FromDate,
                    ToDate, // Pass this so the Excel header shows "Filter: Waste"
                    storeName
                );

                await PageHelper.DisplayAlertAsync("Success", "Inventory report exported successfully!", "OK");
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to export: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Export error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion
        #endregion
    }
}