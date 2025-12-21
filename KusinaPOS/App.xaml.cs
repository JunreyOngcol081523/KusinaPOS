using KusinaPOS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KusinaPOS
{
    public partial class App : Application
    {
        public App(IDatabaseService databaseService, UserService userService)
        {
            InitializeComponent();
            // Call async initialization WITHOUT blocking
            _ = InitializeAppAsync(databaseService, userService);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
        private async Task InitializeAppAsync(IDatabaseService databaseService, UserService userService)
        {
            await databaseService.InitializeAsync();
            await userService.InitializeAsync();
        }
    }
}