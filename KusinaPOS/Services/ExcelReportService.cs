using KusinaPOS.Models;
using Syncfusion.Drawing;
using Syncfusion.XlsIO;
using Color = Syncfusion.Drawing.Color;
using IApplication = Syncfusion.XlsIO.IApplication;

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
                    worksheet.Range["A1"].ColumnWidth = 15;
                    worksheet.Range["B1"].ColumnWidth = 20;
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
    }
}