using KusinaPOS.Enums;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace KusinaPOS
{
    public partial class App : Application
    {
        private readonly SettingsViewModel _settingsViewModel;
        private readonly IDatabaseService _databaseService;
        private readonly UserService _userService;
        private bool _isInitialized = false;

        public App(
            IDatabaseService databaseService,
            UserService userService,
            SettingsViewModel settingsViewModel)
        {
            InitializeComponent();

            _settingsViewModel = settingsViewModel;
            _databaseService = databaseService;
            _userService = userService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _ = InitializeAppInBackgroundAsync();     
            return new Window(new AppShell());
        }

        /// <summary>
        /// Initializes the app in background thread to avoid blocking UI
        /// </summary>
        private async Task InitializeAppInBackgroundAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Starting app initialization ===");

                // Run heavy initialization on background thread
                await Task.Run(async () =>
                {
                    try
                    {
                        // Initialize database
                        System.Diagnostics.Debug.WriteLine("Initializing database...");
                        await _databaseService.InitializeAsync();

                        // Initialize user service
                        System.Diagnostics.Debug.WriteLine("Initializing user service...");
                        await _userService.InitializeAsync();

                        System.Diagnostics.Debug.WriteLine("Core initialization completed");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Error during core initialization: {ex.Message}");
                        throw;
                    }
                });

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("=== App initialization completed ===");

                // Run backup check after initialization (also in background)
                _ = CheckAutoBackupAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Fatal error during app initialization: {ex.Message}");

                // Optionally show error to user on main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await PageHelper.DisplayAlertAsync(
                        "Initialization Error",
                        $"Failed to initialize the application. {ex.Message}",
                        "OK");
                });
            }
        }

        /// <summary>
        /// Checks if last backup was more than 24 hours ago.
        /// If yes → auto backup. Runs in background.
        /// </summary>
        private async Task CheckAutoBackupAsync()
        {
            try
            {
                // Ensure app is initialized first
                if (!_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Backup check skipped - app not initialized");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Checking auto-backup...");

                // Run backup check in background
                await Task.Run(async () =>
                {
                    try
                    {
                        long lastBackupTicks = Preferences.Get(
                            DatabaseConstants.LastBackupDateKey, 0L);

                        if (lastBackupTicks == 0)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "First run - creating initial backup");

                            // First run → backup immediately
                            await _settingsViewModel.CreateBackupDatabaseAsync(
                                BackupType.Auto);
                            return;
                        }

                        var lastBackupDate = new DateTime(lastBackupTicks, DateTimeKind.Utc);
                        var hoursSinceLastBackup = (DateTime.UtcNow - lastBackupDate).TotalHours;

                        System.Diagnostics.Debug.WriteLine(
                            $"Hours since last backup: {hoursSinceLastBackup:F2}");

                        if (hoursSinceLastBackup >= 24)
                        {
                            System.Diagnostics.Debug.WriteLine("Creating auto-backup");
                            await _settingsViewModel.CreateBackupDatabaseAsync(
                                BackupType.Auto);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Backup not needed yet");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Error during backup check: {ex.Message}");
                        throw;
                    }
                });

                System.Diagnostics.Debug.WriteLine("Auto-backup check completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Auto-backup failed: {ex.Message}");
                // Don't crash the app if backup fails
            }
        }

        /// <summary>
        /// Optional: Method to wait for initialization if needed
        /// </summary>
        public async Task<bool> WaitForInitializationAsync(int timeoutMs = 10000)
        {
            var startTime = DateTime.UtcNow;

            while (!_isInitialized)
            {
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                {
                    return false;
                }

                await Task.Delay(100);
            }

            return true;
        }
    }
}