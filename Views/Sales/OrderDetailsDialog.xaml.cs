using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Sales;

public partial class OrderDetailsDialog : Window
{
    public OrderDetailsDialog(DbHelper db, int orderId, string currency)
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            var order = await db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT o.*, u.full_name AS served_by
                  FROM orders o JOIN users u ON o.served_by=u.user_id
                  WHERE o.order_id=@id", new { id = orderId });

            if (order == null) return;
            TxtHeader.Text = $"طلب رقم #{order.order_id}";
            TxtInfo.Text   = $"العميل: {order.customer_name}  |  الكاشير: {order.served_by}  |  {((DateTime)order.created_at):dd/MM/yyyy HH:mm}";

            var items = await db.QueryAsync<dynamic>(
                "SELECT item_name, quantity, unit_price, subtotal FROM order_items WHERE order_id=@id",
                new { id = orderId });

            GridItems.ItemsSource = items.Select(i => new
            {
                i.item_name, i.quantity,
                unit_price = $"{i.unit_price:N2} {currency}",
                subtotal   = $"{i.subtotal:N2} {currency}"
            }).ToList();

            TxtTotals.Text = $"المجموع الفرعي: {order.subtotal:N2}  |  خصم: {order.discount_amount:N2}  |  ضريبة: {order.tax_amount:N2}  |  الإجمالي: {order.total_amount:N2} {currency}";
        };
    }
}
