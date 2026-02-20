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


        public static async Task ShowToast(string message)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var toast = Toast.Make(message, ToastDuration.Short, 14);
                await toast.Show();
            });
        }
    }
}
