using KusinaPOS.Helpers;
using KusinaPOS.ViewModel;
using System.Diagnostics;

namespace KusinaPOS.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _viewModel;
        private bool _isInitialized = false;

        public SettingsPage(SettingsViewModel vm)
        {
            try
            {
                Debug.WriteLine("SettingsPage constructor started");

                InitializeComponent();

                _viewModel = vm;
                BindingContext = _viewModel;

                Debug.WriteLine("SettingsPage constructor completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SettingsPage constructor: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Debug.WriteLine("SettingsPage OnAppearing started");

            if (!_isInitialized)
            {
                await InitializePageAsync();
                _isInitialized = true;
            }
        }

        private async Task InitializePageAsync()
        {
            try
            {
                Debug.WriteLine("Initializing settings page...");

                // Small delay to let page render
                await Task.Delay(100);

                // Initialize WebView safely (if needed)
                await InitializeWebViewAsync();

                // Load backups - don't use Task.Run, just await directly
                await _viewModel.LoadBackupsAsync();

                Debug.WriteLine("Settings page initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing settings page: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Failed to load settings: {ex.Message}", "OK");
            }
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                // Find WebView by name if it exists
                var webView = this.FindByName<WebView>("AboutWebView");

                if (webView != null)
                {
                    Debug.WriteLine("WebView found, waiting for HTML to load...");

                    // Small delay to ensure HTML is set
                    await Task.Delay(200);

                    // Verify HTML source is loaded
                    if (!string.IsNullOrEmpty(_viewModel.HtmlSource))
                    {
                        Debug.WriteLine($"HTML source length: {_viewModel.HtmlSource.Length} characters");

                        // Force refresh WebView on main thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            var htmlSource = new HtmlWebViewSource
                            {
                                Html = _viewModel.HtmlSource
                            };
                            webView.Source = htmlSource;
                            Debug.WriteLine("WebView source set successfully");
                        });
                    }
                    else
                    {
                        Debug.WriteLine("WARNING: HtmlSource is empty!");
                    }
                }
                else
                {
                    Debug.WriteLine("WebView 'AboutWebView' not found in XAML");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing WebView: {ex.Message}");
                // Don't crash the page if WebView fails
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                Debug.WriteLine("SettingsPage OnDisappearing");

                // Clean up WebView if it exists
                var webView = this.FindByName<WebView>("AboutWebView");
                if (webView != null)
                {
                    webView.Source = null;
                    Debug.WriteLine("WebView source cleared");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDisappearing: {ex.Message}");
            }
        }
    }
}