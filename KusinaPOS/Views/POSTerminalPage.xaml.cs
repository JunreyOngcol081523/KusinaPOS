using KusinaPOS.Helpers;
using KusinaPOS.ViewModel;
using System.Diagnostics;

namespace KusinaPOS.Views
{
    public partial class POSTerminalPage : ContentPage
    {
        private readonly POSTerminalViewModel? _viewModel;
        private bool _hasAppeared = false;

        public POSTerminalPage(POSTerminalViewModel vm)
        {
            try
            {
                Debug.WriteLine("POSTerminalPage constructor started");

                InitializeComponent();

                _viewModel = vm;
                BindingContext = _viewModel;

                Debug.WriteLine("POSTerminalPage constructor completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in POSTerminalPage constructor: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Debug.WriteLine("POSTerminalPage OnAppearing started");

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
                Debug.WriteLine("Initializing POS terminal page...");

                // Small delay to let page render
                await Task.Delay(100);

                // Initialize ViewModel data
                await _viewModel.InitializeAsync();

                Debug.WriteLine("POS terminal page initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing POS terminal page: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Failed to load POS terminal: {ex.Message}", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                Debug.WriteLine("POSTerminalPage OnDisappearing");
                _viewModel?.Cleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDisappearing: {ex.Message}");
            }
        }
        protected override bool OnBackButtonPressed()
        {
            // Return true to "consume" the event (stop the back action)
            // Return false to allow the OS to go back
            return true;
        }
    }
}