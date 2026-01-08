using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KusinaPOS.Helpers
{
    public static class DatabaseConstants
    {
        public const string DatabaseFileName = "kusinapos.db3";
        public const string LoggedInUserNameKey = "LoggedInUserName";
        public const string LoggedInUserIdKey = "LoggedInIdName";
        public const string StoreNameKey = "StoreNameKey";
        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

        
    }

}
