using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class DashboardPage : ContentPage
{
	public DashboardPage(DashboardViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}