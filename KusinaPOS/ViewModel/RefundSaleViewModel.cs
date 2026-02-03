using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Enums;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Models.SQLViews;
using KusinaPOS.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace KusinaPOS.ViewModel
{
    public partial class RefundSaleViewModel:ObservableObject
    {
        readonly SalesService _salesService;
        
        [ObservableProperty]
        private Sale selectedSaleTransaction;
        [ObservableProperty]
        private List<SaleItemWithMenuName> saleItems;
        [ObservableProperty]
        private string searchReceiptNo;
        private SaleActionMode _selectedMode = SaleActionMode.Voided;
        private string _buttonText = "Process Void";
        private Color _buttonColor = Colors.RoyalBlue; // Default "Primary" color
        [ObservableProperty]
        private bool isSaleFound = false;
        //constructor
        public RefundSaleViewModel(SalesService salesService)
        {
            _salesService = salesService;
        }
        public SaleActionMode SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode != value)
                {
                    _selectedMode = value;

                    OnPropertyChanged(nameof(SelectedMode));
                    OnPropertyChanged(nameof(IsRefundMode));

                    // Update UI properties based on selection
                    if (_selectedMode == SaleActionMode.Refunded)
                    {
                        ButtonText = "Process Refund";
                        ButtonColor = Colors.Red;
                    }
                    else
                    {
                        ButtonText = "Process Void";
                        ButtonColor = Colors.RoyalBlue; // Or your specific Primary theme color
                    }
                }
            }
        }

        public string ButtonText
        {
            get => _buttonText;
            set => SetProperty(ref _buttonText, value);
        }

        public Color ButtonColor
        {
            get => _buttonColor;
            set => SetProperty(ref _buttonColor, value);
        }

        public bool IsRefundMode => _selectedMode == SaleActionMode.Refunded;

        [RelayCommand]
        private async Task FindSaleAsync() { 
           if(SearchReceiptNo is null || SearchReceiptNo.Trim() == string.Empty)
            {
                await PageHelper.DisplayAlertAsync("Error", "Please enter a valid Sale ID.", "OK");
                return;
            }
            var sale = await _salesService.GetSaleByReceiptNoAsync(SearchReceiptNo.Trim());
            if(sale is null)
            {
                await PageHelper.DisplayAlertAsync("Not Found", $"No sale found with ID: {SearchReceiptNo}", "OK");
                SelectedSaleTransaction = null;
                SaleItems = null;
            }
            else
            {
                IsSaleFound = true;
                SelectedSaleTransaction = sale;
                SaleItems = await _salesService.GetSaleItemsWithMenuNameAsync(sale.Id);
            }
        }
    }
}
//SALESID-20260125092940122