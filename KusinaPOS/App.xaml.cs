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
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}