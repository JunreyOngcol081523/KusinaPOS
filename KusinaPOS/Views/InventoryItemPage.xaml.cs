using KusinaPOS.ViewModel;
using Syncfusion.Maui.DataGrid;

namespace KusinaPOS.Views;

public partial class InventoryItemPage : ContentPage
{
	public InventoryItemPage(InventoryItemViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
    private void DataGrid_QueryRowHeight(object sender, DataGridQueryRowHeightEventArgs e)
    {
        if (e.RowIndex != 0)
        {
            //Calculates and sets the height of the row based on its content.
            e.Height = e.GetIntrinsicRowHeight(e.RowIndex);
            e.Handled = true;
        }
    }
}