using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    public class Sale
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed, NotNull]
        public string ReceiptNo { get; set; } = string.Empty;

        [NotNull]
        public DateTime SaleDate { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Discount { get; set; }

        public decimal Tax { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public decimal AmountPaid { get; set; }

        public decimal ChangeAmount { get; set; }

        // Completed, Voided, Refunded
        [NotNull]
        public string Status { get; set; } = "Completed";
    }


}
