using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    public class TrendDataModel
    {
        // X-Axis: Stores "Mon", "Tue", "Wed", etc.
        public string DateLabel { get; set; }

        // Y-Axis: Stores the total cost for that day
        public decimal Amount { get; set; }
    }
}
