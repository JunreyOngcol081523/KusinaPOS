using CommunityToolkit.Mvvm.ComponentModel;

namespace KusinaPOS.Models
{
    public partial class OrderItem : ObservableObject
    {
        public int MenuItemId { get; set; }

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private decimal price;

        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private decimal subtotal;

        public string ImagePath { get; set; } = string.Empty;

        partial void OnQuantityChanged(int value)
        {
            Subtotal = Price * value;
        }

        partial void OnPriceChanged(decimal value)
        {
            Subtotal = value * Quantity;
        }
    }
}