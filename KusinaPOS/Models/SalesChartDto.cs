using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    public class SalesChartDto
    {
        public string Argument { get; set; } = string.Empty;// The Label (e.g., "Mon", "10 AM", "Jan")
        public decimal Value { get; set; }    // The Sales Amount
    }
}
