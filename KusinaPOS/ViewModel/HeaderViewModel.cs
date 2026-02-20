using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Enums;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using SQLite;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Input;

namespace KusinaPOS.ViewModel
{
    public partial class HeaderViewModel : ObservableObject
    {
        readonly SettingsService _settingsService;
        [ObservableProperty]
        private string appTitle = "KusinaPOS";

        [ObservableProperty]
        private string appLogo = "kusina_logo.png"; // Replace with your actual logo path

        [ObservableProperty]
        private string currentDateTime;

        [ObservableProperty]
        private string loggedInUserName = "Administrator"; // You might fetch this from a UserSession service
        [ObservableProperty] private bool isBusy;
        [ObservableProperty]
        private string _backupLocation = string.Empty;
        // In HeaderViewModel.cs
        public bool IsAdmin => Preferences.Get(DatabaseConstants.UserRoleKey, string.Empty) == "Administrator";
        public HeaderViewModel() : this(IPlatformApplication.Current?.Services.GetService<SettingsService>()) {
        }

        public HeaderViewModel(SettingsService settingsService)
        {
            // Start a timer to update the date/time every second
            CurrentDateTime = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");
            StartClock();
            _settingsService = settingsService;
            _ = InitializeAsync();
            BackupLocation = Preferences.Get(DatabaseConstants.BackupLocationKey, DatabaseConstants.BackupFolder);
        }
        public async Task InitializeAsync()
        {
            

            try
            {
                Debug.WriteLine("DashboardViewModel initialization started");
                

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
                            LoggedInUserName = userNamePref;

                            // Validate logo path exists before setting
                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                            {
                                AppLogo = logoPath;
                            }

                            AppTitle = title;
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading preferences: {ex.Message}");
                    }
                });

                Debug.WriteLine("DashboardViewModel initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
            }
        }
        private void StartClock()
        {
            Dispatcher.GetForCurrentThread().StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                //var userNamePref = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
                //LoggedInUserName = userNamePref;
                CurrentDateTime = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");
                return true; // Keep the timer running
            });
        }


        // 1. SETTINGS NAV
        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            // Close popup logic is handled automatically by SfPopup StaysOpen="False"
            await Shell.Current.GoToAsync("SettingsPage");
        }

        // 2. QUICK BACKUP
        [RelayCommand]
        private async Task BackupDatabaseAsync()
        {
            await CreateBackupDatabaseAsync(Enums.BackupType.Manual);
        }

        // 3. LOGOUT
        [RelayCommand]
        private async Task LogoutAsync()
        {
            bool answer = await PageHelper.DisplayConfirmAsync("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (answer)
            {
                // Navigate to Login Page (Use absolute route ///)
                await Shell.Current.GoToAsync("//MainPage");
            }
        }

        // 4. EXIT / FORCE CLOSE
        [RelayCommand]
        private void ExitApplication()
        {
            Application.Current.Quit();
        }
        public async Task CreateBackupDatabaseAsync(BackupType backupType)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var backupDir = BackupLocation;
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFilePath = Path.Combine(backupDir, $"{backupType}_Backup_{timestamp}.db");

                // Perform backup
                using (var sourceDb = new SQLiteConnection(DatabaseConstants.DatabasePath))
                {
                    sourceDb.Execute($"VACUUM INTO ?", backupFilePath);
                }

                // Cleanup old backups
                CleanupOldBackups(backupDir, maxBackupsToKeep: 10);

                await PageHelper.DisplayAlertAsync(
                        "Quick DB Backup",
                        "Database backup created successfully.",
                        "OK");
                
                //save to preferences date last backup
                Preferences.Set(DatabaseConstants.LastBackupDateKey, DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error backing up database: {ex.Message}");
                await PageHelper.DisplayAlertAsync(
                        "Error",
                        $"Failed to backup database: {ex.Message}",
                        "OK");
                
            }
            finally
            {
                IsBusy = false;
            }

        }
        private void CleanupOldBackups(string backupDirectory, int maxBackupsToKeep)
        {
            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (backupFiles.Count > maxBackupsToKeep)
                {
                    var filesToDelete = backupFiles.Skip(maxBackupsToKeep);

                    foreach (var file in filesToDelete)
                    {
                        file.Delete();
                        Debug.WriteLine($"Deleted old backup: {file.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up old backups: {ex.Message}");
            }
        }
    }
}