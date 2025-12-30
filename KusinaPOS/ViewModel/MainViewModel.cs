using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.Views;
using System.Windows.Input;

namespace KusinaPOS.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _currentPin = "";

        [ObservableProperty]
        private string _pinDisplay = "------";

        [ObservableProperty]
        private string _selectedUserType = "";

        [ObservableProperty]
        private string _selectedUserTypeLabel = "Select a user type";

        [ObservableProperty]
        private Color _selectedUserTypeLabelColor;

        [ObservableProperty]
        private Color _adminBorderColor;

        [ObservableProperty]
        private Color _cashierBorderColor;
        private readonly UserService _userService;

        [ObservableProperty]
        private string appLogoPath;
        public MainViewModel(UserService userService)
        {
            _userService = userService;
            // Initialize colors from resources
            var primaryColor = GetColorFromResource("Primary");
            var gray300Color = GetColorFromResource("Gray300");
            var textSecondaryColor = GetColorFromResource("TextSecondary");

            AdminBorderColor = gray300Color;
            CashierBorderColor = gray300Color;
            SelectedUserTypeLabelColor = textSecondaryColor;
            LoadAppLogo();
        }

        // Helper method to get color from resources
        private Color GetColorFromResource(string key)
        {
            if (Application.Current.Resources.TryGetValue(key, out var value))
            {
                return (Color)value;
            }
            return Colors.Gray;
        }

        [RelayCommand]
        private void OnNumberClicked(string number)
        {
            if (_currentPin.Length < 6)
            {
                // Validate numeric only
                if (int.TryParse(number, out _))
                {
                    CurrentPin += number;
                    UpdatePinDisplay();
                }
            }
        }

        [RelayCommand]
        private void OnBackspaceClicked()
        {
            if (_currentPin.Length > 0)
            {
                CurrentPin = _currentPin.Substring(0, _currentPin.Length - 1);
                UpdatePinDisplay();
            }
        }

        [RelayCommand]
        private void OnClearClicked()
        {
            CurrentPin = "";
            UpdatePinDisplay();
        }

        private void UpdatePinDisplay()
        {
            // Display dots for entered digits and dashes for remaining
            PinDisplay = new string('●', _currentPin.Length).PadRight(6, '-');
        }

        [RelayCommand]
        private void OnAdministratorTapped()
        {
            SelectedUserType = "Administrator";

            // Highlight Administrator
            AdminBorderColor = GetColorFromResource("Primary");

            // Unhighlight Cashier
            CashierBorderColor = GetColorFromResource("Gray300");

            // Update label
            SelectedUserTypeLabel = "Administrator Selected";
            SelectedUserTypeLabelColor = GetColorFromResource("Primary");
        }

        [RelayCommand]
        private void OnCashierTapped()
        {
            SelectedUserType = "Cashier";

            // Highlight Cashier
            CashierBorderColor = GetColorFromResource("Primary");

            // Unhighlight Administrator
            AdminBorderColor = GetColorFromResource("Gray300");

            // Update label
            SelectedUserTypeLabel = "Cashier Selected";
            SelectedUserTypeLabelColor = GetColorFromResource("Primary");
        }



        [RelayCommand]
        private async Task LoginAsync()
        {
            
            if (string.IsNullOrEmpty(_selectedUserType))
            {
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    "Please select a user type",
                    "OK"
                );
                return;
            }

            if (_currentPin.Length < 6)
            {
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    "Please enter a valid PIN (minimum 6 digits)",
                    "OK"
                );
                return;
            }

            var user = await _userService.LoginWithPinAsync(
                _currentPin,
                _selectedUserType
            );

            if (user == null)
            {
                await PageHelper.DisplayAlertAsync(
                    "Login Failed",
                    $"Invalid PIN or user type",
                    "OK"
                );

                OnClearClicked();
                return;
            }

            // ✅ SUCCESS
            await PageHelper.DisplayAlertAsync(
                "Welcome",
                $"Hello {user.Name}",
                "OK"
            );
            Preferences.Set(DatabaseConstants.LoggedInUserIdKey, user.Id);
            Preferences.Set(DatabaseConstants.LoggedInUserNameKey, user.Name);
            await Shell.Current.GoToAsync(nameof(DashboardPage));

            // Cashier → POS Screen

            OnClearClicked();
        }

        [RelayCommand]
        private async Task OnCreateAccount()
        {

            // TODO: Navigate to create account page
            await PageHelper.DisplayAlertAsync("Create Account",
                "Navigate to create account page", "OK");
        }
        private void LoadAppLogo()
        {
            // Get logo path from Preferences, use default if not set
            AppLogoPath = Preferences.Get("AppLogoPath", "kusinaposlogo.png");
        }
    }
}