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

        public ImageSource StoreLogoImage
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(GetStoreLogo) || !File.Exists(GetStoreLogo))
                        return ImageSource.FromFile("kusinaposlogo.png");

                    return ImageSource.FromFile(GetStoreLogo);
                }
                catch
                {
                    return ImageSource.FromFile("kusinaposlogo.png");
                }
            }
        }
        public void SaveStoreSettings(string storeName, string storeAddress)
        {
            // Save settings using Preferences
            Preferences.Set(Helpers.DatabaseConstants.StoreNameKey, storeName);
            Preferences.Set(Helpers.DatabaseConstants.StoreAddressKey, storeAddress);
        }
        //load settings
        public static (string storeName, string storeAddress) LoadStoreSettings()
        {
            string storeName = Preferences.Get(Helpers.DatabaseConstants.StoreNameKey, "Kusina POS");
            string storeAddress = Preferences.Get(Helpers.DatabaseConstants.StoreAddressKey, "123 Main St, City, Country");
            return (storeName, storeAddress);
        }
    }
}
