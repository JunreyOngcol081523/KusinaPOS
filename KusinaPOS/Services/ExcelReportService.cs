using KusinaPOS.Models;
using Syncfusion.Drawing;
using Syncfusion.XlsIO;
using Color = Syncfusion.Drawing.Color;
using IApplication = Syncfusion.XlsIO.IApplication;
using IRange = Syncfusion.XlsIO.IRange;

namespace KusinaPOS.Services
{
    public class ExcelExportService
    {
        public async Task ExportSalesReportAsync(List<Sale> sales, DateTime fromDate, DateTime toDate, string storeName = "Kusina POS")
        {
            await Task.Run(() =>
            {
                // Create an instance of ExcelEngine
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Xlsx;

                    // Create a workbook with a worksheet
                    IWorkbook workbook = application.Workbooks.Create(1);

                    // Access first worksheet from the workbook instance
                    IWorksheet worksheet = workbook.Worksheets[0];

                    // Disable gridlines in the worksheet
                    worksheet.IsGridLinesVisible = false;

                    // Store Information
                    worksheet.Range["A1"].Text = storeName;
                    worksheet.Range["A1"].CellStyle.Font.Bold = true;
                    worksheet.Range["A1"].CellStyle.Font.Size = 18;

                    worksheet.Range["A2"].Text = "Sales Report";
                    worksheet.Range["A2"].CellStyle.Font.Size = 14;
                    worksheet.Range["A2"].CellStyle.Font.Bold = true;

                    worksheet.Range["A3"].Text = $"Period: {fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}";
                    worksheet.Range["A3"].CellStyle.Font.Size = 11;

                    // Title Section
                    worksheet.Range["A5:H5"].Merge();
                    worksheet.Range["A5"].Text = "SALES REPORT";
                    worksheet.Range["A5"].CellStyle.Font.Bold = true;
                    worksheet.Range["A5"].CellStyle.Font.RGBColor = Color.FromArgb(0, 42, 118, 189);
                    worksheet.Range["A5"].CellStyle.Font.Size = 20;
                    worksheet.Range["A5"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    // Summary Section
                    int summaryRow = 7;
                    worksheet.Range[$"A{summaryRow}"].Text = "Summary";
                    worksheet.Range[$"A{summaryRow}"].CellStyle.Font.Bold = true;
                    worksheet.Range[$"A{summaryRow}"].CellStyle.Font.Size = 14;

                    summaryRow++;
                    worksheet.Range[$"A{summaryRow}"].Text = "Total Sales:";
                    worksheet.Range[$"B{summaryRow}"].Number = (double)sales.Sum(s => s.TotalAmount);
                    worksheet.Range[$"B{summaryRow}"].NumberFormat = "₱#,##0.00";
                    worksheet.Range[$"A{summaryRow}:B{summaryRow}"].CellStyle.Font.Bold = true;

                    summaryRow++;
                    worksheet.Range[$"A{summaryRow}"].Text = "Total Transactions:";
                    worksheet.Range[$"B{summaryRow}"].Number = sales.Count;
                    worksheet.Range[$"A{summaryRow}:B{summaryRow}"].CellStyle.Font.Bold = true;

                    summaryRow++;
                    worksheet.Range[$"A{summaryRow}"].Text = "Total Cash Collected:";
                    worksheet.Range[$"B{summaryRow}"].Number = (double)sales.Sum(s => s.AmountPaid);
                    worksheet.Range[$"B{summaryRow}"].NumberFormat = "₱#,##0.00";
                    worksheet.Range[$"A{summaryRow}:B{summaryRow}"].CellStyle.Font.Bold = true;

                    summaryRow++;
                    worksheet.Range[$"A{summaryRow}"].Text = "Average Sale:";
                    worksheet.Range[$"B{summaryRow}"].Number = sales.Any() ? (double)(sales.Sum(s => s.TotalAmount) / sales.Count) : 0;
                    worksheet.Range[$"B{summaryRow}"].NumberFormat = "₱#,##0.00";
                    worksheet.Range[$"A{summaryRow}:B{summaryRow}"].CellStyle.Font.Bold = true;

                    // Transaction Details Header
                    int headerRow = summaryRow + 2;
                    worksheet.Range[$"A{headerRow}"].Text = "RECEIPT NO";
                    worksheet.Range[$"B{headerRow}"].Text = "DATE & TIME";
                    worksheet.Range[$"B{headerRow}"].ColumnWidth = 200;
                    worksheet.Range[$"C{headerRow}"].Text = "SUBTOTAL";
                    worksheet.Range[$"D{headerRow}"].Text = "DISCOUNT";
                    worksheet.Range[$"E{headerRow}"].Text = "TAX";
                    worksheet.Range[$"F{headerRow}"].Text = "TOTAL";
                    worksheet.Range[$"G{headerRow}"].Text = "PAID";
                    worksheet.Range[$"H{headerRow}"].Text = "CHANGE";

                    // Apply header formatting
                    worksheet.Range[$"A{headerRow}:H{headerRow}"].CellStyle.Color = Color.FromArgb(0, 42, 118, 189);
                    worksheet.Range[$"A{headerRow}:H{headerRow}"].CellStyle.Font.Color = ExcelKnownColors.White;
                    worksheet.Range[$"A{headerRow}:H{headerRow}"].CellStyle.Font.Bold = true;
                    worksheet.Range[$"A{headerRow}:H{headerRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    worksheet.Range[$"A{headerRow}:H{headerRow}"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;

                    // Transaction Data
                    int currentRow = headerRow + 1;
                    foreach (var sale in sales)
                    {
                        worksheet.Range[$"A{currentRow}"].Text = sale.ReceiptNo;
                        worksheet.Range[$"B{currentRow}"].DateTime = sale.SaleDate;
                        worksheet.Range[$"B{currentRow}"].NumberFormat = "MMM dd, yyyy hh:mm AM/PM";
                        worksheet.Range[$"C{currentRow}"].Number = (double)sale.SubTotal;
                        worksheet.Range[$"D{currentRow}"].Number = (double)sale.Discount;
                        worksheet.Range[$"E{currentRow}"].Number = (double)sale.Tax;
                        worksheet.Range[$"F{currentRow}"].Number = (double)sale.TotalAmount;
                        worksheet.Range[$"G{currentRow}"].Number = (double)sale.AmountPaid;
                        worksheet.Range[$"H{currentRow}"].Number = (double)sale.ChangeAmount;

                        currentRow++;
                    }

                    // Apply number format to currency columns
                    int lastDataRow = currentRow - 1;
                    worksheet.Range[$"C{headerRow + 1}:H{lastDataRow}"].NumberFormat = "₱#,##0.00";

                    // Apply borders to data table
                    worksheet.Range[$"A{headerRow}:H{lastDataRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"A{headerRow}:H{lastDataRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"A{headerRow}:H{lastDataRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"A{headerRow}:H{lastDataRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"A{headerRow}:H{lastDataRow}"].CellStyle.Borders.Color = ExcelKnownColors.Grey_25_percent;

                    // Total Row
                    int totalRow = currentRow;
                    worksheet.Range[$"A{totalRow}:E{totalRow}"].Merge();
                    worksheet.Range[$"A{totalRow}"].Text = "GRAND TOTAL";
                    worksheet.Range[$"A{totalRow}"].CellStyle.Font.Bold = true;
                    worksheet.Range[$"A{totalRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                    worksheet.Range[$"F{totalRow}"].Formula = $"=SUM(F{headerRow + 1}:F{lastDataRow})";
                    worksheet.Range[$"G{totalRow}"].Formula = $"=SUM(G{headerRow + 1}:G{lastDataRow})";
                    worksheet.Range[$"H{totalRow}"].Formula = $"=SUM(H{headerRow + 1}:H{lastDataRow})";
                    worksheet.Range[$"F{totalRow}:H{totalRow}"].NumberFormat = "₱#,##0.00";
                    worksheet.Range[$"F{totalRow}:H{totalRow}"].CellStyle.Font.Bold = true;

                    // Apply borders to total row
                    worksheet.Range[$"A{totalRow}:H{totalRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"A{totalRow}:H{totalRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"A{totalRow}:H{totalRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Black;
                    worksheet.Range[$"A{totalRow}:H{totalRow}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Black;

                    // Apply font settings
                    worksheet.Range[$"A1:H{totalRow}"].CellStyle.Font.FontName = "Arial";

                    // Auto-fit columns
                    worksheet.UsedRange.AutofitColumns();

                    // Set minimum column widths
                    worksheet.Range["A1"].ColumnWidth = 20;
                    worksheet.Range["B1"].ColumnWidth = 30;
                    worksheet.Range["C1:H1"].ColumnWidth = 12;

                    // Save the workbook
                    MemoryStream ms = new MemoryStream();
                    workbook.SaveAs(ms);
                    ms.Position = 0;

                    // Save the file
                    string fileName = $"SalesReport_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}.xlsx";
                    SaveService saveService = new SaveService();
                    saveService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ms);
                }
            });
        }

        // In ExcelExportService.cs

        #region Menu excel report
        public async Task ExportMenuPerformanceAsync(
                List<AllMenuItemByCategory> volumeData, // Sheet 1 Data
                List<Top5MenuItem> salesData,           // Sheet 2 Data
                DateTime fromDate,
                DateTime toDate,
                string storeName = "Kusina POS")
        {
            await Task.Run(() =>
            {
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Xlsx;

                    // Create a workbook with 2 Worksheets
                    IWorkbook workbook = application.Workbooks.Create(2);

                    // ===========================================
                    // SHEET 1: VOLUME REPORT (Quantity)
                    // ===========================================
                    IWorksheet sheet1 = workbook.Worksheets[0];
                    sheet1.Name = "By Quantity"; // Tab Name
                    GenerateVolumeSheet(sheet1, volumeData, fromDate, toDate, storeName);

                    // ===========================================
                    // SHEET 2: SALES RANKING (Money)
                    // ===========================================
                    IWorksheet sheet2 = workbook.Worksheets[1];
                    sheet2.Name = "By Sales Value"; // Tab Name
                    GenerateSalesValueSheet(sheet2, salesData, fromDate, toDate, storeName);

                    // Save
                    MemoryStream ms = new MemoryStream();
                    workbook.SaveAs(ms);
                    ms.Position = 0;

                    string fileName = $"MenuReport_Full_{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx";
                    SaveService saveService = new SaveService();
                    saveService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ms);
                }
            });
        }

        // Helper for Sheet 1 (Volume - What we wrote before)
        private void GenerateVolumeSheet(IWorksheet worksheet, List<AllMenuItemByCategory> data, DateTime fromDate, DateTime toDate, string storeName)
        {
            worksheet.IsGridLinesVisible = false;

            // Header Info
            worksheet.Range["A1"].Text = storeName;
            worksheet.Range["A1"].CellStyle.Font.Bold = true;
            worksheet.Range["A1"].CellStyle.Font.Size = 18;

            worksheet.Range["A2"].Text = "Menu Volume Report (Qty)";
            worksheet.Range["A2"].CellStyle.Font.Bold = true;
            worksheet.Range["A2"].CellStyle.Font.Size = 14;

            worksheet.Range["A3"].Text = $"Period: {fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}";

            // Table Header
            int headerRow = 5;
            string[] headers = { "CATEGORY", "ITEM NAME", "QTY SOLD" };

            for (int i = 0; i < headers.Length; i++)
                worksheet.Range[headerRow, i + 1].Text = headers[i];

            // Header Style
            IRange headerRange = worksheet.Range[headerRow, 1, headerRow, 3];
            headerRange.CellStyle.Color = Color.FromArgb(0, 42, 118, 189);
            headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
            headerRange.CellStyle.Font.Bold = true;
            headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            headerRange.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            headerRange.RowHeight = 25;

            // Data
            int currentRow = headerRow + 1;
            foreach (var item in data)
            {
                worksheet.Range[currentRow, 1].Text = item.Category;
                worksheet.Range[currentRow, 2].Text = item.MenuItemName;
                worksheet.Range[currentRow, 3].Number = item.QuantitySold;
                currentRow++;
            }

            // Formatting
            int lastRow = currentRow - 1;
            worksheet.Range[$"C{headerRow + 1}:C{lastRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

            // Borders
            IRange borderRange = worksheet.Range[headerRow, 1, lastRow, 3];
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.InsideVertical].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.InsideHorizontal].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders.Color = ExcelKnownColors.Grey_25_percent;

            // Total
            worksheet.Range[currentRow, 1, currentRow, 2].Merge();
            worksheet.Range[currentRow, 1].Text = "GRAND TOTAL";
            worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
            worksheet.Range[currentRow, 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

            worksheet.Range[currentRow, 3].Formula = $"=SUM(C{headerRow + 1}:C{lastRow})";
            worksheet.Range[currentRow, 3].CellStyle.Font.Bold = true;
            worksheet.Range[currentRow, 3].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

            worksheet.UsedRange.AutofitColumns();
            worksheet.Range["A1"].ColumnWidth = 20;
            worksheet.Range["B1"].ColumnWidth = 35;
        }

        // Helper for Sheet 2 (Sales Value - The New One)
        private void GenerateSalesValueSheet(IWorksheet worksheet, List<Top5MenuItem> data, DateTime fromDate, DateTime toDate, string storeName)
        {
            worksheet.IsGridLinesVisible = false;

            // Header Info
            worksheet.Range["A1"].Text = storeName;
            worksheet.Range["A1"].CellStyle.Font.Bold = true;
            worksheet.Range["A1"].CellStyle.Font.Size = 18;

            worksheet.Range["A2"].Text = "Sales Ranking Report (Value)";
            worksheet.Range["A2"].CellStyle.Font.Bold = true;
            worksheet.Range["A2"].CellStyle.Font.Size = 14;

            worksheet.Range["A3"].Text = $"Period: {fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}";

            // Table Header
            int headerRow = 5;
            worksheet.Range[headerRow, 1].Text = "RANK";
            worksheet.Range[headerRow, 2].Text = "ITEM NAME";
            worksheet.Range[headerRow, 3].Text = "TOTAL SALES";

            // Header Style
            IRange headerRange = worksheet.Range[headerRow, 1, headerRow, 3];
            headerRange.CellStyle.Color = Color.FromArgb(0, 42, 118, 189);
            headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
            headerRange.CellStyle.Font.Bold = true;
            headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            headerRange.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            headerRange.RowHeight = 25;

            // Data
            int currentRow = headerRow + 1;
            int rank = 1;
            foreach (var item in data)
            {
                worksheet.Range[currentRow, 1].Number = rank;
                worksheet.Range[currentRow, 2].Text = item.MenuItemName;
                worksheet.Range[currentRow, 3].Number = (double)item.TotalSales;

                rank++;
                currentRow++;
            }

            // Formatting
            int lastRow = currentRow - 1;
            worksheet.Range[$"A{headerRow + 1}:A{lastRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            worksheet.Range[$"C{headerRow + 1}:C{lastRow}"].NumberFormat = "₱#,##0.00";

            // Borders
            IRange borderRange = worksheet.Range[headerRow, 1, lastRow, 3];
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.InsideVertical].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders[ExcelBordersIndex.InsideHorizontal].LineStyle = ExcelLineStyle.Thin;
            borderRange.CellStyle.Borders.Color = ExcelKnownColors.Grey_25_percent;

            // Total
            worksheet.Range[currentRow, 1, currentRow, 2].Merge();
            worksheet.Range[currentRow, 1].Text = "GRAND TOTAL";
            worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
            worksheet.Range[currentRow, 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

            worksheet.Range[currentRow, 3].Formula = $"=SUM(C{headerRow + 1}:C{lastRow})";
            worksheet.Range[currentRow, 3].CellStyle.Font.Bold = true;
            worksheet.Range[currentRow, 3].NumberFormat = "₱#,##0.00";

            worksheet.UsedRange.AutofitColumns();
            worksheet.Range["B1"].ColumnWidth = 35;
        }
        #endregion
        public async Task ExportInventoryReportAsync(List<InventoryHistoryDto> history, DateTime fromDate, DateTime toDate, string storeName = "Kusina POS")
        {
            await Task.Run(() =>
            {
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Xlsx;

                    IWorkbook workbook = application.Workbooks.Create(1);
                    IWorksheet worksheet = workbook.Worksheets[0];
                    worksheet.IsGridLinesVisible = false;

                    // --- 1. HEADER INFO ---
                    worksheet.Range["A1"].Text = storeName;
                    worksheet.Range["A1"].CellStyle.Font.Bold = true;
                    worksheet.Range["A1"].CellStyle.Font.Size = 18;

                    worksheet.Range["A2"].Text = "Inventory History Report";
                    worksheet.Range["A2"].CellStyle.Font.Size = 14;
                    worksheet.Range["A2"].CellStyle.Font.Bold = true;

                    worksheet.Range["A3"].Text = $"Period: {fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}";
                    worksheet.Range["A3"].CellStyle.Font.Size = 11;


                    // --- 2. TITLE SECTION ---
                    // Merged across 7 columns (A to G) now
                    worksheet.Range["A6:G6"].Merge();
                    worksheet.Range["A6"].Text = "INVENTORY MOVEMENT";
                    worksheet.Range["A6"].CellStyle.Font.Bold = true;
                    worksheet.Range["A6"].CellStyle.Font.RGBColor = Color.FromArgb(0, 42, 118, 189);
                    worksheet.Range["A6"].CellStyle.Font.Size = 20;
                    worksheet.Range["A6"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    // --- 3. SUMMARY SECTION ---
                    int summaryRow = 8;
                    worksheet.Range[$"A{summaryRow}"].Text = "Summary";
                    worksheet.Range[$"A{summaryRow}"].CellStyle.Font.Bold = true;
                    worksheet.Range[$"A{summaryRow}"].CellStyle.Font.Size = 14;

                    summaryRow++;
                    worksheet.Range[$"A{summaryRow}"].Text = "Total Transactions:";
                    worksheet.Range[$"B{summaryRow}"].Number = history.Count;
                    worksheet.Range[$"A{summaryRow}:B{summaryRow}"].CellStyle.Font.Bold = true;

                    summaryRow++;
                    worksheet.Range[$"A{summaryRow}"].Text = "Total Value Moved:";
                    worksheet.Range[$"B{summaryRow}"].Number = (double)history.Sum(x => x.TransactionValue);
                    worksheet.Range[$"B{summaryRow}"].NumberFormat = "₱#,##0.00";
                    worksheet.Range[$"A{summaryRow}:B{summaryRow}"].CellStyle.Font.Bold = true;

                    // --- 4. TABLE HEADERS ---
                    int headerRow = summaryRow + 2;

                    // Define Headers
                    worksheet.Range[$"A{headerRow}"].Text = "DATE & TIME";
                    worksheet.Range[$"B{headerRow}"].Text = "ITEM NAME";
                    worksheet.Range[$"C{headerRow}"].Text = "REASON";
                    worksheet.Range[$"D{headerRow}"].Text = "QTY CHANGE";
                    worksheet.Range[$"E{headerRow}"].Text = "UNIT";
                    worksheet.Range[$"F{headerRow}"].Text = "VALUE";
                    worksheet.Range[$"G{headerRow}"].Text = "REMARKS"; // Added Remarks

                    // Style Headers
                    var headerRange = worksheet.Range[$"A{headerRow}:G{headerRow}"];
                    headerRange.CellStyle.Color = Color.FromArgb(0, 42, 118, 189);
                    headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
                    headerRange.CellStyle.Font.Bold = true;
                    headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    headerRange.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;

                    worksheet.Range[$"A{headerRow}"].ColumnWidth = 22;
                    worksheet.Range[$"B{headerRow}"].ColumnWidth = 25;
                    worksheet.Range[$"G{headerRow}"].ColumnWidth = 30; // Remarks wider

                    // --- 5. DATA ROWS ---
                    int currentRow = headerRow + 1;
                    foreach (var item in history)
                    {
                        // Col A: Date
                        worksheet.Range[$"A{currentRow}"].DateTime = item.TransactionDate;
                        worksheet.Range[$"A{currentRow}"].NumberFormat = "MMM dd, yyyy hh:mm AM/PM";

                        // Col B: Item
                        worksheet.Range[$"B{currentRow}"].Text = item.ItemName;

                        // Col C: Reason
                        worksheet.Range[$"C{currentRow}"].Text = item.Reason;

                        // Col D: Qty Change
                        worksheet.Range[$"D{currentRow}"].Number = (double)item.QuantityChange;
                        worksheet.Range[$"D{currentRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                        // Col E: Unit
                        worksheet.Range[$"E{currentRow}"].Text = item.Unit;
                        worksheet.Range[$"E{currentRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                        // Col F: Transaction Value
                        worksheet.Range[$"F{currentRow}"].Number = (double)item.TransactionValue;
                        worksheet.Range[$"F{currentRow}"].NumberFormat = "₱#,##0.00";

                        // Col G: Remarks
                        worksheet.Range[$"G{currentRow}"].Text = item.Remarks ?? "";
                        worksheet.Range[$"G{currentRow}"].WrapText = true; // Enable wrapping for long remarks

                        currentRow++;
                    }

                    // --- 6. FINAL FORMATTING ---
                    int lastDataRow = currentRow - 1;

                    // Apply Borders
                    var dataRange = worksheet.Range[$"A{headerRow}:G{lastDataRow}"];
                    dataRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    dataRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    dataRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    dataRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    dataRange.CellStyle.Borders.Color = ExcelKnownColors.Grey_25_percent;

                    // Set Font
                    worksheet.Range[$"A1:G{lastDataRow}"].CellStyle.Font.FontName = "Arial";

                    // Auto Fit (Except Remarks which we wrapped)
                    worksheet.Range["A:F"].AutofitColumns();

                    // Save
                    using (MemoryStream ms = new MemoryStream())
                    {
                        workbook.SaveAs(ms);
                        ms.Position = 0;

                        string fileName = $"InventoryReport_{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx";

                        SaveService saveService = new SaveService();
                        saveService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ms);
                    }
                }
            });
        }
    }
}