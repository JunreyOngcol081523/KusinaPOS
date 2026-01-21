using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using Syncfusion.Maui.Buttons;
using System.Diagnostics;

namespace KusinaPOS.Views;

public partial class MenuItemPage : ContentPage
{
    private MenuItemService _menuItemService;
    private MenuItemViewModel _menuItemViewModel;
    private IDateTimeService? _dateTimeService = null;
    public MenuItemPage(MenuItemViewModel vm, MenuItemService menuItemService, IDateTimeService dts)
	{
		InitializeComponent();
		BindingContext = vm;
        _menuItemService = menuItemService;
        _menuItemViewModel = vm;
        _dateTimeService = dts;
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
               //await _menuItemViewModel.LoadCategoriesWithMenuItems();
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
    //onapppearing
    //protected override void OnAppearing()
    //{
    //    base.OnAppearing();
    //    _ = SeedOnceAsync();
    //}

    //private async Task SeedOnceAsync()
    //{
    //    try
    //    {
    //        await _menuItemViewModel.SeedMenuItemsAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Seed failed: {ex}");
    //    }
    //}
}