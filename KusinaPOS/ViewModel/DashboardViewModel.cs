using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // For MainThread
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.Views;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace KusinaPOS.ViewModel
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IDateTimeService? _dateTimeService;
        private readonly SalesService? _salesService;
        private readonly SettingsService? _settingsService;
        [ObservableProperty]
        private string _currentDateTime;
        [ObservableProperty]
        private string _loggedInUserName;
        [ObservableProperty]
        private string _loggedInUserId;
        [ObservableProperty]
        private string storeName;
        [ObservableProperty]
        private string appLogo;
        [ObservableProperty]
        private string appTitle;
        public DashboardViewModel(IDateTimeService dateTimeService, SalesService salesService, SettingsService settingsService)
        {
            _dateTimeService = dateTimeService;
            _salesService = salesService;
            _settingsService = settingsService;
            // Subscribe to updates
            _dateTimeService.DateTimeChanged += OnDateTimeChanged;
            CurrentDateTime = _dateTimeService.CurrentDateTime;
            this.StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
            // Load user info
            LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
            LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
            AppLogo = _settingsService.GetStoreLogo ?? "kusinaposlogo.png";
            AppTitle = _settingsService.GetAppTitle ?? "Kusina POS";
        }

        private void OnDateTimeChanged(object? sender, string dateTime)
        {
            CurrentDateTime = dateTime;
        }

        ~DashboardViewModel()
        {
            _dateTimeService.DateTimeChanged -= OnDateTimeChanged;
        }


        //actions
        //OpenMenuManagementCommand
        [RelayCommand]
        private async Task OpenMenuManagementAsync()
        {
            await Shell.Current.GoToAsync(nameof(MenuItemPage));
        }
        [RelayCommand]
        private async Task OpenInventoryManagementAsync()
        {
            await Shell.Current.GoToAsync(nameof(InventoryItemPage));
        }
        [RelayCommand]
        private async Task OpenRecipeManagementAsync()
        {
            await Shell.Current.GoToAsync(nameof(MenuItemIngredientsPage));
        }
        [RelayCommand]
        private async Task OpenUserManagementAsync()
        {
            await Shell.Current.GoToAsync(nameof(UserPage));
        }
        [RelayCommand]
        private async Task OpenReportsAsync()
        {
            await PageHelper.DisplayAlertAsync("Info", "Reports module is under development.", "OK");
            await _salesService.PrintSalesWithItems();
        }
        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
    }
}
