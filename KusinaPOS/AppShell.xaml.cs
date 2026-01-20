using KusinaPOS.Views;

namespace KusinaPOS
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Register routes
            //Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
            Routing.RegisterRoute(nameof(MenuItemPage), typeof(MenuItemPage));
            Routing.RegisterRoute(nameof(InventoryItemPage), typeof(InventoryItemPage));
            Routing.RegisterRoute(nameof(MenuItemIngredientsPage), typeof(MenuItemIngredientsPage));
            Routing.RegisterRoute(nameof(UserPage), typeof(UserPage));
            Routing.RegisterRoute(nameof(POSTerminalPage), typeof(POSTerminalPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

            
        }
    }
}
