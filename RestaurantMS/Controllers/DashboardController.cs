using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;

namespace RestaurantMS.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly DbHelper _db;

    public DashboardController(DbHelper db) => _db = db;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        var salesToday = await _db.QueryScalarAsync<decimal>(
            "SELECT COALESCE(SUM(total_amount),0) FROM orders WHERE DATE(created_at)=@today AND order_status='Completed'",
            new { today });

        var ordersToday = await _db.QueryScalarAsync<int>(
            "SELECT COUNT(*) FROM orders WHERE DATE(created_at)=@today AND order_status='Completed'",
            new { today });

        var activeItems = await _db.QueryScalarAsync<int>(
            "SELECT COUNT(*) FROM menu_items WHERE is_available=true");

        var lowStock = await _db.QueryScalarAsync<int>(
            "SELECT COUNT(*) FROM ingredients WHERE current_stock<=min_stock AND is_active=true");

        var pendingKitchen = await _db.QueryScalarAsync<int>(
            "SELECT COUNT(*) FROM kitchen_orders WHERE status IN ('Pending','Preparing')");

        var todayReservations = await _db.QueryScalarAsync<int>(
            "SELECT COUNT(*) FROM reservations WHERE reservation_date=@today AND status NOT IN ('Cancelled','No Show')",
            new { today });

        var recentOrders = await _db.QueryAsync<dynamic>(
            @"SELECT o.order_id, o.customer_name, o.total_amount, o.payment_method,
                     o.order_status, o.created_at, u.full_name as served_by
              FROM orders o JOIN users u ON o.served_by=u.user_id
              ORDER BY o.created_at DESC LIMIT 8");

        ViewBag.Stats = new
        {
            SalesToday = salesToday,
            OrdersToday = ordersToday,
            ActiveItems = activeItems,
            LowStock = lowStock,
            PendingKitchen = pendingKitchen,
            TodayReservations = todayReservations
        };
        ViewBag.RecentOrders = recentOrders;

        return View();
    }
}
