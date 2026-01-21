using KusinaPOS.ViewModel;
using System.Net.Http;

namespace KusinaPOS.Views;

public partial class SettingsPage : ContentPage
{
	private readonly SettingsViewModel _viewModel;
    public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		_viewModel = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadBackupsAsync();

    }
}