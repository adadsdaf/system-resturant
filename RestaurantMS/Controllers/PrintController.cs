using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;

namespace RestaurantMS.Controllers;

[Authorize]
public class PrintController : Controller
{
    private readonly DbHelper _db;

    public PrintController(DbHelper db) => _db = db;

    private async Task<Dictionary<string, string>> GetSettings()
    {
        var settings = await _db.QueryAsync<dynamic>("SELECT key, value FROM settings");
        return settings.ToDictionary(s => (string)s.key, s => (string?)s.value ?? "");
    }

    private async Task<dynamic?> GetOrder(int orderId)
    {
        return await _db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT o.*, u.full_name as cashier_name, b.arabic_name as branch_name, b.phone as branch_phone
              FROM orders o
              LEFT JOIN users u ON o.served_by=u.user_id
              LEFT JOIN branches b ON o.branch_id=b.branch_id
              WHERE o.order_id=@id", new { id = orderId });
    }

    private async Task<IEnumerable<dynamic>> GetOrderItems(int orderId)
    {
        return await _db.QueryAsync<dynamic>(
            "SELECT * FROM order_items WHERE order_id=@id ORDER BY order_item_id", new { id = orderId });
    }

    [HttpGet("/print/customer_receipt/{orderId}")]
    public async Task<IActionResult> CustomerReceipt(int orderId)
    {
        var order = await GetOrder(orderId);
        if (order == null) return NotFound();
        var items = await GetOrderItems(orderId);
        var settings = await GetSettings();
        ViewBag.Order = order;
        ViewBag.Items = items;
        ViewBag.Settings = settings;
        return View();
    }

    [HttpGet("/print/worker_receipt/{orderId}")]
    public async Task<IActionResult> WorkerReceipt(int orderId)
    {
        var order = await GetOrder(orderId);
        if (order == null) return NotFound();
        var items = await GetOrderItems(orderId);
        var settings = await GetSettings();
        ViewBag.Order = order;
        ViewBag.Items = items;
        ViewBag.Settings = settings;
        return View();
    }

    [HttpGet("/print/kitchen_ticket/{orderId}")]
    public async Task<IActionResult> KitchenTicket(int orderId)
    {
        var kitchenOrder = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM kitchen_orders WHERE order_id=@id", new { id = orderId });
        if (kitchenOrder == null) return NotFound();
        var items = await _db.QueryAsync<dynamic>(
            "SELECT * FROM kitchen_order_items WHERE kitchen_order_id=@id", new { id = (int)kitchenOrder.kitchen_order_id });
        var settings = await GetSettings();
        ViewBag.KitchenOrder = kitchenOrder;
        ViewBag.Items = items;
        ViewBag.Settings = settings;
        return View();
    }

    [HttpGet("/print/invoice/{invoiceId}")]
    public async Task<IActionResult> InvoiceCustomer(int invoiceId)
    {
        var invoice = await _db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT si.*, u.full_name as created_by_name
              FROM sales_invoices si LEFT JOIN users u ON si.created_by=u.user_id
              WHERE si.invoice_id=@id", new { id = invoiceId });
        if (invoice == null) return NotFound();
        var settings = await GetSettings();
        ViewBag.Invoice = invoice;
        ViewBag.Settings = settings;
        return View();
    }
}
