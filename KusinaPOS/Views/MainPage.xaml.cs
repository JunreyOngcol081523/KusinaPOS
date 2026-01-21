using KusinaPOS.Helpers;
using KusinaPOS.Services;
using KusinaPOS.ViewModel;
using System.Diagnostics;

namespace KusinaPOS
{
    public partial class MainPage : ContentPage
    {
 

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();      
            BindingContext = vm;
        }
    }
}
