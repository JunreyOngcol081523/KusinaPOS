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
        [ObservableProperty] private decimal cashCollected;
        [ObservableProperty] private decimal averageSale;
        [ObservableProperty] private ObservableCollection<Sale> salesTransactions = new();
        [ObservableProperty] private Sale selectedSaleTransaction;
        [ObservableProperty] private ObservableCollection<SaleItemWithMenuName> saleItems = new();
        #endregion

        #region Menu Properties & Collections
        [ObservableProperty] private ObservableCollection<Top5MenuItem> top5MenuItems;
        [ObservableProperty] private ObservableCollection<AllMenuItemByCategory> allMenuItemsByCategory;
        [ObservableProperty] private ObservableCollection<string> categories = new();
        public ObservableCollection<MenuItemReportDto> MenuItemsReport { get; } = new();
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
            CategoryService categoryService)
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

            Top5MenuItems = new ObservableCollection<Top5MenuItem>();
            AllMenuItemsByCategory = new ObservableCollection<AllMenuItemByCategory>();

            LoadPreferences();
            _dateTimeService.DateTimeChanged += (s, e) => CurrentDateTime = e;
            _ = LoadTodayReportAsync();
            _ = LoadMenuCategoriesAsync(categoryService);
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
                TotalTransactions = filteredSales.Count;
                CashCollected = filteredSales.Sum(s => s.AmountPaid);
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

                var top5 = await _menuReportService.GetTop5MenuItemsAsync(SelectedCategory, start, end);
                Top5MenuItems = new ObservableCollection<Top5MenuItem>(top5);

                var allItems = await _menuReportService.GetAllMenuItemsByCategoryAsync(SelectedCategory, start, end);
                AllMenuItemsByCategory = new ObservableCollection<AllMenuItemByCategory>(allItems);

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
                  AND (? IS NULL OR Category = ?)
                GROUP BY MenuItemId, MenuItemName, Category
                ORDER BY TotalSales DESC;
            ";

            string? category = SelectedCategory == "All" || string.IsNullOrWhiteSpace(SelectedCategory)
                ? null
                : SelectedCategory;

            var result = await _db.QueryAsync<MenuItemReportDto>(
                sql,
                FromDate,
                ToDate.AddDays(1),
                category,
                category
            );

            foreach (var item in result)
                MenuItemsReport.Add(item);
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

                string categoryFilter = SelectedCategory ?? "All";

                // 1. Fetch VOLUME Data (Qty)
                var volumeData = await _menuReportService.GetAllMenuSalesForExportAsync(categoryFilter, FromDate, ToDate);

                // 2. Fetch SALES VALUE Data (Money)
                var salesData = await _menuReportService.GetAllMenuSalesRankingsAsync(categoryFilter, FromDate, ToDate);

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
    }
}