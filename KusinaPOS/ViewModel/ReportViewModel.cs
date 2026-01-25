using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Models.SQLViews;
using KusinaPOS.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
namespace KusinaPOS.ViewModel
{
    public partial class ReportViewModel : ObservableObject
    {
        #region Fields & Services
        private readonly ReportService _reportService;
        private readonly SalesService _salesService;
        private readonly InventoryTransactionService _inventoryTransactionService;
        private readonly InventoryItemService _inventoryItemService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ExcelExportService _excelExportService;
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
        #endregion

        #region KPI Properties
        [ObservableProperty] private decimal totalSales;
        [ObservableProperty] private int totalTransactions;
        [ObservableProperty] private decimal cashCollected;
        [ObservableProperty] private decimal averageSale;
        #endregion

        #region Collections
        [ObservableProperty]
        private ObservableCollection<Sale> salesTransactions = new();
        [ObservableProperty]
        private Sale selectedSaleTransaction;
        [ObservableProperty]
        private ObservableCollection<SaleItemWithMenuName> saleItems=new();
        #endregion

        #region UI State
        [ObservableProperty] private bool isBusy;
        #endregion

        #region Constructor
        public ReportViewModel(
            ReportService reportService,
            SalesService salesService,
            InventoryTransactionService inventoryTransactionService,
            InventoryItemService inventoryItemService,
            IDateTimeService dateTimeService,
            ExcelExportService excelExportService,
            IDatabaseService db)
        {
            _reportService = reportService;
            _salesService = salesService;
            _inventoryTransactionService = inventoryTransactionService;
            _inventoryItemService = inventoryItemService;
            _dateTimeService = dateTimeService;

            LoadPreferences();
            _dateTimeService.DateTimeChanged += (s, e) => CurrentDateTime = e;
            _ = LoadTodayReportAsync();
            _excelExportService = excelExportService;
        }
        #endregion

        #region Initialization
        private void LoadPreferences()
        {
            LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
            CurrentDateTime = _dateTimeService.CurrentDateTime;
        }
        #endregion

        #region Navigation Commands
        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
        #endregion

        // ====================== SALES REPORT ====================== //

        #region Quick Filter Commands
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

        #region Report Generation
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
                        s.Status == "Completed")
                    .OrderByDescending(s => s.SaleDate)
                    .ToList();

                TotalSales = filteredSales.Sum(s => s.TotalAmount);
                TotalTransactions = filteredSales.Count;
                CashCollected = filteredSales.Sum(s => s.AmountPaid);
                AverageSale = TotalTransactions > 0
                    ? TotalSales / TotalTransactions
                    : 0;

                SalesTransactions = new ObservableCollection<Sale>(filteredSales);
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to generate report: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Export
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (!SalesTransactions.Any())
            {
                await PageHelper.DisplayAlertAsync(
                    "No Data",
                    "No transactions to export",
                    "OK");
                return;
            }

            try
            {
                IsBusy = true;

                var storeName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");

                await _excelExportService.ExportSalesReportAsync(
                    SalesTransactions.ToList(),
                    FromDate,
                    ToDate,
                    storeName);

                await PageHelper.DisplayAlertAsync(
                    "Success",
                    "Sales report exported successfully!",
                    "OK");
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to export: {ex.Message}",
                    "OK");

                Debug.WriteLine($"Export error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }


        #endregion

        #region Helpers
        private async Task LoadTodayReportAsync()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await GenerateReportAsync();
        }
        #endregion
        partial void OnSelectedSaleTransactionChanged(Sale value)
        {
            SaleItems = new ObservableCollection<SaleItemWithMenuName>();

            if (value == null)
            {
                SaleItems.Clear();
                return;
            }

            _ = LoadSaleItemsAsync(value.Id);
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

    }
}
