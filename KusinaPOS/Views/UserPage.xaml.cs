using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class UserPage : ContentPage
{
	public UserPage(UserViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}