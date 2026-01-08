using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    public class SaleItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int SaleId { get; set; }

        [Indexed]
        public int MenuItemId { get; set; }

        [NotNull]
        public int Quantity { get; set; }

        [NotNull]
        public decimal UnitPrice { get; set; }

        [Ignore]
        public decimal LineTotal => Quantity * UnitPrice;

    }


}
