using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Helpers
{
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Graphics;
    using System.Collections.Generic;


        public static class ChartThemeHelper
        {
            // Static property to access the brushes directly
            public static List<Brush> AuditorPalette { get; } = GenerateAuditorPalette();

            private static List<Brush> GenerateAuditorPalette()
            {
                var palette = new List<Brush>();

                // Gradient 1: Peach (Warm & Inviting)
                var peach = new LinearGradientBrush();
                peach.GradientStops.Add(new GradientStop(Color.FromRgb(252, 182, 159), 0));
                peach.GradientStops.Add(new GradientStop(Color.FromRgb(255, 231, 199), 1));
                palette.Add(peach);

                // Gradient 2: Gold (High Value/Warning)
                var gold = new LinearGradientBrush();
                gold.GradientStops.Add(new GradientStop(Color.FromRgb(252, 204, 45), 0));
                gold.GradientStops.Add(new GradientStop(Color.FromRgb(250, 221, 125), 1));
                palette.Add(gold);

                // Gradient 3: Repeat Peach (Or add a Green for "Profit")
                var profit = new LinearGradientBrush();
                profit.GradientStops.Add(new GradientStop(Color.FromRgb(39, 174, 96), 0)); // Green
                profit.GradientStops.Add(new GradientStop(Color.FromRgb(46, 204, 113), 1));
                palette.Add(profit);

                // Gradient 4: Purple (Neutral/Other)
                var purple = new LinearGradientBrush();
                purple.GradientStops.Add(new GradientStop(Color.FromRgb(250, 172, 168), 0));
                purple.GradientStops.Add(new GradientStop(Color.FromRgb(221, 214, 243), 1));
                palette.Add(purple);

                // Gradient 5: Blue (Cold/Frozen)
                var blue = new LinearGradientBrush();
                blue.GradientStops.Add(new GradientStop(Color.FromRgb(123, 176, 249), 0));
                blue.GradientStops.Add(new GradientStop(Color.FromRgb(168, 234, 238), 1));
                palette.Add(blue);

                return palette;
            }
        }
    
}
