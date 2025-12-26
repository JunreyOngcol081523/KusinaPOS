using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Helpers
{
    public static class DatabaseConstants
    {
        public const string DatabaseFileName = "kusinapos.db3";
        public const string LoggedInUserNameKey = "LoggedInUserName";
        public const string LoggedInUserIdKey = "LoggedInIdName";
        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
    }

}
