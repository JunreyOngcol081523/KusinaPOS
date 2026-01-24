using CommunityToolkit.Mvvm.ComponentModel;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KusinaPOS.ViewModel
{
    public partial class ReportViewModel : ObservableObject
    {
        private ReportService _reportService;
        private SalesService _salesService;
        private InventoryTransactionService _inventoryTransactionService;
        private InventoryItemService _inventoryItemService;
        private readonly IDateTimeService _dateTimeService;
        #region Header
        [ObservableProperty] private string currentDateTime = string.Empty;
        [ObservableProperty] private string loggedInUserName = string.Empty;
        [ObservableProperty] private string loggedInUserId = string.Empty;
        [ObservableProperty] private string storeName = "Kusina POS";
        #endregion
        public ReportViewModel(ReportService reportService,
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
        }
        private void LoadPreferences()
        {
            LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
            CurrentDateTime = _dateTimeService.CurrentDateTime;
            
        }
    }
}
