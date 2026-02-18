using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class InventoryItemBulkStocksEditPage : ContentView
{
	public InventoryItemBulkStocksEditPage(InventoryBulkStocksEditViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
    public InventoryItemBulkStocksEditPage()
    {
        InitializeComponent();

        // Resolve the VM from the Handler/Service provider
        if (IPlatformApplication.Current?.Services != null)
        {
            BindingContext = Handler?.MauiContext?.Services.GetService<InventoryBulkStocksEditViewModel>()
                             ?? App.Current.Handler.MauiContext.Services.GetService<InventoryBulkStocksEditViewModel>();
        }
    }
}