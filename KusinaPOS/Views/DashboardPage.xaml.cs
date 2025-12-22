using KusinaPOS.ViewModel;

namespace KusinaPOS.Views;

public partial class DashboardPage : ContentPage
{
	public DashboardPage(DashboardViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}