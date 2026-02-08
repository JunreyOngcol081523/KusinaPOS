using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace KusinaPOS.Helpers
{
    public class AlertHelper
    {
        

        public static void ShowToast(string message)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            ToastDuration duration = ToastDuration.Short;
            double fontSize = 14;
            // Implementation for showing a toast notification
            var toast = Toast.Make(message, duration, fontSize);

            toast.Show(cancellationTokenSource.Token);
        }
    }
}
