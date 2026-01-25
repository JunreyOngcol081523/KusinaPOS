using SQLite;
namespace KusinaPOS.Models.SQLViews
{
    [Table("View_SaleItemsWithMenuName")]
    public class SaleItemWithMenuName
    {
        [PrimaryKey]
        public int Id { get; set; }

        public int SaleId { get; set; }

        public int MenuItemId { get; set; }

        public string MenuItemName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}