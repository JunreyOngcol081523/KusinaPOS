using Foundation;
using UIKit;

namespace KusinaPOS.Services
{
    public partial class SaveService
    {
        public partial void SaveAndView(string filename, string contentType, MemoryStream stream)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

            // Save file
            File.WriteAllBytes(path, stream.ToArray());

            // Create UIDocumentInteractionController to preview/share the file
            var previewController = UIDocumentInteractionController.FromUrl(NSUrl.FromFilename(path));

            // Get the root view controller
            var window = UIApplication.SharedApplication.KeyWindow;
            var viewController = window?.RootViewController;

            if (viewController != null)
            {
                // Present options to open/share the file
                previewController.PresentOpenInMenu(
                    viewController.View.Frame,
                    viewController.View,
                    true
                );
            }
        }
    }
}