using KusinaPOS.Models;
using KusinaPOS.ViewModel;

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
}