using KusinaPOS.Models;
using KusinaPOS.Models.SQLViews;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KusinaPOS.Services
{
    public class SalesService
    {
        private readonly SQLiteAsyncConnection _db;

        public SalesService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        #region Initialization
        public async Task InitializeAsync()
        {
            try
            {
                await _db.CreateTableAsync<Sale>();
                await _db.CreateTableAsync<SaleItem>();
                await _db.CreateTableAsync<InventoryTransaction>();
                await _db.CreateTableAsync<MenuItemIngredient>();
                await _db.CreateTableAsync<InventoryItem>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB INIT ERROR] {ex}");
                throw;
            }
        }
        #endregion

        #region CREATE / COMPLETE SALE
        public async Task<int> CompleteSaleAsync(Sale sale, List<SaleItem> saleItems, bool blockOnInsufficientStock = true)
        {
            try
            {
                await InitializeAsync();

                if (sale == null)
                    throw new ArgumentNullException(nameof(sale));
                if (saleItems == null || saleItems.Count == 0)
                    throw new ArgumentException("Sale must have at least one item.");

                int saleId = 0;

                await _db.RunInTransactionAsync(tran =>
                {
                    tran.Insert(sale);
                    saleId = sale.Id;

                    foreach (var item in saleItems)
                    {
                        item.SaleId = saleId;
                        tran.Insert(item);
                    }

                    foreach (var saleItem in saleItems)
                    {
                        var ingredients = tran.Table<MenuItemIngredient>()
                                              .Where(mi => mi.MenuItemId == saleItem.MenuItemId)
                                              .ToList();

                        foreach (var ingredient in ingredients)
                        {
                            decimal totalDeduction = ingredient.QuantityPerMenu * saleItem.Quantity;

                            var inventoryItem = tran.Find<InventoryItem>(ingredient.InventoryItemId);
                            if (inventoryItem == null)
                                throw new Exception($"Inventory item not found: {ingredient.InventoryItemName}");

                            if (blockOnInsufficientStock && inventoryItem.QuantityOnHand < totalDeduction)
                                throw new Exception($"Insufficient stock for {inventoryItem.Name}");

                            inventoryItem.QuantityOnHand -= totalDeduction;
                            tran.Update(inventoryItem);

                            tran.Insert(new InventoryTransaction
                            {
                                InventoryItemId = inventoryItem.Id,
                                SaleId = saleId,
                                QuantityChange = -totalDeduction,
                                Reason = "SALE",
                                Remarks = $"Sold ReceiptNo={sale.ReceiptNo}",
                                TransactionDate = DateTime.Now
                            });
                        }
                    }
                });

                return saleId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[COMPLETE SALE ERROR] {ex}");
                throw;
            }
        }
        #endregion

        #region READ
        public async Task<Sale> GetSaleAsync(int saleId)
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<Sale>().Where(s => s.Id == saleId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET SALE ERROR] {ex}");
                return null;
            }
        }

        public async Task<List<SaleItem>> GetSaleItemsAsync(int saleId)
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<SaleItem>().Where(i => i.SaleId == saleId).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET SALE ITEMS ERROR] {ex}");
                return new List<SaleItem>();
            }
        }

        public async Task<List<Sale>> GetAllSalesAsync()
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<Sale>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET ALL SALES ERROR] {ex}");
                return new List<Sale>();
            }
        }

        public async Task<List<SaleItemWithMenuName>> GetSaleItemsWithMenuNameAsync(int saleId)
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<SaleItemWithMenuName>()
                                .Where(x => x.SaleId == saleId)
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET SALE ITEMS WITH MENU NAME ERROR] {ex}");
                return new List<SaleItemWithMenuName>();
            }
        }

        public async Task<Sale> GetSaleByReceiptNoAsync(string receiptNo)
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<Sale>()
                                .Where(s => s.ReceiptNo == receiptNo)
                                .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET SALE BY RECEIPT NO ERROR] {ex}");
                return null;
            }
        }

        public async Task<List<Sale>> GetSalesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                await InitializeAsync();
                var startOfDay = fromDate.Date;
                var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);

                return await _db.Table<Sale>()
                    .Where(s => s.SaleDate >= startOfDay
                             && s.SaleDate <= endOfDay
                             && s.Status == "Completed")
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET SALES BY DATE RANGE ERROR] {ex}");
                return new List<Sale>();
            }
        }

        public async Task<decimal> GetNetSalesAsync(DateTime fromDate, DateTime toDate)
        {
            await InitializeAsync();

            var start = fromDate.Date;
            var end = toDate.Date.AddDays(1).AddTicks(-1);

            // We include BOTH Completed and Refunded to get the true total
            var sales = await _db.Table<Sale>()
                .Where(s => s.SaleDate >= start
                         && s.SaleDate <= end
                         && (s.Status == "Completed" || s.Status == "Refunded"))
                .ToListAsync();

            // Summing here handles the negative TotalAmount from refunds automatically
            return sales.Sum(s => s.TotalAmount);
        }
        public async Task<int> GetTotalTransactionsCount(DateTime fromDate, DateTime toDate)
        {
            try
            {
                await InitializeAsync();

                var startOfDay = fromDate.Date;
                var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);

                // OPTIMIZED: Ask the DB for the count directly 
                // instead of getting all sales objects into memory.
                return await _db.Table<Sale>()
                    .Where(s => s.SaleDate >= startOfDay
                             && s.SaleDate <= endOfDay
                             && s.Status == "Completed")
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET TOTAL TRANSACTIONS COUNT ERROR] {ex}");
                return 0;
            }
        }
        public async Task<decimal> GetRefundTotalByDateAsync(DateTime start, DateTime end)
        {
            await InitializeAsync();

            // The database is optimized to do this SUM much faster than C# can
            var result = await _db.Table<Sale>()
                .Where(s => s.SaleDate >= start
                         && s.SaleDate <= end
                         && s.Status == "Refunded")
                .ToListAsync();

            return Math.Abs(result.Sum(x => x.TotalAmount));
        }
        #endregion

        #region UPDATE
        public async Task<bool> UpdateSaleAsync(Sale sale)
        {
            try
            {
                await InitializeAsync();
                if (sale == null) return false;

                var rows = await _db.UpdateAsync(sale);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UPDATE SALE ERROR] {ex}");
                return false;
            }
        }

        public async Task<bool> UpdateSaleItemAsync(SaleItem item)
        {
            try
            {
                await InitializeAsync();
                if (item == null) return false;

                var rows = await _db.UpdateAsync(item);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UPDATE SALE ITEM ERROR] {ex}");
                return false;
            }
        }
        #endregion

        #region DELETE
        public async Task<bool> DeleteSaleAsync(int saleId)
        {
            try
            {
                await InitializeAsync();
                await _db.RunInTransactionAsync(tran =>
                {
                    tran.Execute("DELETE FROM InventoryTransaction WHERE SaleId = ?", saleId);
                    tran.Execute("DELETE FROM SaleItem WHERE SaleId = ?", saleId);
                    tran.Execute("DELETE FROM Sale WHERE Id = ?", saleId);
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DELETE SALE ERROR] {ex}");
                return false;
            }
        }

        public async Task<bool> DeleteSaleItemAsync(int saleItemId)
        {
            try
            {
                await InitializeAsync();
                var rows = await _db.DeleteAsync<SaleItem>(saleItemId);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DELETE SALE ITEM ERROR] {ex}");
                return false;
            }
        }
        #endregion

        #region INVENTORY UTILITY
        public async Task AddInventoryAsync(int inventoryItemId, decimal quantity, string reason, string remarks = "")
        {
            try
            {
                await InitializeAsync();
                await _db.RunInTransactionAsync(tran =>
                {
                    var item = tran.Find<InventoryItem>(inventoryItemId);
                    if (item == null) throw new Exception("Inventory item not found");

                    item.QuantityOnHand += quantity;
                    tran.Update(item);

                    tran.Insert(new InventoryTransaction
                    {
                        InventoryItemId = inventoryItemId,
                        SaleId = null,
                        QuantityChange = quantity,
                        Reason = reason,
                        Remarks = remarks,
                        TransactionDate = DateTime.Now
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADD INVENTORY ERROR] {ex}");
                throw;
            }
        }
        #endregion

        #region RECEIPT GENERATION
        public async Task<string> GenerateReceiptNoAsync()
        {
            try
            {
                string year = DateTime.Now.Year.ToString();
                string prefix = $"RCPT-{year}";

                var lastSale = await _db.Table<Sale>()
                                        .Where(s => s.ReceiptNo.StartsWith(prefix))
                                        .OrderByDescending(s => s.Id)
                                        .FirstOrDefaultAsync();

                int nextSequence = 1;

                if (lastSale != null)
                {
                    string numberPart = lastSale.ReceiptNo.Substring(prefix.Length);

                    if (int.TryParse(numberPart, out int lastNumber))
                    {
                        nextSequence = lastNumber + 1;
                    }
                }

                return $"{prefix}{nextSequence:D4}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GENERATE RECEIPT ERROR] {ex}");
                return $"RCPT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            }
        }
        #endregion

        #region VOID / REFUND LOGIC
        public async Task<bool> VoidSaleAsync(Sale originalSale, string reason, string authorizedBy)
        {
            try
            {
                await InitializeAsync();

                if (originalSale == null)
                    throw new ArgumentNullException(nameof(originalSale));

                await _db.RunInTransactionAsync(tran =>
                {
                    var voidSale = new Sale
                    {
                        ReceiptNo = $"VOID-{originalSale.ReceiptNo}",
                        ReferenceReceiptNo = originalSale.ReceiptNo,
                        SaleDate = DateTime.Now,
                        ActionDate = DateTime.Now,
                        TotalAmount = -originalSale.TotalAmount,
                        Status = "Voided",
                        AuthorizedBy = authorizedBy,
                        Reason = reason
                    };
                    tran.Insert(voidSale);

                    originalSale.Status = "Voided";
                    originalSale.ActionDate = DateTime.Now;
                    tran.Update(originalSale);

                    var originalItems = tran.Table<SaleItem>()
                                            .Where(i => i.SaleId == originalSale.Id)
                                            .ToList();

                    foreach (var saleItem in originalItems)
                    {
                        var ingredients = tran.Table<MenuItemIngredient>()
                                              .Where(mi => mi.MenuItemId == saleItem.MenuItemId)
                                              .ToList();

                        foreach (var ingredient in ingredients)
                        {
                            decimal quantityToRestore = ingredient.QuantityPerMenu * saleItem.Quantity;

                            var inventoryItem = tran.Find<InventoryItem>(ingredient.InventoryItemId);
                            if (inventoryItem != null)
                            {
                                inventoryItem.QuantityOnHand += quantityToRestore;
                                tran.Update(inventoryItem);

                                tran.Insert(new InventoryTransaction
                                {
                                    InventoryItemId = inventoryItem.Id,
                                    SaleId = originalSale.Id,
                                    QuantityChange = quantityToRestore,
                                    Reason = "VOID",
                                    Remarks = $"Restocked from Voided Receipt: {originalSale.ReceiptNo}",
                                    TransactionDate = DateTime.Now
                                });
                            }
                        }
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VOID SALE ERROR] {ex}");
                return false;
            }
        }

        public async Task<bool> RefundSaleAsync(Sale originalSale, decimal refundAmount, string customerName, string customerContact, string reason, string authorizedBy)
        {
            try
            {
                await InitializeAsync();

                if (originalSale == null || refundAmount <= 0)
                    return false;

                var refundTransaction = new Sale
                {
                    ReceiptNo = $"REF-{originalSale.ReceiptNo}",
                    SaleDate = DateTime.Now,
                    SubTotal = -refundAmount,
                    Tax = 0,
                    Discount = 0,
                    TotalAmount = -refundAmount,
                    AmountPaid = -refundAmount,
                    ChangeAmount = 0,
                    Status = "Refunded",
                    ActionDate = DateTime.Now,
                    Reason = reason,
                    AuthorizedBy = authorizedBy,
                    CustomerName = customerName,
                    CustomerContact = customerContact,
                    ReferenceReceiptNo = originalSale.ReceiptNo
                };

                await _db.InsertAsync(refundTransaction);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REFUND SALE ERROR] {ex}");
                return false;
            }
        }
        #endregion

        #region DEBUG UTILITY
        public async Task PrintSalesWithItems()
        {
            try
            {
                await InitializeAsync();
                var sales = await _db.Table<Sale>().ToListAsync();

                foreach (var sale in sales)
                {
                    Debug.WriteLine($"Sale ID: {sale.Id}, Date: {sale.SaleDate}, Total: {sale.TotalAmount}");

                    var items = await _db.Table<SaleItem>()
                                         .Where(i => i.SaleId == sale.Id)
                                         .ToListAsync();

                    if (items.Count == 0)
                    {
                        Debug.WriteLine("  No items for this sale.");
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            Debug.WriteLine($"  Item ID: {item.MenuItemId}, Qty: {item.Quantity}, UnitPrice: {item.UnitPrice}, LineTotal: {item.LineTotal}");
                        }
                    }

                    Debug.WriteLine(new string('-', 50));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRINT SALES ERROR] {ex}");
            }
        }
        #endregion
    }
}