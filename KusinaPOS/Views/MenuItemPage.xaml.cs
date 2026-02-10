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

    public MenuItemPage(MenuItemViewModel vm, MenuItemService menuItemService)
    {
        InitializeComponent();
        BindingContext = vm;
        _menuItemService = menuItemService;
        _menuItemViewModel = vm;
    }

    private async void SfSwitch_StateChanged(object sender, SwitchStateChangedEventArgs e)
    {
        if (sender is SfSwitch sw && sw.BindingContext is Models.MenuItem menuItem)
        {
            try
            {
                await _menuItemService.UpdateMenuItemAsync(menuItem);
            }
            catch (Exception ex)
            {
                menuItem.IsActive = !menuItem.IsActive; // rollback
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to update status: {ex.Message}", "OK");
            }
        }
    }

    // REMOVED: CategorySegment_SelectionChanged 
    // Logic is now handled by OnSelectedSegmentCategoryChanged in the ViewModel

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // We trigger the initial load via the ViewModel command
        // This ensures the database seeding and category loading happen correctly
        _ = _menuItemViewModel.LoadCategoriesAsync();
    }
}