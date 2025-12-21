namespace KusinaPOS
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Register routes
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        }
    }
}
