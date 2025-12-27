using KusinaPOS.Services;
using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class DashboardPage : ContentPage
{
    private IDateTimeService _dateTimeService;

    public DashboardPage(
        DashboardViewModel vm,
        IDateTimeService dts)
    {
        InitializeComponent();
        BindingContext = vm;
        _dateTimeService = dts;
    }
}
