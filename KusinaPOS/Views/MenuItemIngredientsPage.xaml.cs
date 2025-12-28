using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class MenuItemIngredientsPage : ContentPage
{
	public MenuItemIngredientsPage(MenuItemIngredientsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext=viewModel;
	}
}