using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using KusinaPOS.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace KusinaPOS.ViewModel
{
    public partial class DashboardViewModel : ObservableObject
    {
        #region Fields

        private readonly IDateTimeService? _dateTimeService;
        private readonly SalesService? _salesService;
        private readonly InventoryItemService? _inventoryItemService;
        private readonly SettingsService? _settingsService;
        private bool _isInitialized = false;

        #endregion

        #region Properties

        // In your ViewModel
        public List<Brush> CustomBrushes => ChartThemeHelper.AuditorPalette;

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
        [ObservableProperty]
        private int totalVoidedTransactions;
        [ObservableProperty]
        private int totalRefundedTransactions;
        [ObservableProperty]
        private int totalLowStocksItems;

        // 1. Property for the Chart
        public ObservableCollection<SalesChartDto> SalesChartData { get; set; } = new ObservableCollection<SalesChartDto>();

        // 2. Command to handle Radio Button selection
        public ICommand FilterChartCommand { get; private set; }

        #endregion

        #region Constructor

        public DashboardViewModel(
            IDateTimeService dateTimeService,
            SalesService salesService,
            SettingsService settingsService,
            InventoryItemService? inventoryItemService)
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

                _ = LoadSales();
                FilterChartCommand = new Command<string>(async (type) => await LoadSalesChartAsync(type));
                // Load default (e.g., "Daily")
                LoadSalesChartAsync("Daily");
                Debug.WriteLine("DashboardViewModel constructor completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DashboardViewModel constructor: {ex.Message}");
            }

            _inventoryItemService = inventoryItemService;
        }

        #endregion

        #region Initialization and Cleanup

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
                        // Use 'await' to extract the actual decimal/int from the Task
                        // Get logo path safely
                        var logoPath = _settingsService?.GetStoreLogo ?? "kusinaposlogo.png";
                        var title = _settingsService?.GetAppTitle ?? "Kusina POS";

                        // Update on main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {


                            Debug.WriteLine($"todaysales: {TodaySalesTotal}");
                            Debug.WriteLine($"todaysalestransaction: {TodayTransactionsCount}");
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

        #endregion

        #region Data Loading

        private async Task LoadSales()
        {
            var fromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var toDate = DateTime.Today;

            // 1. Await the Transaction Counts (Don't forget the await!)
            var weeklyCount = await _salesService.GetTotalTransactionsCount(fromDate, toDate);
            var todayCount = await _salesService.GetTotalTransactionsCount(DateTime.Today, DateTime.Today);

            // 2. Await the Net Sales
            var todaysls = await _salesService.GetNetSalesAsync(DateTime.Today, DateTime.Today);
            var weeksls = await _salesService.GetNetSalesAsync(fromDate, toDate);

            // 3. Update the UI Strings
            WeeklyTransactionsCount = $"Weekly Transactions: {weeklyCount}";
            TodayTransactionsCount = $"Today's Transaction: {todayCount}";

            TodaySalesTotal = $"₱{todaysls:N2}";
            WeeklySalesTotal = $"₱{weeksls:N2}";
            TotalVoidedTransactions = await _salesService.GetSalesCountByStatusAsync("Voided");
            TotalRefundedTransactions = await _salesService.GetSalesCountByStatusAsync("Refunded");
            var allitems = await _inventoryItemService.GetAllInventoryItemsAsync();
            var lowStock = allitems.Where(i => i.IsLowStock).ToList();
            var lowStockList = new ObservableCollection<InventoryItem>(lowStock);
            TotalLowStocksItems = lowStockList.Count;
        }

        public async Task LoadSalesChartAsync(string filterType)
        {
            try
            {
                // 1. SMART DATE LOGIC
                DateTime queryStart = DateTime.Today;
                DateTime queryEnd = DateTime.Today.AddDays(1).AddTicks(-1);

                switch (filterType)
                {
                    case "Hourly":
                        // Logic: Show TODAY'S performance
                        queryStart = DateTime.Today;
                        queryEnd = DateTime.Today.AddDays(1).AddTicks(-1);
                        break;

                    case "Daily":
                        // Logic: Show THIS WEEK (Mon-Sun)
                        var today = DateTime.Today;
                        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                        queryStart = today.AddDays(-1 * diff).Date;
                        queryEnd = queryStart.AddDays(7).AddTicks(-1);
                        break;

                    case "Weekly":
                        // Logic: Show THIS MONTH
                        queryStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        queryEnd = queryStart.AddMonths(1).AddTicks(-1);
                        break;

                    case "Monthly":
                        // Logic: Show THIS YEAR
                        queryStart = new DateTime(DateTime.Today.Year, 1, 1);
                        queryEnd = new DateTime(DateTime.Today.Year, 12, 31);
                        break;
                }

                // 2. FETCH DATA
                var allSales = await _salesService.GetSalesByDateRangeAsync(queryStart, queryEnd);
                IEnumerable<SalesChartDto> processedData = new List<SalesChartDto>();

                // 3. GROUP DATA (Updated Hourly Logic)
                switch (filterType)
                {
                    case "Hourly":
                        TimeBracket timeBracket = new TimeBracket();
                        // --- NEW BRACKET LOGIC ---
                        processedData = allSales
                            // Step A: Tag every sale with a Bracket
                            .Select(x => new
                            {
                                Sale = x,
                                Bracket = timeBracket.GetTimeBracket(x.SaleDate.Hour)
                            })
                            // Step B: Group by Bracket ID (so they sort correctly: Breakfast -> Lunch -> Dinner)
                            .GroupBy(x => x.Bracket.Id)
                            .OrderBy(g => g.Key)
                            // Step C: Create the Chart Data
                            .Select(g => new SalesChartDto
                            {
                                // Use the label from the first item (e.g., "Lunch Rush")
                                Argument = g.First().Bracket.Label,
                                Value = g.Sum(x => x.Sale.TotalAmount)
                            });
                        break;

                    case "Daily":
                        processedData = allSales
                            .GroupBy(x => x.SaleDate.DayOfWeek)
                            .OrderBy(g => ((int)g.Key + 6) % 7) // Sort Mon(0) to Sun(6)
                            .Select(g => new SalesChartDto
                            {
                                Argument = g.Key.ToString().Substring(0, 3),
                                Value = g.Sum(x => x.TotalAmount)
                            });
                        break;

                    case "Weekly":
                        processedData = allSales
                            .GroupBy(x => GetWeekOfMonth(x.SaleDate))
                            .OrderBy(g => g.Key)
                            .Select(g => new SalesChartDto
                            {
                                Argument = $"Week {g.Key}",
                                Value = g.Sum(x => x.TotalAmount)
                            });
                        break;

                    case "Monthly":
                        processedData = allSales
                            .GroupBy(x => x.SaleDate.Month)
                            .OrderBy(g => g.Key)
                            .Select(g => new SalesChartDto
                            {
                                Argument = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                                Value = g.Sum(x => x.TotalAmount)
                            });
                        break;
                }

                // 4. UPDATE UI
                SalesChartData.Clear();
                foreach (var item in processedData)
                {
                    SalesChartData.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error chart: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

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

        #endregion

        #region Navigation Commands

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

        #endregion

        #region Helper Methods

        // Helper method for "Weekly" view
        private int GetWeekOfMonth(DateTime date)
        {
            DateTime beginningOfMonth = new DateTime(date.Year, date.Month, 1);
            // This is a simple calculation; for strict calendar weeks, logic is more complex.
            // This basically divides the day of the month by 7.
            return (date.Day - 1) / 7 + 1;
        }

        #endregion
    }
}