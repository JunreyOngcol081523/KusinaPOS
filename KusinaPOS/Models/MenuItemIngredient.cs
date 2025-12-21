using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    public class MenuItemIngredient
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int MenuItemId { get; set; }

        [Indexed]
        public int InventoryItemId { get; set; }

        // Exact grams or pcs deducted per sale
        [NotNull]
        public decimal QuantityPerMenu { get; set; }
    }


}
