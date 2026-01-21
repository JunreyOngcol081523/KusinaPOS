using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.Models
{
    public class DBBackupInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string FormattedDate { get; set; }
        public string FormattedTime { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileSizeFormatted { get; set; }

        public DBBackupInfo(FileInfo fileInfo)
        {
            FileName = fileInfo.Name;
            FilePath = fileInfo.FullName;
            CreatedDateTime = fileInfo.CreationTime;
            FormattedDate = fileInfo.CreationTime.ToString("MMM dd, yyyy");
            FormattedTime = fileInfo.CreationTime.ToString("hh:mm:ss tt");
            FileSizeBytes = fileInfo.Length;
            FileSizeFormatted = FormatFileSize(fileInfo.Length);
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
