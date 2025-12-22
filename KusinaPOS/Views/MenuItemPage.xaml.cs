using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class MenuItemPage : ContentPage
{
	public MenuItemPage(MenuItemViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}