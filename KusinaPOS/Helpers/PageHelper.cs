namespace KusinaPOS.Helpers
{
    public static class PageHelper
    {
        public static Page? GetCurrentPage()
        {
            var app = Application.Current;
            if (app != null && app.Windows.Count > 0)
            {
                var window = app.Windows[0];
                return window?.Page;
            }
            return null;
        }

        public static async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(title, message, cancel);
            }
        }

        public static async Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
        {
            var page = GetCurrentPage();
            if (page != null)
            {
                return await page.DisplayAlertAsync(title, message, accept, cancel);
            }
            return false;
        }

        public static async Task NavigateToAsync(Page page)
        {
            var currentPage = GetCurrentPage();
            if (currentPage?.Navigation != null)
            {
                await currentPage.Navigation.PushAsync(page);
            }
        }

        public static async Task NavigateBackAsync()
        {
            var currentPage = GetCurrentPage();
            if (currentPage?.Navigation != null)
            {
                await currentPage.Navigation.PopAsync();
            }
        }
    }
}