using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Enums;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using KusinaPOS.Views;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace KusinaPOS.ViewModel
{
    public partial class SettingsViewModel : ObservableObject {

        private SettingsService _settingsService=null;
        private readonly IDateTimeService? _dateTimeService;
        //===================================
        // Observable Properties
        //===================================
        [ObservableProperty]
        private string _backupLocation = string.Empty;
        [ObservableProperty]
        private string _storeName = string.Empty;
        [ObservableProperty]
        private string _storeAddress = string.Empty;
        [ObservableProperty]
        private string _storeLogo= string.Empty;
        [ObservableProperty]
        private Image _logoFile = null;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StoreImageSource))]
        [NotifyPropertyChangedFor(nameof(ImageLabel))]
        private string imagePath;
        [ObservableProperty]
        private string _htmlSource = string.Empty;
        // Date & Time
        [ObservableProperty]
        private string _currentDateTime;

        // User info
        [ObservableProperty]
        private string loggedInUserName = string.Empty;
        [ObservableProperty]
        private string loggedInUserId = string.Empty;
        //===================================database properties===================================

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;
        [ObservableProperty]
        private ObservableCollection<DBBackupInfo> backups;
        public SettingsViewModel(SettingsService settingsService , IDateTimeService dateTimeService)
        {

            
            try
            {
                _settingsService = settingsService;
                BackupLocation = Preferences.Get(DatabaseConstants.BackupLocationKey, DatabaseConstants.BackupFolder);
                ImagePath = _settingsService.GetStoreLogo;

                Backups = new ObservableCollection<DBBackupInfo>();
                LoggedInUserId = Preferences.Get(DatabaseConstants.LoggedInUserIdKey, 0).ToString();
                LoggedInUserName = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, string.Empty);
                StoreName = Preferences.Get(DatabaseConstants.StoreNameKey, "Kusina POS");
                _dateTimeService = dateTimeService;
                _dateTimeService.DateTimeChanged += OnDateTimeChanged;
                CurrentDateTime = _dateTimeService.CurrentDateTime;

                LoadStoreSettings();
                LoadAboutHtml();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MenuItemViewModel constructor: {ex.Message}");
            }
        }
        private void OnDateTimeChanged(object? sender, string dateTime)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CurrentDateTime = dateTime;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDateTimeChanged: {ex.Message}");
            }
        }
        public async void LoadAboutHtml()
        {
            try
            {
                // Load from Resources/Raw/about.html
                using var stream = await FileSystem.OpenAppPackageFileAsync("about.html");
                using var reader = new StreamReader(stream);
                var htmlContent = await reader.ReadToEndAsync();

                HtmlSource = htmlContent;

                Debug.WriteLine("HTML loaded from file successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading HTML from file: {ex.Message}");

                // Fallback to inline HTML
                HtmlSource = @"<html><body style='font-family: Arial; padding: 20px;'>
                                <h1>Kusina POS</h1>
                                <p>Version 1.0.0</p>
                                <h2>Features:</h2>
                                <ul>
                                    <li>Menu Management</li>
                                    <li>Inventory Tracking</li>
                                    <li>Sales Reports</li>
                                </ul>
                            </body></html>";
            }
        }
        public string ImageLabel => string.IsNullOrWhiteSpace(ImagePath) ? "Click to upload" : Path.GetFileName(ImagePath);
        public ImageSource StoreImageSource
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ImagePath))
                        return "kusinaposlogo.png";

                    if (!File.Exists(ImagePath))
                        return "kusinaposlogo.png";

                    return ImageSource.FromFile(ImagePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading menu image: {ex.Message}");
                    return "kusinaposlogo.png";
                }
            }
        }
        [RelayCommand]
        private async Task SaveStoreSettingsAsync()
        {
            if (_settingsService != null)
            {
                _settingsService.SaveStoreSettings(StoreName, StoreAddress);
                await PageHelper.DisplayAlertAsync("Success", "Store settings saved successfully.", "OK");
            }
        }
        [RelayCommand]
        public async Task UploadImageAsync()
        {
            try
            {


                var results = await MediaPicker.PickPhotosAsync(new MediaPickerOptions { Title = "Upload Store Logo", SelectionLimit = 1 });
                var result = results?.FirstOrDefault();
                if (result == null) return;

                var imagesDir = DatabaseConstants.StoreLogoFolder;
                if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);

                var fileName = $"storelogo{Path.GetExtension(result.FileName)}";
                var destinationPath = Path.Combine(imagesDir, fileName);

                using var sourceStream = await result.OpenReadAsync();
                using var destinationStream = File.Create(destinationPath);
                await sourceStream.CopyToAsync(destinationStream);

                if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
                    File.Delete(ImagePath);

                ImagePath = destinationPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uploading image: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error", $"Image upload failed: {ex.Message}", "OK");
            }
        }
        //load store settings
        public void LoadStoreSettings()
        {
            var (storeName, storeAddress) = SettingsService.LoadStoreSettings();
            StoreName = storeName;
            StoreAddress = storeAddress;
        }

        //===================================DATABASE SETTINGS===================================
        // backup database
        [RelayCommand]
        public async Task BackupDatabaseAsync()
        {
            await CreateBackupDatabaseAsync(BackupType.Manual);
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

                await PageHelper.DisplayAlertAsync("Success",
                    $"Database backed up successfully.", "OK");
                //save to preferences date last backup
                Preferences.Set(DatabaseConstants.LastBackupDateKey,DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error backing up database: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to backup database: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadBackupsAsync();
                });
            }

        }

        [RelayCommand]
        public async Task LoadBackupsAsync()
        {
            try
            {
                IsBusy = true;

                var backupDir = BackupLocation;
                Debug.WriteLine($"Loading backups from: {backupDir}");

                if (!Directory.Exists(backupDir))
                {
                    Debug.WriteLine("Backup directory does not exist!");
                    return;
                }

                var files = Directory.GetFiles(backupDir, "*.db");
                Debug.WriteLine($"Found {files.Length} backup files");

                var backupFiles = files
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => new DBBackupInfo(f))
                    .ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Backups.Clear();
                    foreach (var backup in backupFiles)
                    {
                        Backups.Add(backup);
                    }
                });

                Debug.WriteLine($"Loaded {Backups.Count} backups into collection");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading backups: {ex.Message}");
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to load backups: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ShareBackupAsync(DBBackupInfo backup)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Database Backup",
                    File = new ShareFile(backup.FilePath)
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sharing backup: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to share backup: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        [RelayCommand]
        public async Task RestoreBackupAsync(DBBackupInfo backup)
        {
            if (IsBusy) return;

            bool confirm = await PageHelper.DisplayConfirmAsync("Confirm Restore",
                $"Restore database from:\n\n{backup.FileName}\n{backup.FormattedDate} at {backup.FormattedTime}\n\n⚠️ This will replace your current database!",
                "Restore", "Cancel");

            if (!confirm) return;

            try
            {
                IsBusy = true;

                var dbPath = DatabaseConstants.DatabasePath;

                // Close database connections
                // Add your database close logic here

                File.Copy(backup.FilePath, dbPath, overwrite: true);

                await PageHelper.DisplayAlertAsync("Success",
                    "Database restored successfully!\n\nPlease restart the app for changes to take effect.",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restoring backup: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to restore backup: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        [RelayCommand]
        public async Task DeleteBackupAsync(DBBackupInfo backup)
        {
            if (IsBusy) return;

            bool confirm = await PageHelper.DisplayConfirmAsync("Confirm Delete",
                $"Delete this backup?\n\n{backup.FileName}\n{backup.FormattedDate} at {backup.FormattedTime}",
                "Delete", "Cancel");

            if (!confirm) return;

            try
            {
                IsBusy = true;

                File.Delete(backup.FilePath);
                Backups.Remove(backup);

                await PageHelper.DisplayAlertAsync("Success",
                    "Backup deleted successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting backup: {ex.Message}");
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to delete backup: {ex.Message}", "OK");
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

        [RelayCommand]
        public async Task RefreshBackupsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsRefreshing = true;
                await LoadBackupsAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }
        [RelayCommand]
        public async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating back: {ex.Message}");
            }
        }
    }

}
