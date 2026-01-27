using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.Views;
using System.Diagnostics;

namespace KusinaPOS.ViewModel
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IDateTimeService? _dateTimeService;
        private readonly SalesService? _salesService;
        private readonly SettingsService? _settingsService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _currentDateTime = string.Empty;

        [ObservableProperty]
        private string _loggedInUserName = string.Empty;

        [ObservableProperty]
        private string _loggedInUserId = string.Empty;

        [ObservableProperty]
        private string storeName = string.Empty;
        [ObservableProperty]
        private string todaySalesTotal;
        [ObservableProperty]
        private string todayTransactionsCount;
        [ObservableProperty]
        private string weeklySalesTotal;
        [ObservableProperty]
        private string weeklyTransactionsCount;

        [ObservableProperty]
        private string appLogo = "kusinaposlogo.png"; // Default immediately

        [ObservableProperty]
        private string appTitle = "Kusina POS"; // Default immediately

        [ObservableProperty]
        private bool isLoading = true;

        public DashboardViewModel(
            IDateTimeService dateTimeService,
            SalesService salesService,
            SettingsService settingsService)
        {
            try
            {
                Debug.WriteLine("DashboardViewModel constructor started");

                _dateTimeService = dateTimeService;
                _salesService = salesService;
                _settingsService = settingsService;

                // Set defaults immediately - no waiting
                CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                StoreName = "Kusina POS";
                AppLogo = "kusinaposlogo.png";
                AppTitle = "Kusina POS";
                TodaySalesTotal = $"₱{_salesService.GetTodaySale(DateTime.Today, DateTime.Today):N2}";
                TodayTransactionsCount = $"Today's Transaction: {_salesService.GetTotalTransactionsCount(DateTime.Today,DateTime.Today)}";
                _=FilterWeekSales();
                Debug.WriteLine("DashboardViewModel constructor completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DashboardViewModel constructor: {ex.Message}");
            }
        }

        /// <summary>
        /// Call this from OnAppearing in the page
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                Debug.WriteLine("DashboardViewModel initialization started");
                IsLoading = true;

                // Load data in background
                await Task.Run(() =>
                {
                    try
                    {
                        // Load preferences safely
                        var storeNamePref = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
                        var userIdPref = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
                        var userNamePref = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);

                        // Get logo path safely
                        var logoPath = _settingsService?.GetStoreLogo ?? "kusinaposlogo.png";
                        var title = _settingsService?.GetAppTitle ?? "Kusina POS";

                        // Update on main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            StoreName = storeNamePref;
                            LoggedInUserId = userIdPref;
                            LoggedInUserName = userNamePref;

                            // Validate logo path exists before setting
                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                            {
                                AppLogo = logoPath;
                            }

                            AppTitle = title;

                            Debug.WriteLine($"Loaded: Store={StoreName}, User={LoggedInUserName}, Logo={AppLogo}");
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading preferences: {ex.Message}");
                    }
                });

                // Subscribe to datetime updates AFTER everything is loaded
                if (_dateTimeService != null)
                {
                    _dateTimeService.DateTimeChanged += OnDateTimeChanged;
                    CurrentDateTime = _dateTimeService.CurrentDateTime;
                    Debug.WriteLine("DateTime service subscribed");
                }

                _isInitialized = true;
                Debug.WriteLine("DashboardViewModel initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnDateTimeChanged(object? sender, string dateTime)
        {
            try
            {
                CurrentDateTime = dateTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating datetime: {ex.Message}");
            }
        }

        /// <summary>
        /// Call this from OnDisappearing in the page
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (_dateTimeService != null)
                {
                    _dateTimeService.DateTimeChanged -= OnDateTimeChanged;
                    Debug.WriteLine("DateTime service unsubscribed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in cleanup: {ex.Message}");
            }
        }

        //===================================
        // Navigation Commands
        //===================================

        [RelayCommand]
        private async Task OpenMenuManagementAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(MenuItemPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenInventoryManagementAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(InventoryItemPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenRecipeManagementAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(MenuItemIngredientsPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenUserManagementAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(UserPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenReportsAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(ReportPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(SettingsPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
        private async Task FilterWeekSales()
        {
            var fromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var toDate = DateTime.Today;
            WeeklyTransactionsCount = $"Weekly Transactions: {_salesService.GetTotalTransactionsCount(fromDate, toDate)}";
            WeeklySalesTotal = $"₱{_salesService.GetTodaySale(fromDate, toDate):N2}";
        }
    }
}