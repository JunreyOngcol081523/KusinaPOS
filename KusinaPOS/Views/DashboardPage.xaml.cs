using KusinaPOS.Services;
using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class DashboardPage : ContentPage
{
    private DateTimeService? _dateTimeService = null;
    public DashboardPage(DashboardViewModel vm,DateTimeService dts )
	{
		InitializeComponent();
		BindingContext = vm;
        _dateTimeService = dts;
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_dateTimeService != null)
        {
            _dateTimeService.Dispose();
            _dateTimeService = null; // prevent double-dispose
        }
    }


}