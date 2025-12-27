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
            Preferences.Set(DatabaseConstants.StoreNameKey, "The Myth | Food🥄and Drinks🥂");
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
           
            builder.Services.AddSingleton<InventoryItemService>();

            //viewmodels and pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<MenuItemPage>();
            builder.Services.AddTransient<MenuItemViewModel>();
            builder.Services.AddTransient<InventoryItemPage>();
            builder.Services.AddTransient<InventoryItemViewModel>();


            return builder.Build();
        }
    }
}
