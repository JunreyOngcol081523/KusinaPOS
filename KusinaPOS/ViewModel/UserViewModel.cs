using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KusinaPOS.ViewModel
{
    public partial class UserViewModel : ObservableObject
    {
        private readonly UserService _userService;

        public ObservableCollection<User> ActiveUsers { get; } = new();

        //==========================================
        // Editing properties
        //==========================================
        [ObservableProperty] private User editingUser;
        [ObservableProperty] private string name;
        [ObservableProperty] private int pin;
        [ObservableProperty] private int confirmPin;
        [ObservableProperty] private string role;
        [ObservableProperty] private string storeName;

        //==========================================
        // Constructor
        //==========================================
        public UserViewModel(UserService userService)
        {
            _userService = userService;
            StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "KusinaPOS");
        }

        //==========================================
        // Load active users
        //==========================================
        public async Task LoadActiveUsersAsync()
        {
            var users = await _userService.GetUsersAsync();

            ActiveUsers.Clear();
            foreach (var user in users)
                ActiveUsers.Add(user);
        }

        //==========================================
        // Load a specific user by role
        //==========================================
        private async Task LoadUserByRoleAsync(string roleToLoad)
        {
            var user = await _userService.GetUserByRoleAsync(roleToLoad);

            if (user == null)
            {
                // Create new user if not exists
                EditingUser = new User
                {
                    Role = roleToLoad,
                    Name = "",
                };
            }
            else
            {
                EditingUser = user;
            }

            // Bind editing properties
            Name = EditingUser.Name;
            Role = EditingUser.Role;
            Pin = 0; // Reset PIN input
            ConfirmPin = 0;
        }

        [RelayCommand]
        public async Task TapAdminAsync()
        {
            await LoadUserByRoleAsync("Administrator");
        }

        [RelayCommand]
        public async Task TapCashierAsync()
        {
            await LoadUserByRoleAsync("Cashier");
        }

        //==========================================
        // Reset users table
        //==========================================
        [RelayCommand]
        public async Task ResetUsersAsync()
        {
            await LoadActiveUsersAsync();
            await LoadUserByRoleAsync("Administrator"); // reload admin
        }

        //==========================================
        // Save user (name + optional PIN)
        //==========================================
        [RelayCommand]
        public async Task SaveUserAsync()
        {
            try
            {
                if (EditingUser == null)
                    return;

                if (string.IsNullOrWhiteSpace(Name))
                    return;

                if (Pin != 0 && Pin != ConfirmPin)
                {
                    await PageHelper.DisplayAlertAsync("Error", "PIN and Confirm PIN do not match.", "OK");
                    return;
                }

                // Update editing user
                EditingUser.Name = Name.Trim();

                string pinToUpdate = Pin > 0 ? Pin.ToString() : null;

                if (EditingUser.Id == 0)
                {
                    // New user
                    await _userService.InsertUserAsync(EditingUser);

                    if (!string.IsNullOrWhiteSpace(pinToUpdate))
                        await _userService.SaveUserAsync(EditingUser, pinToUpdate);
                }
                else
                {
                    // Existing user
                    await _userService.SaveUserAsync(EditingUser, pinToUpdate);
                }

                await LoadActiveUsersAsync(); // Refresh list
                await PageHelper.DisplayAlertAsync("Success", "User saved successfully.", "OK");

                // Clear PIN fields
                Pin = 0;
                ConfirmPin = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UserViewModel SaveUserAsync Error: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Failed to save user. {ex.Message}", "OK");
            }
        }
    }
}
