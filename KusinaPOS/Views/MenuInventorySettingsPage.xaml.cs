using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class MenuInventorySettingsPage : ContentView
{
	public MenuInventorySettingsPage(MenuInventorySettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
    // The parameterless constructor allows XAML to "see" it
    public MenuInventorySettingsPage()
    {
        InitializeComponent();

        // Resolve the VM from the Handler/Service provider
        if (IPlatformApplication.Current?.Services != null)
        {
            BindingContext = Handler?.MauiContext?.Services.GetService<MenuInventorySettingsViewModel>()
                             ?? App.Current.Handler.MauiContext.Services.GetService<MenuInventorySettingsViewModel>();
        }
    }
}