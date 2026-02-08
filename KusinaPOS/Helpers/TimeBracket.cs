using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Helpers
{
    // ---------------------------------------------------------
    // HELPER: Time Bracketing Logic
    // ---------------------------------------------------------

    public class TimeBracket
    {
        public int Id { get; set; }       // For sorting (1 comes before 2)
        public string Label { get; set; } // The text to show on chart


        public TimeBracket GetTimeBracket(int hour)
        {
            if (hour >= 6 && hour < 10) return new TimeBracket { Id = 1, Label = "Breakfast\n(6-10am)" };
            if (hour >= 10 && hour < 12) return new TimeBracket { Id = 2, Label = "Mid-Morn\n(10-12pm)" };
            if (hour >= 12 && hour < 14) return new TimeBracket { Id = 3, Label = "Lunch Rush\n(12-2pm)" };
            if (hour >= 14 && hour < 18) return new TimeBracket { Id = 4, Label = "Afternoon\n(2-6pm)" };
            if (hour >= 18 && hour < 21) return new TimeBracket { Id = 5, Label = "Dinner\n(6-9pm)" };
            if (hour >= 21) return new TimeBracket { Id = 6, Label = "Late Night\n(9pm+)" };

            return new TimeBracket { Id = 7, Label = "Early Bird" }; // 12am - 6am
        }
    }
}