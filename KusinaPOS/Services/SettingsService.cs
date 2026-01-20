using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using KusinaPOS.Helpers;

namespace KusinaPOS.Services
{
    public class SettingsService
    {
        public string GetStoreLogo
        {
            get
            {
                // Folder where logos are stored
                string folder = DatabaseConstants.StoreLogoFolder;

                // List of allowed extensions
                string[] extensions = new[] { ".png", ".jpg", ".jpeg" };

                foreach (var ext in extensions)
                {
                    string filePath = Path.Combine(folder, "storelogo" + ext);
                    if (File.Exists(filePath))
                        return filePath;
                }

                // Return null or default image if not found
                return null;
            }
        }

        public void SaveStoreSettings(string storeName, string storeAddress, string storeLogo, Image logoFile)
        {
            // Save settings using Preferences
            Preferences.Set(Helpers.DatabaseConstants.StoreNameKey, storeName);
            Preferences.Set(Helpers.DatabaseConstants.StoreAddressKey, storeAddress);
            //save logo file
            if (logoFile != null && logoFile.Source is FileImageSource fileImageSource)
            {
                var logoFolder = Helpers.DatabaseConstants.StoreLogoFolder;
                if (!Directory.Exists(logoFolder))
                {
                    Directory.CreateDirectory(logoFolder);
                }
                var logoPath = Path.Combine(logoFolder, storeLogo);
                var sourcePath = fileImageSource.File;
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, logoPath, true);
                }
            }
        }
        //load settings
        public static (string storeName, string storeAddress) LoadStoreSettings()
        {
            string storeName = Preferences.Get(Helpers.DatabaseConstants.StoreNameKey, "Kusina POS");
            string storeAddress = Preferences.Get(Helpers.DatabaseConstants.StoreAddressKey, "123 Main St, City, Country");
            return (storeName, storeAddress);
        }
        //load logo file path
        public static string GetStoreLogoPath(string storeLogo)
        {
            if (string.IsNullOrEmpty(storeLogo))
            {
                return string.Empty;
            }
            var logoFolder = Helpers.DatabaseConstants.StoreLogoFolder;
            var logoPath = Path.Combine(logoFolder, storeLogo);
            return logoPath;
        }
    }
}
