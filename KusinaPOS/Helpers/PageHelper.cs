namespace KusinaPOS.Helpers
{
    public static class PageHelper
    {
        private static Page? GetCurrentPage()
        {
            var app = Application.Current;
            if (app?.Windows?.Count > 0)
            {
                return app.Windows[0].Page;
            }
            return null;
        }

        // ✅ UI-SAFE ALERT
        public static Task DisplayAlertAsync(
            string title,
            string message,
            string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    await page.DisplayAlertAsync(title, message, cancel);
                }
            });
        }

        // ✅ UI-SAFE CONFIRM
        public static Task<bool> DisplayConfirmAsync(
            string title,
            string message,
            string accept,
            string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    return await page.DisplayAlertAsync(
                        title,
                        message,
                        accept,
                        cancel);
                }
                return false;
            });
        }

        // ✅ UI-SAFE NAVIGATION
        public static Task NavigateToAsync(Page page)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var currentPage = GetCurrentPage();
                if (currentPage?.Navigation != null)
                {
                    await currentPage.Navigation.PushAsync(page);
                }
            });
        }

        // ✅ UI-SAFE BACK NAVIGATION
        public static Task NavigateBackAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var currentPage = GetCurrentPage();
                if (currentPage?.Navigation != null)
                {
                    await currentPage.Navigation.PopAsync();
                }
            });
        }
    }
}
