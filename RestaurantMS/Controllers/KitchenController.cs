using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;

namespace RestaurantMS.Controllers;

[Authorize]
public class KitchenController : Controller
{
    private readonly DbHelper _db;

    public KitchenController(DbHelper db) => _db = db;

    [HttpGet("/kitchen")]
    public async Task<IActionResult> Index()
    {
        var orders = await _db.QueryAsync<dynamic>(
            @"SELECT ko.kitchen_order_id, ko.order_id, ko.table_number, ko.customer_name,
                     ko.status, ko.notes, ko.created_at, ko.updated_at
              FROM kitchen_orders ko
              WHERE ko.status NOT IN ('Served','Cancelled')
              ORDER BY ko.created_at ASC");

        foreach (var order in orders)
        {
            var items = await _db.QueryAsync<dynamic>(
                "SELECT item_name, quantity, status FROM kitchen_order_items WHERE kitchen_order_id=@id",
                new { id = (int)order.kitchen_order_id });
            order.Items = items;
        }

        ViewBag.Orders = orders;
        return View();
    }

    [HttpPost("/kitchen/update_status")]
    public async Task<IActionResult> UpdateStatus(int kitchen_order_id, string status)
    {
        await _db.ExecuteAsync(
            "UPDATE kitchen_orders SET status=@s, updated_at=NOW() WHERE kitchen_order_id=@id",
            new { s = status, id = kitchen_order_id });
        return Json(new { success = true });
    }

    [HttpGet("/kitchen/api/orders")]
    public async Task<IActionResult> ApiOrders()
    {
        var orders = await _db.QueryAsync<dynamic>(
            @"SELECT ko.kitchen_order_id, ko.order_id, ko.table_number, ko.customer_name,
                     ko.status, ko.notes, ko.created_at
              FROM kitchen_orders ko
              WHERE ko.status NOT IN ('Served','Cancelled')
              ORDER BY ko.created_at ASC");
        return Json(orders);
    }
}
