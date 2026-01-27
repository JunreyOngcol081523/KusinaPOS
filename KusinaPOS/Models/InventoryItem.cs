using SQLite;
using System;

namespace KusinaPOS.Models
{
    public class InventoryItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Name { get; set; } = string.Empty;

        // grams, pcs, ml, etc.
        [NotNull]
        public string Unit { get; set; } = string.Empty;

        [NotNull]
        public decimal QuantityOnHand { get; set; }

        [NotNull]
        public decimal CostPerUnit { get; set; }

        public decimal ReOrderLevel { get; set; }

        public bool IsActive { get; set; } = true;

        // Computed (not stored in DB)
        [Ignore]
        public bool IsLowStock => QuantityOnHand <= ReOrderLevel;
    }
}
