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

        public App(
            IDatabaseService databaseService,
            UserService userService,
            SettingsViewModel settingsViewModel)
        {
            InitializeComponent();

            _settingsViewModel = settingsViewModel;

            // Fire-and-forget startup tasks
            _ = InitializeAppAsync(databaseService, userService);
            _ = CheckAutoBackupAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        private async Task InitializeAppAsync(
            IDatabaseService databaseService,
            UserService userService)
        {
            await databaseService.InitializeAsync();
            await userService.InitializeAsync();
        }

        /// <summary>
        /// Checks if last backup was more than 24 hours ago.
        /// If yes → auto backup.
        /// </summary>
        private async Task CheckAutoBackupAsync()
        {
            try
            {
                long lastBackupTicks = Preferences.Get(
                    DatabaseConstants.LastBackupDateKey, 0L);

                if (lastBackupTicks == 0)
                {
                    // First run → backup immediately
                    await _settingsViewModel.CreateBackupDatabaseAsync(BackupType.Auto);
                    return;
                }

                var lastBackupDate = new DateTime(lastBackupTicks, DateTimeKind.Utc);
                var hoursSinceLastBackup = (DateTime.UtcNow - lastBackupDate).TotalHours;

                if (hoursSinceLastBackup >= 24)
                {
                    await _settingsViewModel.CreateBackupDatabaseAsync(BackupType.Auto);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Auto-backup failed: {ex.Message}");
            }
        }
    }
}
