using KusinaPOS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KusinaPOS
{
    public partial class App : Application
    {
        public App(IDatabaseService databaseService)
        {
            InitializeComponent();
            Task.Run(async () => await databaseService.InitializeAsync())
            .Wait();

            // Add exception handler to see the actual error
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"Unhandled Exception: {exception?.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {exception?.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {exception?.InnerException?.Message}");
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}