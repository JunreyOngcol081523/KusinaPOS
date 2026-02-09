using KusinaPOS.Helpers;
using KusinaPOS.ViewModel;
using Microsoft.Maui.Controls;
using Syncfusion.Maui.Popup;

namespace KusinaPOS.Views.Controls;

public partial class AppHeaderView : ContentView
{
    HeaderViewModel _vm =new HeaderViewModel();
    public AppHeaderView()
    {
        InitializeComponent();

        var vm = IPlatformApplication.Current?.Services.GetService<HeaderViewModel>();
        _vm = vm ?? new HeaderViewModel();
        if (vm == null)
        {
            System.Diagnostics.Debug.WriteLine("CRITICAL ERROR: HeaderViewModel is NULL. Check MauiProgram.cs!");
        }

        BindingContext = vm;
    }
    private void OnUserMenuTapped(object sender, EventArgs e)
    {
        if (sender is View anchorView)
        {
            // Shows the popup aligned to the Bottom-Right of the gear icon
            UserMenuPopup.ShowRelativeToView(anchorView, PopupRelativePosition.AlignBottomRight);
        }
    }
    private void ContentView_Loaded(object sender, EventArgs e)
    {
        // Fetch once when the control appears on screen
        var name = Preferences.Get(DatabaseConstants.LoggedInUserNameKey, "Administrator");

        if (BindingContext is HeaderViewModel vm)
        {
            vm.LoggedInUserName = name;
            _ = vm.InitializeAsync();
        }
    }
}