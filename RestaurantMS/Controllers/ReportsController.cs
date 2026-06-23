using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;

namespace RestaurantMS.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly DbHelper _db;

    public ReportsController(DbHelper db) => _db = db;

    [HttpGet("/reports")]
    public IActionResult Index() => View();

    [HttpGet("/reports/api/summary")]
    public async Task<IActionResult> Summary(string? date_from, string? date_to)
    {
        var from = DateTime.TryParse(date_from, out var df) ? df : DateTime.Today;
        var to = DateTime.TryParse(date_to, out var dt) ? dt : DateTime.Today;

        var summary = await _db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT COUNT(*) as total_orders, COALESCE(SUM(total_amount),0) as total_revenue,
                     COALESCE(AVG(total_amount),0) as avg_order,
                     COALESCE(SUM(discount_amount),0) as total_discount,
                     COALESCE(SUM(tax_amount),0) as total_tax
              FROM orders WHERE DATE(created_at) BETWEEN @from AND @to AND order_status='Completed'",
            new { from, to });

        var byDate = await _db.QueryAsync<dynamic>(
            @"SELECT DATE(created_at) as date, COUNT(*) as orders, COALESCE(SUM(total_amount),0) as revenue
              FROM orders WHERE DATE(created_at) BETWEEN @from AND @to AND order_status='Completed'
              GROUP BY DATE(created_at) ORDER BY date",
            new { from, to });

        var byPayment = await _db.QueryAsync<dynamic>(
            @"SELECT payment_method, COUNT(*) as orders, COALESCE(SUM(total_amount),0) as revenue
              FROM orders WHERE DATE(created_at) BETWEEN @from AND @to AND order_status='Completed'
              GROUP BY payment_method ORDER BY revenue DESC",
            new { from, to });

        var topItems = await _db.QueryAsync<dynamic>(
            @"SELECT oi.item_name, SUM(oi.quantity) as qty_sold, SUM(oi.subtotal) as revenue
              FROM order_items oi
              JOIN orders o ON oi.order_id=o.order_id
              WHERE DATE(o.created_at) BETWEEN @from AND @to AND o.order_status='Completed'
              GROUP BY oi.item_name ORDER BY qty_sold DESC LIMIT 10",
            new { from, to });

        var byCategory = await _db.QueryAsync<dynamic>(
            @"SELECT mc.category_name, SUM(oi.quantity) as qty_sold, SUM(oi.subtotal) as revenue
              FROM order_items oi
              JOIN orders o ON oi.order_id=o.order_id
              JOIN menu_items mi ON oi.menu_item_id=mi.item_id
              JOIN menu_categories mc ON mi.category_id=mc.category_id
              WHERE DATE(o.created_at) BETWEEN @from AND @to AND o.order_status='Completed'
              GROUP BY mc.category_name ORDER BY revenue DESC",
            new { from, to });

        return Json(new { summary, by_date = byDate, by_payment = byPayment, top_items = topItems, by_category = byCategory });
    }

    [HttpGet("/reports/api/by_cashier")]
    public async Task<IActionResult> ByCashier(string? date_from, string? date_to)
    {
        var from = DateTime.TryParse(date_from, out var df) ? df : DateTime.Today;
        var to = DateTime.TryParse(date_to, out var dt) ? dt : DateTime.Today;

        var cashiers = await _db.QueryAsync<dynamic>(
            @"SELECT u.user_id, u.full_name, COUNT(o.order_id) as total_orders,
                     COALESCE(SUM(o.total_amount),0) as total_revenue,
                     COALESCE(AVG(o.total_amount),0) as avg_order
              FROM orders o JOIN users u ON o.served_by=u.user_id
              WHERE DATE(o.created_at) BETWEEN @from AND @to AND o.order_status='Completed'
              GROUP BY u.user_id, u.full_name ORDER BY total_revenue DESC",
            new { from, to });

        return Json(cashiers);
    }

    [HttpGet("/reports/api/cashier_orders")]
    public async Task<IActionResult> CashierOrders(int user_id, string? date_from, string? date_to)
    {
        var from = DateTime.TryParse(date_from, out var df) ? df : DateTime.Today;
        var to = DateTime.TryParse(date_to, out var dt) ? dt : DateTime.Today;

        var orders = await _db.QueryAsync<dynamic>(
            @"SELECT order_id, customer_name, total_amount, payment_method, created_at
              FROM orders WHERE served_by=@uid AND DATE(created_at) BETWEEN @from AND @to AND order_status='Completed'
              ORDER BY created_at DESC",
            new { uid = user_id, from, to });

        return Json(orders);
    }

    [HttpGet("/reports/api/low_stock")]
    public async Task<IActionResult> LowStock()
    {
        var items = await _db.QueryAsync<dynamic>(
            @"SELECT ingredient_name, unit, current_stock, min_stock, reorder_point
              FROM ingredients WHERE current_stock<=reorder_point AND is_active=true
              ORDER BY current_stock ASC");
        return Json(items);
    }
}
