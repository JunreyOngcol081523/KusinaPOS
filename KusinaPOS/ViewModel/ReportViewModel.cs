using CommunityToolkit.Mvvm.ComponentModel;
using KusinaPOS.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.ViewModel
{
    public partial class ReportViewModel : ObservableObject
    {
        private ReportService _reportService;
        private SalesService _salesService;
        private InventoryTransactionService _inventoryTransactionService;
        private InventoryItemService _inventoryItemService;
        public ReportViewModel(ReportService reportService,
            SalesService salesService,
            InventoryTransactionService inventoryTransactionService,
            InventoryItemService inventoryItemService)
        {
            _reportService = reportService;
            _salesService = salesService;
            _inventoryTransactionService = inventoryTransactionService;
            _inventoryItemService = inventoryItemService;
        }
    }
}
