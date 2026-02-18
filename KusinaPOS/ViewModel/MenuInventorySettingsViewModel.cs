using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MenuItem = KusinaPOS.Models.MenuItem;
using KusinaPOS.Helpers;
using KusinaPOS.Services;
using SQLite;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Text;
using IApplication = Syncfusion.XlsIO.IApplication;
using KusinaPOS.Models;

namespace KusinaPOS.ViewModel
{
    public partial class MenuInventorySettingsViewModel : ObservableObject
    {
        private readonly MenuItemService menuItemService;
        private readonly InventoryItemService inventoryItemService;
        private readonly SQLiteAsyncConnection _db;
        
        [ObservableProperty]
        private bool isBusy = false;
        [ObservableProperty]
        private bool hasUnitBasedSelected = false;

        [ObservableProperty]
        private string selectedMenuFilePath = string.Empty;
        [ObservableProperty]
        private string selectedMenuFileName = string.Empty;
        [ObservableProperty]
        private bool hasRecipeBasedSelected = false;

        [ObservableProperty]
        private string selectedInventoryFilePath = string.Empty;
        [ObservableProperty]
        private string selectedInventoryFileName = string.Empty;
        public MenuInventorySettingsViewModel(MenuItemService menuItemService, InventoryItemService inventoryItemService, IDatabaseService databaseService)
        {
            this.menuItemService = menuItemService;
            this.inventoryItemService = inventoryItemService;
            SelectedMenuFilePath = SelectedInventoryFilePath = "No file selected...";
            _db = databaseService.GetConnection();
        }
        #region UnitBasedMenuImport
        [RelayCommand]
        private async Task DownloadUnitBasedMenuTemplateAsync()
        {
            try
            {
                var fileName = "UnitBasedMenuItemsTemplate.xlsx";
                // 1. Open the file from MauiAssets (Resources/Raw)
                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);

                // 2. Use FileSaver to let the user save it to their tablet
                var fileSaveResult = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);

                if (fileSaveResult.IsSuccessful)
                {
                    await PageHelper.DisplayAlertAsync("Success", $"Template saved to: {fileSaveResult.FilePath}", "OK");
                }
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", "Could not export template.", "OK");
            }
        }
        //select xlsx file from filechooser
        [RelayCommand]
        private async Task SelectUnitBasedMenuTemplateAsync()
        {
            try
            {
                // 1. Define the Excel file types for each platform
                var excelFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "org.openxmlformats.spreadsheetml.sheet", "com.microsoft.excel.xls" } }, // UTType
                        { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" } }, // MIME type
                        { DevicePlatform.WinUI, new[] { ".xlsx", ".xls" } }, // Extension
                        { DevicePlatform.MacCatalyst, new[] { "xlsx", "xls" } }
                    });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Menu Excel File",
                    FileTypes = excelFileType
                });

                // FIX: Check if the user canceled the picker first!
                if (result == null)
                {
                    return; // User canceled, exit quietly
                }

                // 3. Now validate the file they actually picked
                var expectedHeaders = new List<string> { "Name", "Description", "Category", "Price", "Unit", "Quantity on Hand", "Cost Per Unit", "ReOrder Level" };
                var isValid = await IsSyncfusionTemplateValidAsync(result, expectedHeaders,2);

                if (!isValid)
                {
                    // Note: Your IsSyncfusionTemplateValidAsync already shows a DisplayAlert
                    // so you don't necessarily need a second one here unless you want to be generic.
                    return;
                }

                // 4. Update UI/State
                SelectedMenuFilePath = result.FullPath;
                SelectedMenuFileName = result.FileName;
                HasUnitBasedSelected = true;
            }
            catch (Exception ex)
            {
                // Log error (e.g., user denied permissions or system error)
                await PageHelper.DisplayAlertAsync("Error", $"Unable to pick file: {ex.Message}", "OK");
                HasUnitBasedSelected = false;
            }
        }
        [RelayCommand]
        private async Task StartImportUnitBasedMenuTemplateAsync()
        {
            int importedCount = 0;
            int skippedCount = 0; // 🟢 Track skipped duplicates
            if (IsBusy) return;

            // Add a safety check just in case
            if (string.IsNullOrEmpty(SelectedMenuFilePath))
            {
                await PageHelper.DisplayAlertAsync("Error", "Please select a file first.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                using var stream = System.IO.File.OpenRead(SelectedMenuFilePath);

                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    IWorkbook workbook = application.Workbooks.Open(stream);
                    IWorksheet worksheet = workbook.Worksheets[0];

                    int lastRow = worksheet.UsedRange.LastRow;

                    // Wrapping everything in a transaction for speed and safety
                    await _db.RunInTransactionAsync(tran =>
                    {
                        // 🟢 Get all existing menu names from the database BEFORE the loop
                        // StringComparer.OrdinalIgnoreCase makes "Coke" and "coke" count as the same thing
                        var existingItems = tran.Table<MenuItem>().ToList();
                        var existingNames = new HashSet<string>(existingItems.Select(m => m.Name), StringComparer.OrdinalIgnoreCase);

                        // Starting at Row 3 based on your template screenshot
                        for (int row = 3; row <= lastRow; row++)
                        {
                            string name = worksheet.Range[row, 1].Text?.Trim();

                            if (string.IsNullOrWhiteSpace(name)) continue;

                            // 🟢 DUPLICATE CHECKER: Skip if the name is already in our HashSet
                            if (existingNames.Contains(name))
                            {
                                skippedCount++;
                                continue;
                            }

                            // 🟢 Add to HashSet so we also catch duplicates within the Excel file itself
                            existingNames.Add(name);

                            // 1. Create MenuItem (Columns A-D)
                            var item = new MenuItem
                            {
                                Name = name,
                                Description = worksheet.Range[row, 2].Text?.Trim() ?? "",
                                Category = worksheet.Range[row, 3].Text?.Trim() ?? "General",
                                Price = (decimal)worksheet.Range[row, 4].Number,
                                Type = "Unit-Based",
                                IsActive = true
                            };
                            tran.Insert(item); // Insert to generate item.Id

                            // 2. Create InventoryItem (Columns E-H)
                            var inv = new InventoryItem
                            {
                                Name = item.Name,
                                Unit = worksheet.Range[row, 5].Text?.Trim() ?? "pc",
                                QuantityOnHand = (decimal)worksheet.Range[row, 6].Number,
                                CostPerUnit = (decimal)worksheet.Range[row, 7].Number,
                                ReOrderLevel = (decimal)worksheet.Range[row, 8].Number,
                                IsActive = true
                            };
                            tran.Insert(inv); // Insert to generate inv.Id

                            // 3. Create the Link (MenuItemIngredient)
                            tran.Insert(new MenuItemIngredient
                            {
                                MenuItemId = item.Id,
                                InventoryItemId = inv.Id,
                                InventoryItemName = inv.Name,
                                UnitOfMeasurement = inv.Unit,
                                QuantityPerMenu = 1 // Unit-based logic
                            });

                            // 4. Create Initial Stock Transaction
                            if (inv.QuantityOnHand > 0)
                            {
                                tran.Insert(new InventoryTransaction
                                {
                                    InventoryItemId = inv.Id,
                                    QuantityChange = inv.QuantityOnHand,
                                    CostAtTransaction = inv.CostPerUnit,
                                    Reason = "Stock In",
                                    Remarks = $"Initial bulk import for {item.Name}",
                                    TransactionDate = DateTime.Now
                                });
                            }

                            importedCount++;
                        }
                    });
                    IsBusy = false;

                    // 🟢 Update the success message to report skipped items
                    string successMessage = $"{importedCount} items and their inventory links have been created.";
                    if (skippedCount > 0)
                    {
                        successMessage += $"\n\n{skippedCount} items were skipped because they already exist in the database.";
                    }

                    await PageHelper.DisplayAlertAsync("Import Finished", successMessage, "OK");

                    // Clean up state
                    SelectedMenuFileName = string.Empty;
                    SelectedMenuFilePath = "No file selected...";
                    HasUnitBasedSelected = false;
                }
            }
            catch (Exception ex)
            {
                // Add skipped count to the offset so the error reports the correct Excel row
                await PageHelper.DisplayAlertAsync("Import Error", $"Row {importedCount + skippedCount + 3}: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion



        #region RecipeBasedInventoryItems
        [RelayCommand]
        private async Task DownloadRecipeBasedTemplateAsync()
        {
            try
            {
                var fileName = "RecipeBasedInventoryTemplate.xlsx";
                // 1. Open the file from MauiAssets (Resources/Raw)
                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);

                // 2. Use FileSaver to let the user save it to their tablet
                var fileSaveResult = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);

                if (fileSaveResult.IsSuccessful)
                {
                    await PageHelper.DisplayAlertAsync("Success", $"Template saved to: {fileSaveResult.FilePath}", "OK");
                }
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", "Could not export template.", "OK");
            }
        }
        [RelayCommand]
        private async Task StartImportInventoryTemplateAsync()
        {
            int importedCount = 0;
            int skippedCount = 0; // 🟢 Track skipped duplicates
            if (IsBusy) return;

            // Safety check - assuming you have a property for the selected file path
            if (string.IsNullOrEmpty(SelectedInventoryFilePath))
            {
                await PageHelper.DisplayAlertAsync("Error", "Please select a file first.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // Read from the actual file path the user selected
                using var stream = System.IO.File.OpenRead(SelectedInventoryFilePath);

                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    IWorkbook workbook = application.Workbooks.Open(stream);
                    IWorksheet worksheet = workbook.Worksheets[0];

                    int lastRow = worksheet.UsedRange.LastRow;

                    // Wrapping everything in a transaction for speed and safety
                    await _db.RunInTransactionAsync(tran =>
                    {
                        // 🟢 Get all existing INVENTORY names BEFORE the loop
                        var existingItems = tran.Table<InventoryItem>().ToList();
                        var existingNames = new HashSet<string>(existingItems.Select(i => i.Name), StringComparer.OrdinalIgnoreCase);

                        // Starting at Row 2 based on your template screenshot
                        for (int row = 2; row <= lastRow; row++)
                        {
                            string name = worksheet.Range[row, 1].Text?.Trim();

                            // Skip empty rows
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            // 🟢 DUPLICATE CHECKER: Skip if the inventory item already exists
                            if (existingNames.Contains(name))
                            {
                                skippedCount++;
                                continue;
                            }

                            // 🟢 Add to HashSet to prevent duplicates within the Excel file itself
                            existingNames.Add(name);

                            // 🟢 SAFETY FIX: Use double.IsNaN to prevent System.OverflowException on empty cells
                            double rawQty = worksheet.Range[row, 3].Number;
                            decimal quantityOnHand = double.IsNaN(rawQty) ? 0m : (decimal)rawQty;

                            double rawCost = worksheet.Range[row, 4].Number;
                            decimal costPerUnit = double.IsNaN(rawCost) ? 0m : (decimal)rawCost;

                            double rawReorder = worksheet.Range[row, 5].Number;
                            decimal reOrderLevel = double.IsNaN(rawReorder) ? 0m : (decimal)rawReorder;

                            // 1. Create InventoryItem 
                            var inv = new InventoryItem
                            {
                                Name = name,
                                Unit = worksheet.Range[row, 2].Text?.Trim() ?? "pcs",
                                QuantityOnHand = quantityOnHand,
                                CostPerUnit = costPerUnit,
                                ReOrderLevel = reOrderLevel,
                                IsActive = true
                            };

                            tran.Insert(inv); // Insert to generate inv.Id

                            // 2. Create Initial Stock Transaction
                            if (inv.QuantityOnHand > 0)
                            {
                                tran.Insert(new InventoryTransaction
                                {
                                    InventoryItemId = inv.Id,
                                    QuantityChange = inv.QuantityOnHand, // Positive for Stock In
                                    CostAtTransaction = inv.CostPerUnit,
                                    Reason = "Stock In",
                                    Remarks = $"Initial bulk import for {inv.Name}",
                                    TransactionDate = DateTime.Now
                                });
                            }

                            importedCount++;
                        }
                    });

                    IsBusy = false;

                    // 🟢 Dynamic success message to report imported vs skipped
                    string successMessage = $"{importedCount} raw ingredients have been added to your inventory.";
                    if (skippedCount > 0)
                    {
                        successMessage += $"\n\n{skippedCount} items were skipped because they already exist in the database.";
                    }

                    await PageHelper.DisplayAlertAsync("Import Finished", successMessage, "OK");

                    // Clean up state
                    SelectedInventoryFilePath = string.Empty;
                    SelectedInventoryFileName = "No file selected...";

                }
            }
            catch (Exception ex)
            {
                // Factored in skipped count for accurate row reporting on errors
                await PageHelper.DisplayAlertAsync("Import Error", $"Row {importedCount + skippedCount + 2}: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        [RelayCommand]
        private async Task SelectRecipeBasedInventoryTemplateAsync()
        {
            try
            {
                // 1. Define the Excel file types for each platform
                var excelFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "org.openxmlformats.spreadsheetml.sheet", "com.microsoft.excel.xls" } }, // UTType
                        { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" } }, // MIME type
                        { DevicePlatform.WinUI, new[] { ".xlsx", ".xls" } }, // Extension
                        { DevicePlatform.MacCatalyst, new[] { "xlsx", "xls" } }
                    });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Menu Excel File",
                    FileTypes = excelFileType
                });

                // FIX: Check if the user canceled the picker first!
                if (result == null)
                {
                    return; // User canceled, exit quietly
                }

                // 3. Now validate the file they actually picked
                var expectedHeaders = new List<string> { "Name", "Unit","Quantity on Hand", "Cost Per Unit", "ReOrder Level" };
                var isValid = await IsSyncfusionTemplateValidAsync(result, expectedHeaders,1);

                if (!isValid)
                {
                    // Note: Your IsSyncfusionTemplateValidAsync already shows a DisplayAlert
                    // so you don't necessarily need a second one here unless you want to be generic.
                    return;
                }

                // 4. Update UI/State
                SelectedInventoryFilePath = result.FullPath;
                SelectedInventoryFileName = result.FileName;
                HasRecipeBasedSelected = true;
            }
            catch (Exception ex)
            {
                // Log error (e.g., user denied permissions or system error)
                await PageHelper.DisplayAlertAsync("Error", $"Unable to pick file: {ex.Message}", "OK");
                HasRecipeBasedSelected = false;
            }
        }
        #endregion
        private async Task<bool> IsSyncfusionTemplateValidAsync(FileResult excelFile,List<string> expectedHeaders, int headerRowIndex)
        {    
            
            try
            {
                using (var stream = await excelFile.OpenReadAsync())
                {
                    using (ExcelEngine excelEngine = new ExcelEngine())
                    {
                        IApplication application = excelEngine.Excel;
                        IWorkbook workbook = application.Workbooks.Open(stream);
                        IWorksheet worksheet = workbook.Worksheets[0];

                        for (int i = 0; i < expectedHeaders.Count; i++)
                        {
                            // worksheet.Range[Row, Column]
                            // Column is i + 1 because it's 1-indexed (Column 1 = A)
                            string actualHeader = worksheet.Range[headerRowIndex, i + 1].Text;

                            if (!string.Equals(actualHeader.Trim(), expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                            {
                                await PageHelper.DisplayAlertAsync("Template Error",
                                    $"Invalid Header at {worksheet.Range[headerRowIndex, i + 1].AddressLocal}. " +
                                    $"Expected '{expectedHeaders[i]}' but found '{actualHeader}'.", "OK");
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                await PageHelper.DisplayAlertAsync("Error", "Could not read the file headers.", "OK");
                return false;
            }
        }
    }
}
