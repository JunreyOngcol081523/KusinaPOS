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

}