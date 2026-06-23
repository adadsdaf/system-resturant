using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;

namespace RestaurantMS.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly DbHelper _db;

    public CustomersController(DbHelper db) => _db = db;

    [HttpGet("/customers")]
    public async Task<IActionResult> Index()
    {
        var customers = await _db.QueryAsync<dynamic>(
            @"SELECT c.customer_id, c.full_name, c.phone, c.email, c.address, c.is_active, c.created_at,
                     COALESCE(la.points_balance, 0) as points,
                     COUNT(o.order_id) as total_orders,
                     COALESCE(SUM(o.total_amount), 0) as total_spent
              FROM customers c
              LEFT JOIN loyalty_accounts la ON c.customer_id=la.customer_id
              LEFT JOIN orders o ON c.customer_id=o.customer_id
              GROUP BY c.customer_id, c.full_name, c.phone, c.email, c.address, c.is_active, c.created_at, la.points_balance
              ORDER BY c.full_name");
        ViewBag.Customers = customers;
        return View();
    }

    [HttpPost("/customers/add")]
    public async Task<IActionResult> Add(string full_name, string? phone, string? email, string? address, string? notes)
    {
        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            var customerId = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn,
                "INSERT INTO customers (full_name, phone, email, address, notes) VALUES (@n, @p, @e, @a, @nt) RETURNING customer_id",
                new { n = full_name, p = phone ?? "", e = email ?? "", a = address ?? "", nt = notes ?? "" }, tx);
            await Dapper.SqlMapper.ExecuteAsync(conn,
                "INSERT INTO loyalty_accounts (customer_id) VALUES (@id)", new { id = customerId }, tx);
            tx.Commit();
            TempData["Success"] = "تم إضافة العميل";
        }
        catch { tx.Rollback(); }
        return RedirectToAction("Index");
    }

    [HttpPost("/customers/update")]
    public async Task<IActionResult> Update(int customer_id, string full_name, string? phone, string? email, string? address, string? notes)
    {
        await _db.ExecuteAsync(
            "UPDATE customers SET full_name=@n, phone=@p, email=@e, address=@a, notes=@nt WHERE customer_id=@id",
            new { n = full_name, p = phone ?? "", e = email ?? "", a = address ?? "", nt = notes ?? "", id = customer_id });
        TempData["Success"] = "تم تحديث بيانات العميل";
        return RedirectToAction("Index");
    }

    [HttpPost("/customers/toggle")]
    public async Task<IActionResult> Toggle(int customer_id)
    {
        await _db.ExecuteAsync("UPDATE customers SET is_active=NOT is_active WHERE customer_id=@id", new { id = customer_id });
        return Json(new { success = true });
    }
}
