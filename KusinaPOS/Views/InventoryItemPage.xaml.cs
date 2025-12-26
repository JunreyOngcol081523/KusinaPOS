using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class InventoryItemPage : ContentPage
{
	public InventoryItemPage(InventoryItemViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}