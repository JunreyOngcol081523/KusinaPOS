using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.ViewModel;
using System.Threading.Tasks;

namespace KusinaPOS.Views;

public partial class MenuItemIngredientsPage : ContentPage
{
	public MenuItemIngredientsPage(MenuItemIngredientsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext=viewModel;

	}
    private void OnQuantityEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is MenuItemIngredient ingredient)
        {
            ((MenuItemIngredientsViewModel)BindingContext).DebouncedQuantityChangedCommand.Execute(ingredient);
        }
    }

    private async Task ImageButton_Clicked(object sender, EventArgs e)
    {

        await PageHelper.DisplayAlertAsync("Info", "Quantity per serve updated", "OK");
    }
}