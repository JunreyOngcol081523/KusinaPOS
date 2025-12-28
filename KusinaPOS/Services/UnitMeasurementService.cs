using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Services
{
    public class UnitMeasurementService
    {
        public static readonly Dictionary<string, List<string>> UnitsByCategory = new Dictionary<string, List<string>>()
    {
        { "Count-Based", new List<string> { "pcs", "bottle", "pack", "box", "carton" } },
        { "Weight-Based", new List<string> { "grams", "kg" } },
        { "Volume-Based", new List<string> { "ml", "liter" } },
        { "Recipe Measurements", new List<string> { "cup", "tbsp", "tsp", "slice", "dozen" } }
    };

        // Flatten all units into a single list
        public static List<string> AllUnits => UnitsByCategory.Values.SelectMany(u => u).ToList();
        // Static method to get units by category key
        public static List<string> GetUnitsByCategory(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new List<string>();

            if (UnitsByCategory.TryGetValue(key, out var units))
                return units;

            return new List<string>(); // return empty if key not found
        }
    }
}
