using KusinaPOS.Models;

public static class OrderItemMapper
{
    public static List<SaleItem> ToSaleItems(
        IEnumerable<OrderItem> orderItems)
    {
        if (orderItems == null)
            return new List<SaleItem>();

        return orderItems
            .Where(o => o.Quantity > 0)
            .Select(o => new SaleItem
            {
                MenuItemId = o.MenuItemId,
                Quantity = o.Quantity,
                UnitPrice = o.Price
                // SaleId is NOT set here
            })
            .ToList();
    }
}
