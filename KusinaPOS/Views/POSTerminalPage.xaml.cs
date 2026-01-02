using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class POSTerminalPage : ContentPage
{
	public POSTerminalPage(POSTerminalViewModel vm)
	{
        try
        {
            InitializeComponent();
            BindingContext = vm;

            // DEBUG: Verify BindingContext
            System.Diagnostics.Debug.WriteLine($"=== BindingContext set: {BindingContext != null} ===");
            System.Diagnostics.Debug.WriteLine($"=== ViewModel type: {BindingContext?.GetType().Name} ===");

            if (BindingContext is POSTerminalViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"=== MenuCategories count: {viewModel.MenuCategories?.Count ?? 0} ===");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"POSTerminalPage Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            throw; // Re-throw to see in debugger
        }
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is POSTerminalViewModel vm)
        {
            System.Diagnostics.Debug.WriteLine($"=== OnAppearing - Categories: {vm.MenuCategories?.Count ?? 0} ===");
        }
    }
}