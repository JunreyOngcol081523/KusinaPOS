using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KusinaPOS.Models;
using MenuItem = KusinaPOS.Models.MenuItem;
using System.Diagnostics;

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

    }
}
