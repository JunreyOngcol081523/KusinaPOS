using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models.SQLViews
{
    public class MenuItemReportDto
    {
        public string MenuItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalSales { get; set; }
    }

}
