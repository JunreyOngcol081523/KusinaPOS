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
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load the chart immediately (Fixed Weekly View)
        if (BindingContext is ReportViewModel vm)
        {
            await vm.LoadWeeklyChartDataAsync();
            await vm.LoadExpensesReportAsync();
            await vm.LoadLowStockItemsAsync();
            await vm.LoadStockMovementPieChartAsync();
        }
    }
}