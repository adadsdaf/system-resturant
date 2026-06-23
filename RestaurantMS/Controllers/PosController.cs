using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize]
public class PosController : Controller
{
    private readonly DbHelper _db;

    public PosController(DbHelper db) => _db = db;

    [HttpGet("/pos")]
    public async Task<IActionResult> Index()
    {
        var categories = await _db.QueryAsync<dynamic>(
            @"SELECT DISTINCT mc.category_id, mc.category_name
              FROM menu_categories mc
              JOIN menu_items mi ON mc.category_id=mi.category_id
              WHERE mi.is_available=true ORDER BY mc.category_name");

        var items = await _db.QueryAsync<dynamic>(
            @"SELECT mi.item_id, mi.item_name, mc.category_name, mc.category_id,
                     mi.price, mi.description
              FROM menu_items mi
              JOIN menu_categories mc ON mi.category_id=mc.category_id
              WHERE mi.is_available=true
              ORDER BY mc.category_name, mi.item_name");

        var taxRate = await _db.QueryScalarAsync<string>("SELECT value FROM settings WHERE key='tax_rate'") ?? "0";
        var taxEnabled = await _db.QueryScalarAsync<string>("SELECT value FROM settings WHERE key='tax_enabled'") ?? "0";
        var currency = await _db.QueryScalarAsync<string>("SELECT value FROM settings WHERE key='currency'") ?? "ريال";

        ViewBag.Categories = categories;
        ViewBag.Items = items;
        ViewBag.TaxRate = double.TryParse(taxRate, out var tr) ? tr : 0.0;
        ViewBag.TaxEnabled = taxEnabled == "1";
        ViewBag.Currency = currency;

        return View();
    }

    [HttpGet("/pos/search_customer")]
    public async Task<IActionResult> SearchCustomer(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return Json(null);
        var customer = await _db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT c.customer_id, c.full_name, c.phone,
                     COALESCE(la.points_balance, 0) as points
              FROM customers c
              LEFT JOIN loyalty_accounts la ON c.customer_id=la.customer_id
              WHERE c.phone=@phone",
            new { phone });
        return Json(customer);
    }

    [HttpPost("/pos/save_order")]
    public async Task<IActionResult> SaveOrder([FromBody] SaveOrderRequest data)
    {
        if (data.Cart == null || data.Cart.Count == 0)
            return Json(new { success = false, error = "السلة فارغة" });

        var taxRateStr = await _db.QueryScalarAsync<string>("SELECT value FROM settings WHERE key='tax_rate'") ?? "0";
        var taxEnabledStr = await _db.QueryScalarAsync<string>("SELECT value FROM settings WHERE key='tax_enabled'") ?? "0";
        var taxRate = double.TryParse(taxRateStr, out var tr) ? tr : 0.0;
        var taxEnabled = taxEnabledStr == "1";

        var subtotal = data.Cart.Sum(i => i.Price * i.Quantity);
        var discountAmt = Math.Round(subtotal * data.DiscountPct / 100, 2);
        var taxable = subtotal - discountAmt;
        var taxAmount = taxEnabled ? Math.Round(taxable * taxRate / 100, 2) : 0;
        var totalAmount = Math.Round(taxable + taxAmount, 2);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var branchId = int.TryParse(User.FindFirstValue("BranchId"), out var bid) ? bid : 1;

        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            var orderId = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn,
                @"INSERT INTO orders (customer_name, customer_id, served_by, branch_id, subtotal,
                   discount_amount, tax_amount, total_amount, payment_method, payment_status, order_status)
                   VALUES (@customerName, @customerId, @userId, @branchId, @subtotal,
                           @discountAmt, @taxAmount, @totalAmount, @paymentMethod, 'Paid', 'Completed')
                   RETURNING order_id",
                new { customerName = data.CustomerName ?? "زبون", data.CustomerId, userId, branchId,
                      subtotal, discountAmt, taxAmount, totalAmount, paymentMethod = data.PaymentMethod ?? "Cash" }, tx);

            foreach (var item in data.Cart)
            {
                await Dapper.SqlMapper.ExecuteAsync(conn,
                    @"INSERT INTO order_items (order_id, menu_item_id, item_name, quantity, unit_price, subtotal)
                      VALUES (@orderId, @itemId, @itemName, @qty, @price, @sub)",
                    new { orderId, itemId = item.ItemId, itemName = item.Name, qty = item.Quantity,
                          price = item.Price, sub = item.Price * item.Quantity }, tx);
            }

            await Dapper.SqlMapper.ExecuteAsync(conn,
                @"INSERT INTO payments (order_id, amount_paid, payment_method, reference_no)
                  VALUES (@orderId, @totalAmount, @payMethod, @ref)",
                new { orderId, totalAmount, payMethod = data.PaymentMethod ?? "Cash", @ref = data.ReferenceNo ?? "" }, tx);

            // Kitchen order
            var kitchenId = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn,
                @"INSERT INTO kitchen_orders (order_id, table_number, customer_name, notes)
                  VALUES (@orderId, @table, @cust, @notes) RETURNING kitchen_order_id",
                new { orderId, table = data.TableNumber ?? "", cust = data.CustomerName ?? "زبون",
                      notes = data.Notes ?? "" }, tx);

            foreach (var item in data.Cart)
            {
                await Dapper.SqlMapper.ExecuteAsync(conn,
                    "INSERT INTO kitchen_order_items (kitchen_order_id, item_name, quantity) VALUES (@kid, @name, @qty)",
                    new { kid = kitchenId, name = item.Name, qty = item.Quantity }, tx);
            }

            tx.Commit();
            return Json(new { success = true, order_id = orderId, subtotal, discount_amount = discountAmt, tax_amount = taxAmount, total_amount = totalAmount });
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("/pos/recent_orders")]
    public async Task<IActionResult> RecentOrders()
    {
        var orders = await _db.QueryAsync<dynamic>(
            @"SELECT o.order_id, o.customer_name, o.total_amount, o.payment_method,
                     o.order_status, o.created_at, u.full_name as served_by
              FROM orders o JOIN users u ON o.served_by=u.user_id
              ORDER BY o.created_at DESC LIMIT 20");
        ViewBag.Orders = orders;
        return View();
    }
}

public class CartItem
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public int Quantity { get; set; }
}

public class SaveOrderRequest
{
    public List<CartItem> Cart { get; set; } = new();
    public string? CustomerName { get; set; }
    public int? CustomerId { get; set; }
    public double DiscountPct { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TableNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNo { get; set; }
}
