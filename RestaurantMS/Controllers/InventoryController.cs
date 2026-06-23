using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly DbHelper _db;

    public InventoryController(DbHelper db) => _db = db;

    [HttpGet("/inventory")]
    public async Task<IActionResult> Index()
    {
        var items = await _db.QueryAsync<dynamic>(
            @"SELECT i.ingredient_id, i.ingredient_name, i.unit, i.current_stock, i.min_stock, i.reorder_point,
                     i.cost_per_unit, i.is_active, s.supplier_name
              FROM ingredients i
              LEFT JOIN suppliers s ON i.supplier_id=s.supplier_id
              WHERE i.is_active=true
              ORDER BY i.ingredient_name");
        var suppliers = await _db.QueryAsync<dynamic>("SELECT supplier_id, supplier_name FROM suppliers WHERE is_active=true ORDER BY supplier_name");
        ViewBag.Items = items;
        ViewBag.Suppliers = suppliers;
        return View();
    }

    [HttpPost("/inventory/add")]
    public async Task<IActionResult> Add(string ingredient_name, string unit, decimal current_stock, decimal min_stock, decimal reorder_point, decimal cost_per_unit, int? supplier_id)
    {
        await _db.ExecuteAsync(
            @"INSERT INTO ingredients (ingredient_name, unit, current_stock, min_stock, reorder_point, cost_per_unit, supplier_id, is_active)
              VALUES (@n, @u, @cs, @ms, @rp, @cu, @sid, TRUE)",
            new { n = ingredient_name, u = unit, cs = current_stock, ms = min_stock, rp = reorder_point, cu = cost_per_unit, sid = supplier_id });
        TempData["Success"] = "تم إضافة المادة بنجاح";
        return RedirectToAction("Index");
    }

    [HttpPost("/inventory/update")]
    public async Task<IActionResult> Update(int ingredient_id, string ingredient_name, string unit, decimal min_stock, decimal reorder_point, decimal cost_per_unit, int? supplier_id)
    {
        await _db.ExecuteAsync(
            @"UPDATE ingredients SET ingredient_name=@n, unit=@u, min_stock=@ms, reorder_point=@rp,
              cost_per_unit=@cu, supplier_id=@sid, updated_at=NOW() WHERE ingredient_id=@id",
            new { n = ingredient_name, u = unit, ms = min_stock, rp = reorder_point, cu = cost_per_unit, sid = supplier_id, id = ingredient_id });
        TempData["Success"] = "تم تحديث المادة";
        return RedirectToAction("Index");
    }

    [HttpPost("/inventory/stock_in")]
    public async Task<IActionResult> StockIn(int ingredient_id, decimal quantity, decimal unit_cost, string? reference_no, string? notes)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            await Dapper.SqlMapper.ExecuteAsync(conn,
                "UPDATE ingredients SET current_stock=current_stock+@q, cost_per_unit=@uc, updated_at=NOW() WHERE ingredient_id=@id",
                new { q = quantity, uc = unit_cost, id = ingredient_id }, tx);
            await Dapper.SqlMapper.ExecuteAsync(conn,
                @"INSERT INTO inventory_transactions (ingredient_id, transaction_type, quantity, unit_cost, reference_no, notes, created_by)
                  VALUES (@id, 'StockIn', @q, @uc, @ref, @notes, @uid)",
                new { id = ingredient_id, q = quantity, uc = unit_cost, ref_ = reference_no ?? "", notes = notes ?? "", uid = userId }, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); }
        TempData["Success"] = "تم تسجيل الوارد";
        return RedirectToAction("Index");
    }

    [HttpPost("/inventory/stock_out")]
    public async Task<IActionResult> StockOut(int ingredient_id, decimal quantity, string? notes)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            await Dapper.SqlMapper.ExecuteAsync(conn,
                "UPDATE ingredients SET current_stock=GREATEST(0, current_stock-@q), updated_at=NOW() WHERE ingredient_id=@id",
                new { q = quantity, id = ingredient_id }, tx);
            await Dapper.SqlMapper.ExecuteAsync(conn,
                @"INSERT INTO inventory_transactions (ingredient_id, transaction_type, quantity, notes, created_by)
                  VALUES (@id, 'StockOut', @q, @notes, @uid)",
                new { id = ingredient_id, q = quantity, notes = notes ?? "", uid = userId }, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); }
        TempData["Success"] = "تم تسجيل الصادر";
        return RedirectToAction("Index");
    }

    [HttpPost("/inventory/delete")]
    public async Task<IActionResult> Delete(int ingredient_id)
    {
        await _db.ExecuteAsync("UPDATE ingredients SET is_active=FALSE WHERE ingredient_id=@id", new { id = ingredient_id });
        return Json(new { success = true });
    }

    [HttpGet("/inventory/transactions")]
    public async Task<IActionResult> Transactions()
    {
        var txs = await _db.QueryAsync<dynamic>(
            @"SELECT t.transaction_id, i.ingredient_name, t.transaction_type, t.quantity, t.unit_cost,
                     t.reference_no, t.notes, u.full_name as created_by_name, t.created_at
              FROM inventory_transactions t
              JOIN ingredients i ON t.ingredient_id=i.ingredient_id
              LEFT JOIN users u ON t.created_by=u.user_id
              ORDER BY t.created_at DESC LIMIT 200");
        ViewBag.Transactions = txs;
        return View();
    }
}
