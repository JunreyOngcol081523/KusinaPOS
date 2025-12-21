using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace KusinaPOS
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

        public MainViewModel()
        {
            // Initialize colors from resources
            var primaryColor = GetColorFromResource("Primary");
            var gray300Color = GetColorFromResource("Gray300");
            var textSecondaryColor = GetColorFromResource("TextSecondary");

            AdminBorderColor = primaryColor;
            CashierBorderColor = gray300Color;
            SelectedUserTypeLabelColor = textSecondaryColor;
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
        private async Task OnLogin()
        {
            if (string.IsNullOrEmpty(_selectedUserType))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please select a user type", "OK");
                return;
            }

            if (_currentPin.Length < 4)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter a valid PIN (minimum 4 digits)", "OK");
                return;
            }

            // TODO: Implement login logic
            await Application.Current.MainPage.DisplayAlert("Login",
                $"User Type: {_selectedUserType}\nPIN: {_currentPin}", "OK");

            // Clear PIN after login attempt
            OnClearClicked();
        }

        [RelayCommand]
        private async Task OnCreateAccount()
        {
            // TODO: Navigate to create account page
            await Application.Current.MainPage.DisplayAlert("Create Account",
                "Navigate to create account page", "OK");
        }
    }
}