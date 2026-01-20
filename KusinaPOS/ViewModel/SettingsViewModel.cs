using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KusinaPOS.ViewModel
{
    public partial class SettingsViewModel : ObservableObject {

        private SettingsService _settingsService=null;
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
        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
            BackupLocation = Preferences.Get(DatabaseConstants.BackupLocationKey, DatabaseConstants.BackupFolder);
            ImagePath = _settingsService.GetStoreLogo;
            LoadStoreSettings();
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
    }

}
