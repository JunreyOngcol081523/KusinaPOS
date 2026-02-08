using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using Syncfusion.Maui.Buttons;
using System.Diagnostics;

namespace KusinaPOS.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;
        private readonly IDateTimeService _dateTimeService;
        private bool _hasAppeared = false;

        public DashboardPage(
            DashboardViewModel vm,
            IDateTimeService dts)
        {
            try
            {
                Debug.WriteLine("DashboardPage constructor started");

                InitializeComponent();

                _viewModel = vm;
                _dateTimeService = dts;
                BindingContext = _viewModel;

                Debug.WriteLine("DashboardPage constructor completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL: DashboardPage constructor failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Create emergency fallback UI
                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    Children =
                    {
                        new Label
                        {
                            Text = "Error Loading Dashboard",
                            FontSize = 20,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = ex.Message,
                            FontSize = 14,
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                };
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Debug.WriteLine("DashboardPage OnAppearing started");

            if (!_hasAppeared)
            {
                _hasAppeared = true;
                await InitializePageAsync();
            }
        }

        private async Task InitializePageAsync()
        {
            try
            {
                Debug.WriteLine("Initializing dashboard page...");

                // Small delay to let page render
                await Task.Delay(100);

                // Initialize ViewModel
                await _viewModel.InitializeAsync();

                Debug.WriteLine("Dashboard page initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing dashboard: {ex.Message}");
                AlertHelper.ShowToast($"Dashboard Initialization Failed{ ex.Message}");
            }
        }
        private async void OnFilterChanged(object sender, StateChangedEventArgs e)
        {
            // 1. Only react when a button is CHECKED (ignore the uncheck event)
            if (e.IsChecked.HasValue && e.IsChecked.Value)
            {
                var radioButton = sender as SfRadioButton;
                if (radioButton == null) return;

                // 2. Get the ViewModel
                var viewModel = BindingContext as DashboardViewModel;

                // 3. Call your existing logic with the text (Hourly, Daily, etc.)
                if (viewModel != null)
                {
                    await viewModel.LoadSalesChartAsync(radioButton.Text);
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                Debug.WriteLine("DashboardPage OnDisappearing");
                _viewModel?.Cleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDisappearing: {ex.Message}");
            }
        }
    }
}