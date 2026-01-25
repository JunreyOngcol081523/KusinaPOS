using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class ReportPage : ContentPage
{
	public ReportPage(ReportViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
        vm.ShowSaleItemsPopupRequested += () =>
        {
            SaleItemsPopup.Show();
        };
    }
}