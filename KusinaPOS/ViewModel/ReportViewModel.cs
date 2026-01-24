using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
            IDateTimeService dateTimeService)
        {
            _reportService = reportService;
            _salesService = salesService;
            _inventoryTransactionService = inventoryTransactionService;
            _inventoryItemService = inventoryItemService;
            _dateTimeService = dateTimeService;

            LoadPreferences();
            _dateTimeService.DateTimeChanged += (s, e) => CurrentDateTime = e;
            _= LoadTodayReportAsync();
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

                var fileName = $"SalesReport_{FromDate:yyyyMMdd}_to_{ToDate:yyyyMMdd}.csv";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                await ExportToCsvAsync(filePath);

                await PageHelper.DisplayAlertAsync(
                    "Success",
                    $"Report exported successfully to {fileName}",
                    "OK");
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to export: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportToCsvAsync(string filePath)
        {
            var csv = new StringBuilder();

            csv.AppendLine("Receipt Number,Date & Time,Subtotal,Discount,Tax,Total Amount,Amount Paid,Change,Status");

            foreach (var sale in SalesTransactions)
            {
                csv.AppendLine(
                    $"{sale.ReceiptNo}," +
                    $"{sale.SaleDate:MMM dd, yyyy hh:mm tt}," +
                    $"{sale.SubTotal:N2}," +
                    $"{sale.Discount:N2}," +
                    $"{sale.Tax:N2}," +
                    $"{sale.TotalAmount:N2}," +
                    $"{sale.AmountPaid:N2}," +
                    $"{sale.ChangeAmount:N2}," +
                    $"{sale.Status}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
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
    }
}
