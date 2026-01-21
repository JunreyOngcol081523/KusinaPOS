using CommunityToolkit.Maui;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using KusinaPOS.Views;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using System.Diagnostics;

namespace KusinaPOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Debug.WriteLine($"Database Path: {DatabaseConstants.DatabasePath}");
            
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1ccnVdRGZdUUB/XkdWYEs=");
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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
            builder.Services.AddSingleton<InventoryTransactionService>();
            builder.Services.AddSingleton<InventoryItemService>();
            builder.Services.AddSingleton<MenuItemIngredientService>();
            builder.Services.AddSingleton<SalesService>();
            builder.Services.AddSingleton<SettingsService>();

            //viewmodels and pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<MenuItemPage>();
            builder.Services.AddTransient<MenuItemViewModel>();
            builder.Services.AddTransient<InventoryItemPage>();
            builder.Services.AddTransient<InventoryItemViewModel>();
            builder.Services.AddTransient<MenuItemIngredientsPage>();
            builder.Services.AddTransient<MenuItemIngredientsViewModel>();
            builder.Services.AddTransient<UserViewModel>();
            builder.Services.AddTransient<UserPage>();
            builder.Services.AddTransient<POSTerminalViewModel>();
            builder.Services.AddTransient<POSTerminalPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsViewModel>();


            return builder.Build();
        }
    }
}
