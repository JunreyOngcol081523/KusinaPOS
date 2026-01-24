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
        if (sender is SfSwitch sw && sw.BindingContext is Models.MenuItem menuItem)
        {
            try
            {
                await _menuItemService.UpdateMenuItemAsync(menuItem);
                // No need to reload all categories
            }
            catch (Exception ex)
            {
                menuItem.IsActive = !menuItem.IsActive; // rollback
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to update status: {ex.Message}", "OK");
            }
        }
    }
    private void CategorySegment_SelectionChanged(object sender, Syncfusion.Maui.Buttons.SelectionChangedEventArgs e)
    {
        if (_menuItemViewModel.Categories.Count == 0) return;

        var selectedIndex = (sender as Syncfusion.Maui.Buttons.SfSegmentedControl)?.SelectedIndex ?? 0;
        _menuItemViewModel.SelectedSegmentCategory = _menuItemViewModel.Categories[selectedIndex];

        // Reset paging
        _menuItemViewModel.ResetPaging();
        _menuItemViewModel.HideBorder();
        // Load first page
        _ = _menuItemViewModel.LoadMoreMenuItemsAsync();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_menuItemViewModel.Categories.Any())
        {
            _menuItemViewModel.SelectedSegmentCategory = _menuItemViewModel.Categories[0];
            _menuItemViewModel.ResetPaging();
            _ = _menuItemViewModel.LoadMoreMenuItemsAsync();
        }
    }

}