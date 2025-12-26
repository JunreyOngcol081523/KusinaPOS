using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using KusinaPOS.Views;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

namespace KusinaPOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1ccnVdRGZdUUB/XkdWYEs=");
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
            builder.ConfigureSyncfusionCore();
            builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
            builder.Services.AddSingleton<CategoryService>();
            builder.Services.AddSingleton<MenuItemService>();
            builder.Services.AddSingleton<DateTimeService>(); // now DI knows both DateTimeService and IDateTimeService

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
