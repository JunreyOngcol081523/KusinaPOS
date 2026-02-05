using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    public class InventoryTransaction
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int InventoryItemId { get; set; }

        // Nullable for stock-in / adjustments
        [Indexed]
        public int? SaleId { get; set; }

        // Negative = deduction, Positive = add
        [NotNull]
        public decimal QuantityChange { get; set; }
        [NotNull]
        public decimal CostAtTransaction { get; set; }

        // Sale, Void, StockIn, Adjustment
        [NotNull]
        public string Reason { get; set; } = string.Empty;
        //can be null for cases where no remarks are needed
        public string? Remarks { get; set; } = string.Empty;

        [NotNull]
        public DateTime TransactionDate { get; set; }
    }


}
