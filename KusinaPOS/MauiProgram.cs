using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using KusinaPOS.Views;
using Microsoft.Extensions.Logging;

namespace KusinaPOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            //services
            builder.Services.AddSingleton<IDatabaseService>(new DatabaseService(DatabaseConstants.DatabasePath));
            builder.Services.AddSingleton<UserService>();

            //viewmodels and pages
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<DashboardPage>();
            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<MenuItemPage>();
            builder.Services.AddSingleton<MenuItemViewModel>();
            return builder.Build();
        }
    }
}
