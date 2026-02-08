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
        readonly UserService _userService;
        // 3. Authorization Fields
        [ObservableProperty]
        private string _adminPassword = string.Empty;

        [ObservableProperty]
        private string _authorizedBy = string.Empty;

        // 4. Refund Details
        [ObservableProperty]
        private string _voidReason = string.Empty;

        [ObservableProperty]
        private decimal _refundedAmount;

        // 5. Customer Details
        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerContact = string.Empty;
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
        public RefundSaleViewModel(SalesService salesService, UserService userService)
        {
            _salesService = salesService;
            _userService = userService;
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
                RefundedAmount = 0;
            }
            else
            {
                IsSaleFound = true;
                SelectedSaleTransaction = sale;
                RefundedAmount = sale.TotalAmount;
                SaleItems = await _salesService.GetSaleItemsWithMenuNameAsync(sale.Id);
            }
        }

        [RelayCommand]
        private async Task ProcessVoidSaleAsync()
        {
            // 1. Validate Selection exists
            if (SelectedSaleTransaction is null)
            {
                await PageHelper.DisplayAlertAsync("Error", "No sale selected to process.", "OK");
                return;
            }

            // --- NEW VALIDATION: Check Status ---
            // Ensure we only process sales that are currently valid/completed.
            if (SelectedSaleTransaction.Status != "Completed")
            {
                await PageHelper.DisplayAlertAsync("Action Denied", $"This transaction is already marked as '{SelectedSaleTransaction.Status}' and cannot be modified further.", "OK");
                return;
            }
            // ------------------------------------

            // 2. Validate Common Required Fields (Admin Password, Reason, Authorizer)
            var user = await _userService.LoginWithPinAsync(AdminPassword,"Administrator");

            if (string.IsNullOrWhiteSpace(AdminPassword))
            {
                await PageHelper.DisplayAlertAsync("Validation Error", "Please enter the Admin Password.", "OK");
                return;
            }
            if (user is null)
            {
                await PageHelper.DisplayAlertAsync("Authentication Failed", "Invalid Admin Password. Please try again.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(VoidReason))
            {
                await PageHelper.DisplayAlertAsync("Validation Error", "Please provide a reason for this action.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(AuthorizedBy))
            {
                await PageHelper.DisplayAlertAsync("Validation Error", "Please enter the name of the person authorizing this.", "OK");
                return;
            }

            // 3. Handle Mode-Specific Logic
            if (IsRefundMode)
            {
                // ... (Refund Validation Logic remains the same) ...
                if (string.IsNullOrWhiteSpace(CustomerName))
                {
                    await PageHelper.DisplayAlertAsync("Validation Error", "Customer Name is required for refunds.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(CustomerContact))
                {
                    await PageHelper.DisplayAlertAsync("Validation Error", "Customer Contact number is required for refunds.", "OK");
                    return;
                }

                if (RefundedAmount <= 0)
                {
                    await PageHelper.DisplayAlertAsync("Validation Error", "Refund amount must be greater than zero.", "OK");
                    return;
                }

                if (RefundedAmount > SelectedSaleTransaction.TotalAmount)
                {
                    await PageHelper.DisplayAlertAsync("Validation Error", $"Refund amount cannot exceed the original sale amount (₱{SelectedSaleTransaction.TotalAmount:N2}).", "OK");
                    return;
                }

                // --- Execute Refund Logic ---
                bool confirm = await PageHelper.DisplayConfirmAsync("Confirm Refund",
                    $"Refund ₱{RefundedAmount:N2} to {CustomerName}?\n(Receipt: {SelectedSaleTransaction.ReceiptNo})",
                    "Yes", "No");

                if (!confirm) return;

                bool result = await _salesService.RefundSaleAsync(SelectedSaleTransaction, RefundedAmount, CustomerName, CustomerContact, VoidReason, AuthorizedBy);

                if (result)
                {
                    await PageHelper.DisplayAlertAsync("Success", "Refund processed successfully.", "OK");
                    ResetForm();
                }
                else
                {
                    await PageHelper.DisplayAlertAsync("Error", "Refund failed. Please check the logs or try again.", "OK");
                }
            }
            else
            {
                // ... (Void Logic remains the same) ...
                bool confirm = await PageHelper.DisplayConfirmAsync("Confirm Void",
                    $"Are you sure you want to completely VOID sale with Receipt No: {SelectedSaleTransaction.ReceiptNo}?",
                    "Yes", "No");

                if (!confirm) return;

                bool result = await _salesService.VoidSaleAsync(SelectedSaleTransaction, VoidReason, AuthorizedBy);

                if (result)
                {
                    await PageHelper.DisplayAlertAsync("Success", "Sale has been successfully voided.", "OK");
                    ResetForm();
                }
                else
                {
                    await PageHelper.DisplayAlertAsync("Error", "Failed to void the sale. Please check Admin Password and try again.", "OK");
                }
            }
        }

        // Helper to clear the inputs after success
        private void ResetForm()
        {
            SelectedSaleTransaction = null;
            SaleItems = null;
            SearchReceiptNo = string.Empty;
            IsSaleFound = false;

            // Clear inputs
            AdminPassword = string.Empty;
            VoidReason = string.Empty;
            AuthorizedBy = string.Empty;
            CustomerName = string.Empty;
            CustomerContact = string.Empty;
            RefundedAmount = 0;
        }
    }
}