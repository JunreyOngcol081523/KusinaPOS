using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // For MainThread
using KusinaPOS.Helpers;
using KusinaPOS.Views;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace KusinaPOS.ViewModel
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly Timer _timer;
        [ObservableProperty]
        private string _currentDateTime;
        [ObservableProperty]
        private string _loggedInUserName;
        [ObservableProperty]
        private string _loggedInUserId;
        public DashboardViewModel()
        {
            UpdateDateTime();
            _timer = new Timer(_ => UpdateDateTime(), null, 0, 1000); // 1 second
            LoggedInUserId = Preferences.Get("LoggedInUserId", string.Empty);
            LoggedInUserName = Preferences.Get("LoggedInUserName", string.Empty);
        }

        private void UpdateDateTime()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentDateTime = DateTime.Now.ToString("MMMM dd, yyyy - hh:mm tt");
            });
        }
        public void Dispose()
        {
            _timer?.Dispose();
        }

        //actions
        //OpenMenuManagementCommand
        [RelayCommand]
        private async Task OpenMenuManagementAsync()
        {
            await Shell.Current.GoToAsync(nameof(MenuItemPage));
        }
    }
}
