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

        private readonly SettingsService? _settingsService=null;
        private readonly IDateTimeService? _dateTimeService;
        private readonly CategoryService? _categoryService;
        private readonly MenuItemService? _menuItemService;
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
        [ObservableProperty]
        private ObservableCollection<Category> categories = new();
        [ObservableProperty] private string newCategoryName;
        [ObservableProperty] private Category selectedCategory;
        public SettingsViewModel(SettingsService settingsService , 
            IDateTimeService dateTimeService, 
            CategoryService categoryService, 
            MenuItemService menuItemService)
        {

            
            try
            {
                _settingsService = settingsService;
                _categoryService = categoryService;
                _menuItemService = menuItemService;
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
                LoadCategories();
                LoadPOSSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MenuItemViewModel constructor: {ex.Message}");
            }
        }
        private async void LoadCategories()
        {
            var categoryList = await _categoryService.GetAllCategoriesAsync();
            var menuItems = await _menuItemService.GetAllMenuItemsAsync();

            foreach (var category in categoryList)
            {
                category.NumberOfMenuUnderThisCategory = menuItems.Count(m => m.Category == category.Name);
            }

            Categories = new ObservableCollection<Category>(categoryList);
        }
        [RelayCommand]
        public async Task AddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                await PageHelper.DisplayAlertAsync("Validation", "Enter category name.", "OK");
                return;
            }

            // Check for duplicates
            if (Categories.Any(c => c.Name.Equals(NewCategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                await PageHelper.DisplayAlertAsync("Validation", "Category already exists.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // Add to database
                await _categoryService.AddCategoryAsync(NewCategoryName.Trim());

                // Reload categories
                LoadCategories();
                await PageHelper.DisplayAlertAsync("Success", "Category added.", "OK");
                // Clear input
                NewCategoryName = string.Empty;

                // Optionally, select the newly added category
                SelectedCategory = Categories.LastOrDefault();
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to add category: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        //delete category where there are no menu items under this category
        [RelayCommand]
        public async Task DeleteCategoryAsync(Category category)
        {
            if (category == null) return;
            var menuItems = await _menuItemService.GetMenuItemsByCategoryAsync(category.Name);
            if (menuItems.Any())
            {
                await PageHelper.DisplayAlertAsync("Cannot Delete", "There are menu items under this category. Please delete them first or edit its category", "OK");
                return;
            }
            bool confirm = await PageHelper.DisplayConfirmAsync("Confirm Delete",
                $"Are you sure you want to delete the category '{category.Name}'?",
                "Delete", "Cancel");
            if (!confirm) return;
            try
            {
                IsBusy = true;
                await _categoryService.DeleteCategoryAsync(category);
                LoadCategories();
                await PageHelper.DisplayAlertAsync("Success", "Category deleted.", "OK");
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", $"Failed to delete category: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
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

        #region database settings
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
                Preferences.Set(DatabaseConstants.LastBackupDateKey, DateTime.UtcNow.Ticks);
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
        #endregion
        //========================POS SETTINGS========================//
        //load preferences
        public void LoadPOSSettings()
        {
            // Load Discount Settings
            AllowDiscount = Preferences.Get(SettingsConstants.AllowDiscountKey, false);
            IsDiscountFixedAmount = Preferences.Get(SettingsConstants.IsDiscountFixedAmountKey, true);
            IsDiscountPercentage = Preferences.Get(SettingsConstants.IsDiscountPercentageKey, false);
            DiscountValue = Preferences.Get(SettingsConstants.DiscountValueKey, string.Empty);
            // Load VAT Settings
            AllowVAT = Preferences.Get(SettingsConstants.AllowVATKey, false);
            VatValue = Preferences.Get(SettingsConstants.VATValueKey, string.Empty);
            // Load Printer Settings
            AllowPrint = Preferences.Get(SettingsConstants.AllowPrintKey, false);
            SelectedPrinter = Preferences.Get(SettingsConstants.SelectedPrinterKey, string.Empty);
        }
        // Discount Settings
        [ObservableProperty]
        private bool allowDiscount;

        partial void OnAllowDiscountChanged(bool value)
        {
            // Trigger when allowDiscount changes
             Preferences.Set(SettingsConstants.AllowDiscountKey, value);
            if (!value)
            {
                // Clear discount when disabled
                DiscountValue = string.Empty;
            }
        }

        [ObservableProperty]
        private bool isDiscountFixedAmount = true;

        partial void OnIsDiscountFixedAmountChanged(bool value)
        {
            Preferences.Set(SettingsConstants.IsDiscountFixedAmountKey, value);
            // Trigger when discount type changes to fixed amount
            if (value)
            {
                IsDiscountPercentage = false;
            }
        }

        [ObservableProperty]
        private bool isDiscountPercentage;

        partial void OnIsDiscountPercentageChanged(bool value)
        {
            Preferences.Set(SettingsConstants.IsDiscountPercentageKey, value);
            // Trigger when discount type changes to percentage
            if (value)
            {
                IsDiscountFixedAmount = false;
            }
        }

        [ObservableProperty]
        private string discountValue;

        partial void OnDiscountValueChanged(string value)
        {
            Preferences.Set(SettingsConstants.DiscountValueKey, value);
        }

        // VAT Settings
        [ObservableProperty]
        private bool allowVAT;

        partial void OnAllowVATChanged(bool value)
        {
            Preferences.Set(SettingsConstants.AllowVATKey, value);
            if (!value)
            {
                // Reset VAT settings when disabled
            }
        }

        [ObservableProperty]
        private string vatValue;
        partial void OnVatValueChanged(string value)
        {
            Preferences.Set(SettingsConstants.VATValueKey, value);
        }
        // Printer Settings
        [ObservableProperty]
        private bool allowPrint;

        partial void OnAllowPrintChanged(bool value)
        {
            Preferences.Set(SettingsConstants.AllowPrintKey, value);
            if (!value)
            {
                // Disconnect printer or clear printer settings
            }
        }

        [ObservableProperty]
        private ObservableCollection<string> availablePrinters = new();

        partial void OnAvailablePrintersChanged(ObservableCollection<string> value)
        {
            // Trigger when available printers list changes
        }

        [ObservableProperty]
        private string selectedPrinter;

        partial void OnSelectedPrinterChanged(string value)
        {
            // Trigger when selected printer changes
            Preferences.Set(SettingsConstants.SelectedPrinterKey, value);
            if (!string.IsNullOrEmpty(value))
            {
                PrinterConnectionStatus = "Printer selected, ready to connect";
                PrinterConnectionStatusColor = Colors.Blue;
            }
        }

        [ObservableProperty]
        private string printerConnectionStatus = "Not connected";

        partial void OnPrinterConnectionStatusChanged(string value)
        {
            // Trigger when connection status changes
        }

        [ObservableProperty]
        private Color printerConnectionStatusColor = Colors.Gray;

        partial void OnPrinterConnectionStatusColorChanged(Color value)
        {
            // Trigger when connection status color changes
        }

        // Commands
        [RelayCommand]
        private async Task FindPrinter()
        {
            // Your bluetooth printer discovery logic
            AvailablePrinters.Clear();
            PrinterConnectionStatus = "Searching for printers...";
            PrinterConnectionStatusColor = Colors.Orange;

            // Add found printers to AvailablePrinters
            // Simulate finding printers
            AvailablePrinters.Add("Thermal Printer 1");
            AvailablePrinters.Add("Bluetooth Printer 2");

            PrinterConnectionStatus = $"Found {AvailablePrinters.Count} printer(s)";
            PrinterConnectionStatusColor = Colors.Blue;
        }

        [RelayCommand]
        private async Task ConnectPrinter()
        {
            if (SelectedPrinter == null)
            {
                PrinterConnectionStatus = "Please select a printer";
                PrinterConnectionStatusColor = Colors.Orange;
                return;
            }

            PrinterConnectionStatus = "Connecting...";
            PrinterConnectionStatusColor = Colors.Orange;

            // Your connection logic here
            await Task.Delay(1000); // Simulate connection delay

            // On success:
            PrinterConnectionStatus = "Connected successfully";
            PrinterConnectionStatusColor = Colors.Green;

            // On failure:
            // PrinterConnectionStatus = "Connection failed";
            // PrinterConnectionStatusColor = Colors.Red;
        }
    }

}
