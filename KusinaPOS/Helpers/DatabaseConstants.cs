using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace KusinaPOS.Helpers
{
    public static class DatabaseConstants
    {
        public const string DatabaseFileName = "kusinapos.db3";
        public const string LoggedInUserNameKey = "LoggedInUserName";
        public const string LoggedInUserIdKey = "LoggedInIdName";
        public const string StoreNameKey = "StoreNameKey";
        public const string StoreAddressKey = "StoreAddressKey";
        public const string BackupLocationKey = "BackupLocationKey";
        public const string LastBackupDateKey = "LastBackupDateKey";
        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        public static string SettingsFolder =>
            Path.Combine(FileSystem.AppDataDirectory, "Settings");
        public static string BackupFolder =>
            Path.Combine(SettingsFolder, "Backups");
        public static string StoreLogoFolder =>
            Path.Combine(SettingsFolder, "StoreLogo");
    }

}
