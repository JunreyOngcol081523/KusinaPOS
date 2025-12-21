namespace KusinaPOS
{
    public partial class MainPage : ContentPage
    {
        

        public MainPage()
        {
            InitializeComponent();
            PinEntry.TextChanged += OnPinEntryTextChanged;
        }
        private void OnAdministratorTapped(object sender, EventArgs e)
        {
            // Highlight Administrator
            AdminBorder.Stroke = (Color)Application.Current.Resources["Primary"];

            // Unhighlight Cashier
            CashierBorder.Stroke = (Color)Application.Current.Resources["Gray300"];

            // Update label
            SelectedUserTypeLabel.Text = "Administrator Selected";
            SelectedUserTypeLabel.TextColor = (Color)Application.Current.Resources["Primary"];
        }

        private void OnCashierTapped(object sender, EventArgs e)
        {
            // Highlight Cashier
            CashierBorder.Stroke = (Color)Application.Current.Resources["Primary"];

            // Unhighlight Administrator
            AdminBorder.Stroke = (Color)Application.Current.Resources["Gray300"];

            // Update label
            SelectedUserTypeLabel.Text = "Cashier Selected";
            SelectedUserTypeLabel.TextColor = (Color)Application.Current.Resources["Primary"];
        }
        private void OnPinEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NewTextValue))
                return;

            // Remove any non-numeric characters
            if (!int.TryParse(e.NewTextValue, out _))
            {
                ((Entry)sender).Text = e.OldTextValue;
            }
        }
    }
}
