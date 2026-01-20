using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}