using CommunityToolkit.Mvvm.ComponentModel;
using KusinaPOS.Models;
using KusinaPOS.Models.SQLViews;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MenuItem = KusinaPOS.Models.MenuItem;

namespace KusinaPOS.Services
{
    public class SalesService
    {
        private readonly SQLiteAsyncConnection _db;
        
        private SaleItem saleItems;

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
                await _db.CreateTableAsync<InventoryItem>();
                await _db.CreateTableAsync<InventoryTransaction>();
                await _db.CreateTableAsync<MenuItem>();
                await _db.CreateTableAsync<MenuItemIngredient>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB INIT ERROR] {ex}");
                throw;
            }
        }

        #endregion

        #region CREATE / COMPLETE SALE

        /// <summary>
        /// Complete a sale: saves Sale + SaleItems, deducts inventory, logs InventoryTransactions.
        /// All in a single transaction.
        /// </summary>
        public async Task<int> CompleteSaleAsync(Sale sale, List<SaleItem> saleItems, bool blockOnInsufficientStock = true)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            if (saleItems == null || saleItems.Count == 0) throw new ArgumentException("Sale must have at least one item.");

            int saleId = 0;

            try
            {
                await _db.RunInTransactionAsync(tran =>
                {
                    // 1️⃣ Save sale
                    tran.Insert(sale);
                    saleId = sale.Id;

                    // 2️⃣ Save sale items
                    foreach (var item in saleItems)
                    {
                        item.SaleId = saleId;
                        tran.Insert(item);
                    }

                    // 3️⃣ Deduct inventory & log transactions
                    foreach (var saleItem in saleItems)
                    {
                        // Get all ingredients for the menu item
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

                            // Deduct quantity
                            inventoryItem.QuantityOnHand -= totalDeduction;
                            tran.Update(inventoryItem);

                            // Record inventory transaction
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
                Console.WriteLine($"[COMPLETE SALE ERROR] {ex}");
                throw;
            }
        }

        #endregion

        #region READ

        public async Task<Sale> GetSaleAsync(int saleId)
        {
            try
            {
                return await _db.Table<Sale>().Where(s => s.Id == saleId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET SALE ERROR] {ex}");
                return null;
            }
        }

        public async Task<List<SaleItem>> GetSaleItemsAsync(int saleId)
        {
            try
            {
                return await _db.Table<SaleItem>().Where(i => i.SaleId == saleId).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET SALE ITEMS ERROR] {ex}");
                return new List<SaleItem>();
            }
        }

        public async Task<List<Sale>> GetAllSalesAsync()
        {
            try
            {
                return await _db.Table<Sale>().ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET ALL SALES ERROR] {ex}");
                return new List<Sale>();
            }
        }
        public async Task<List<SaleItemWithMenuName>> GetSaleItemsWithMenuNameAsync(int saleId)
        {
            try
            {
                return await _db.Table<SaleItemWithMenuName>()
                                .Where(x => x.SaleId == saleId)
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading sale items: {ex.Message}");
                return new List<SaleItemWithMenuName>();
            }
        }
        //get sale by ReceiptNo
        public async Task<Sale> GetSaleByReceiptNoAsync(string receiptNo)
        {
            try
            {
                // Added the condition: && s.Status == "Completed"
                return await _db.Table<Sale>()
                                .Where(s => s.ReceiptNo == receiptNo)
                                .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET SALE BY RECEIPT NO ERROR] {ex}");
                return null;
            }
        }

        #endregion

        #region UPDATE

        public async Task<bool> UpdateSaleAsync(Sale sale)
        {
            if (sale == null) return false;

            try
            {
                var rows = await _db.UpdateAsync(sale);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UPDATE SALE ERROR] {ex}");
                return false;
            }
        }

        public async Task<bool> UpdateSaleItemAsync(SaleItem item)
        {
            if (item == null) return false;

            try
            {
                var rows = await _db.UpdateAsync(item);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UPDATE SALE ITEM ERROR] {ex}");
                return false;
            }
        }

        #endregion

        #region DELETE

        public async Task<bool> DeleteSaleAsync(int saleId)
        {
            try
            {
                await _db.RunInTransactionAsync(tran =>
                {
                    // Delete inventory transactions related to this sale
                    tran.Execute("DELETE FROM InventoryTransaction WHERE SaleId = ?", saleId);

                    // Delete sale items
                    tran.Execute("DELETE FROM SaleItem WHERE SaleId = ?", saleId);

                    // Delete sale
                    tran.Execute("DELETE FROM Sale WHERE Id = ?", saleId);
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DELETE SALE ERROR] {ex}");
                return false;
            }
        }

        public async Task<bool> DeleteSaleItemAsync(int saleItemId)
        {
            try
            {
                var rows = await _db.DeleteAsync<SaleItem>(saleItemId);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DELETE SALE ITEM ERROR] {ex}");
                return false;
            }
        }

        #endregion

        #region INVENTORY UTILITY

        /// <summary>
        /// Adds stock to inventory (for restock / adjustment)
        /// </summary>
        public async Task AddInventoryAsync(int inventoryItemId, decimal quantity, string reason, string remarks = "")
        {
            try
            {
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
                Console.WriteLine($"[ADD INVENTORY ERROR] {ex}");
                throw;
            }
        }

        #endregion

        public async Task PrintSalesWithItems()
        {
            try
            {
                // Load all sales asynchronously
                var sales = await _db.Table<Sale>().ToListAsync();

                foreach (var sale in sales)
                {
                    Debug.WriteLine($"Sale ID: {sale.Id}, Date: {sale.SaleDate}, Total: {sale.TotalAmount}");

                    // Load sale items for this sale asynchronously
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
        public async Task<List<Sale>> GetSalesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
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
                Console.WriteLine($"[GET SALES BY DATE RANGE ERROR] {ex}");
                return new List<Sale>();
            }
        }
        public decimal GetTodaySale(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var sales = Task
                    .Run(() => GetSalesByDateRangeAsync(fromDate, toDate))
                    .GetAwaiter()
                    .GetResult();

                decimal totalAmount = 0m;

                foreach (var sale in sales)
                    totalAmount += sale.TotalAmount;

                return totalAmount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET TODAY SALE TOTAL ERROR] {ex}");
                return 0m;
            }
        }
        public int GetTotalTransactionsCount(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var sales = Task
                    .Run(() => GetSalesByDateRangeAsync(fromDate, toDate))
                    .GetAwaiter()
                    .GetResult();
                return sales.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GET TOTAL TRANSACTIONS COUNT ERROR] {ex}");
                return 0;
            }
        }
        /// <summary>
        /// Generates a sequential receipt number (e.g., RCPT-20260001).
        /// Resets the sequence to 0001 at the start of every new year.
        /// </summary>
        public async Task<string> GenerateReceiptNoAsync()
        {
            try
            {
                // 1. Define the prefix based on current year
                string year = DateTime.Now.Year.ToString();
                string prefix = $"RCPT-{year}"; // e.g., "RCPT-2026"

                // 2. Find the last sale that starts with this prefix
                // We order by ID descending to get the most recent one
                var lastSale = await _db.Table<Sale>()
                                        .Where(s => s.ReceiptNo.StartsWith(prefix))
                                        .OrderByDescending(s => s.Id)
                                        .FirstOrDefaultAsync();

                int nextSequence = 1;

                if (lastSale != null)
                {
                    // 3. Extract the numeric part
                    // Format is prefix + number (RCPT-2026 + 0001)
                    // Substring starts after the prefix length
                    string numberPart = lastSale.ReceiptNo.Substring(prefix.Length);

                    if (int.TryParse(numberPart, out int lastNumber))
                    {
                        nextSequence = lastNumber + 1;
                    }
                }

                // 4. Return formatted string
                // D4 pads with zeros: 1 -> 0001, 15 -> 0015
                return $"{prefix}{nextSequence:D4}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GENERATE RECEIPT ERROR] {ex}");
                // Fallback safe receipt if DB fails, to prevent blocking sales
                return $"RCPT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            }
        }

        #region VOID / REFUND LOGIC

        /// <summary>
        /// Process a VOID: Reverses the sale, creates a negative record, and RESTORES inventory.
        /// Use this when the order was cancelled BEFORE the food was cooked/consumed.
        /// </summary>
        public async Task<bool> VoidSaleAsync(Sale originalSale, string reason, string authorizedBy)
        {
            if (originalSale == null) throw new ArgumentNullException(nameof(originalSale));

            try
            {
                await _db.RunInTransactionAsync(tran =>
                {
                    // 1️⃣ Create the Negative "Void" Transaction
                    var voidSale = new Sale
                    {
                        ReceiptNo = $"VOID-{originalSale.ReceiptNo}",
                        ReferenceReceiptNo = originalSale.ReceiptNo,

                        // Dates
                        SaleDate = DateTime.Now,       // The date of this negative transaction
                        ActionDate = DateTime.Now,     // Audit: When the void actually happened

                        // Money
                        TotalAmount = -originalSale.TotalAmount,

                        // Status & Audit
                        Status = "Voided",
                        AuthorizedBy = authorizedBy,
                        Reason = reason

                        // Removed PaymentMethod (not in your model)
                    };
                    tran.Insert(voidSale);

                    // 2️⃣ Update Original Sale
                    originalSale.Status = "Voided";
                    originalSale.ActionDate = DateTime.Now; // Optional: Mark when the original was cancelled
                    tran.Update(originalSale);

                    // 3️⃣ Restore Inventory
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
                Console.WriteLine($"[VOID SALE ERROR] {ex}");
                return false;
            }
        }
        public async Task<bool> RefundSaleAsync(Sale originalSale, decimal refundAmount, string customerName, string customerContact, string reason, string authorizedBy)
        {
            try
            {
                // 1. Safety Checks
                if (originalSale == null || refundAmount <= 0)
                    return false;

                // 2. Create the "Negative" Sale Transaction
                var refundTransaction = new Sale
                {
                    // Generate a unique Refund Receipt No
                    ReceiptNo = $"REF-{originalSale.ReceiptNo}",

                    SaleDate = DateTime.Now,

                    // --- FINANCIALS ---
                    // Key Concept: Negative values reduce your Daily Sales Total automatically.
                    // We apply the whole refund to SubTotal for simplicity (flat adjustment).
                    SubTotal = -refundAmount,
                    Tax = 0,
                    Discount = 0,
                    TotalAmount = -refundAmount,

                    // AmountPaid is negative because money is LEAVING the cash drawer
                    AmountPaid = -refundAmount,
                    ChangeAmount = 0,

                    // --- STATUS & AUDIT ---
                    Status = "Refunded",
                    ActionDate = DateTime.Now, // When the refund happened

                    // Fill in the Audit fields from your UI
                    Reason = reason,
                    AuthorizedBy = authorizedBy,
                    CustomerName = customerName,
                    CustomerContact = customerContact,

                    // --- LINKING ---
                    // This connects this negative row to the original positive row
                    ReferenceReceiptNo = originalSale.ReceiptNo
                };

                // 3. Save to Database
                await _db.InsertAsync(refundTransaction);

                return true;
            }
            catch (Exception ex)
            {
                // Log error here if you have a logger
                System.Diagnostics.Debug.WriteLine($"Refund Error: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}
