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


        public decimal AmountPaid { get; set; }

        public decimal ChangeAmount { get; set; }

        // Status: "Completed", "Voided", "Refunded"
        [NotNull, Indexed]
        public string Status { get; set; } = "Completed";

        // --- Audit Fields for Voids/Refunds ---
        public DateTime? ActionDate { get; set; } // When it was voided/refunded
        public string? Reason { get; set; }       // Why? (e.g., "Customer changed mind")
        public string? AuthorizedBy { get; set; } // Which manager approved it?
        public string? CustomerName { get; set; } // Which manager approved it?
        public string? CustomerContact{ get; set; } // Which manager approved it?

        // For Refunds: Link to the original ReceiptNo if this is a partial refund
        public string? ReferenceReceiptNo { get; set; }
    }


}
