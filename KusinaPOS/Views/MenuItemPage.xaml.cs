using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using Syncfusion.Maui.Buttons;

namespace KusinaPOS.Views;

public partial class MenuItemPage : ContentPage
{
    private MenuItemService _menuItemService;
    private MenuItemViewModel _menuItemViewModel;
    public MenuItemPage(MenuItemViewModel vm, MenuItemService menuItemService)
	{
		InitializeComponent();
		BindingContext = vm;
        _menuItemService = menuItemService;
        _menuItemViewModel = vm;
    }
    private async void SfSwitch_StateChanged(object sender, SwitchStateChangedEventArgs e)
    {
        if (sender is SfSwitch sw &&
            sw.BindingContext is Models.MenuItem menuItem)
        {
            try
            {
                // IsActive is ALREADY updated because of TwoWay binding
                await _menuItemService.UpdateMenuItemAsync(menuItem);
               await _menuItemViewModel.LoadCategoriesWithMenuItems();
            }
            catch (Exception ex)
            {
                // rollback if DB update fails
                menuItem.IsActive = !menuItem.IsActive;
                
                await PageHelper.DisplayAlertAsync(
                    "Error",
                    $"Failed to update status: {ex.Message}",
                    "OK"
                );
            }
        }
    }

}