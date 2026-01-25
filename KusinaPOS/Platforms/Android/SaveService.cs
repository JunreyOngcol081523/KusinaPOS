using Android.Content;
using Android.OS;
using Java.IO;
using AndroidX.Core.Content;
using System;
using System.IO;
using FileProvider = AndroidX.Core.Content.FileProvider;
using Console = System.Console;

namespace KusinaPOS.Services
{
    public partial class SaveService
    {
        public partial void SaveAndView(string filename, string contentType, MemoryStream stream)
        {
            try
            {
                if (stream == null || stream.Length == 0)
                    return;

                // Determine root folder
                string root = Android.OS.Environment.IsExternalStorageEmulated
                    ? Android.App.Application.Context!.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads)!.AbsolutePath
                    : System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

                // Create folder for reports
                var myDir = new Java.IO.File(System.IO.Path.Combine(root, "KusinaPOS"));
                if (!myDir.Exists())
                    myDir.Mkdirs();

                var file = new Java.IO.File(myDir, filename);
                if (file.Exists())
                    file.Delete();

                // Write the Excel file
                using (var outs = new FileOutputStream(file))
                {
                    outs.Write(stream.ToArray());
                    outs.Flush();
                }

                if (!file.Exists())
                    return;

                // Prepare intent
                Intent intent;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    var fileUri = FileProvider.GetUriForFile(
                        Android.App.Application.Context,
                        Android.App.Application.Context.PackageName + ".provider",
                        file);

                    intent = new Intent(Intent.ActionView);
                    intent.SetDataAndType(fileUri, contentType);
                    intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
                }
                else
                {
                    var fileUri = Android.Net.Uri.Parse(file.AbsolutePath);
                    intent = new Intent(Intent.ActionView);
                    intent.SetDataAndType(fileUri, contentType);
                    intent.AddFlags(ActivityFlags.NewTask);
                }

                // Launch chooser
                intent = Intent.CreateChooser(intent, "Open with");
                intent.AddFlags(ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("SaveService", $"Error saving or opening file: {ex}");
                Console.WriteLine($"Error saving or opening file: {ex}");
            }
        }
    }
}
