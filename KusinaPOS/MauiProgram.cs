using KusinaPOS.Helpers;
using KusinaPOS.Services;
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
            builder.Services.AddSingleton<IDatabaseService>(
                new DatabaseService(DatabaseConstants.DatabasePath)
            );

            return builder.Build();
        }
    }
}
