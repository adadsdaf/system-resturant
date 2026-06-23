using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize]
public class SuppliersController : Controller
{
    private readonly DbHelper _db;

    public SuppliersController(DbHelper db) => _db = db;

    [HttpGet("/suppliers")]
    public async Task<IActionResult> Index()
    {
        var suppliers = await _db.QueryAsync<dynamic>(
            @"SELECT s.supplier_id, s.supplier_name, s.contact_person, s.phone, s.email, s.address, s.is_active,
                     COUNT(po.po_id) as total_orders, COALESCE(SUM(po.total_amount),0) as total_spent
              FROM suppliers s
              LEFT JOIN purchase_orders po ON s.supplier_id=po.supplier_id
              GROUP BY s.supplier_id, s.supplier_name, s.contact_person, s.phone, s.email, s.address, s.is_active
              ORDER BY s.supplier_name");

        var purchaseOrders = await _db.QueryAsync<dynamic>(
            @"SELECT po.po_id, po.po_number, po.status, po.total_amount, po.created_at, po.received_at,
                     s.supplier_name, u.full_name as created_by_name
              FROM purchase_orders po
              JOIN suppliers s ON po.supplier_id=s.supplier_id
              LEFT JOIN users u ON po.created_by=u.user_id
              ORDER BY po.created_at DESC LIMIT 50");

        var ingredients = await _db.QueryAsync<dynamic>(
            "SELECT ingredient_id, ingredient_name, unit FROM ingredients WHERE is_active=true ORDER BY ingredient_name");

        ViewBag.Suppliers = suppliers;
        ViewBag.PurchaseOrders = purchaseOrders;
        ViewBag.Ingredients = ingredients;
        return View();
    }

    [HttpPost("/suppliers/add")]
    public async Task<IActionResult> Add(string supplier_name, string? contact_person, string? phone, string? email, string? address)
    {
        await _db.ExecuteAsync(
            "INSERT INTO suppliers (supplier_name, contact_person, phone, email, address) VALUES (@n, @c, @p, @e, @a)",
            new { n = supplier_name, c = contact_person ?? "", p = phone ?? "", e = email ?? "", a = address ?? "" });
        TempData["Success"] = "تم إضافة المورد";
        return RedirectToAction("Index");
    }

    [HttpPost("/suppliers/update")]
    public async Task<IActionResult> Update(int supplier_id, string supplier_name, string? contact_person, string? phone, string? email, string? address)
    {
        await _db.ExecuteAsync(
            "UPDATE suppliers SET supplier_name=@n, contact_person=@c, phone=@p, email=@e, address=@a WHERE supplier_id=@id",
            new { n = supplier_name, c = contact_person ?? "", p = phone ?? "", e = email ?? "", a = address ?? "", id = supplier_id });
        TempData["Success"] = "تم تحديث بيانات المورد";
        return RedirectToAction("Index");
    }

    [HttpPost("/suppliers/toggle")]
    public async Task<IActionResult> Toggle(int supplier_id)
    {
        await _db.ExecuteAsync("UPDATE suppliers SET is_active=NOT is_active WHERE supplier_id=@id", new { id = supplier_id });
        return Json(new { success = true });
    }

    [HttpPost("/suppliers/create_po")]
    public async Task<IActionResult> CreatePo([FromBody] CreatePoRequest data)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var poNumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}";
        var total = data.Items?.Sum(i => i.Quantity * i.UnitCost) ?? 0;

        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            var poId = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn,
                @"INSERT INTO purchase_orders (supplier_id, po_number, status, total_amount, notes, created_by)
                  VALUES (@sid, @num, 'Draft', @total, @notes, @uid) RETURNING po_id",
                new { sid = data.SupplierId, num = poNumber, total, notes = data.Notes ?? "", uid = userId }, tx);

            if (data.Items != null)
            {
                foreach (var item in data.Items)
                {
                    await Dapper.SqlMapper.ExecuteAsync(conn,
                        @"INSERT INTO purchase_order_items (po_id, ingredient_id, ingredient_name, quantity, unit_cost, total_cost)
                          VALUES (@pid, @iid, @iname, @q, @uc, @tc)",
                        new { pid = poId, iid = item.IngredientId, iname = item.IngredientName, q = item.Quantity, uc = item.UnitCost, tc = item.Quantity * item.UnitCost }, tx);
                }
            }
            tx.Commit();
            return Json(new { success = true, po_number = poNumber });
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("/suppliers/receive_po")]
    public async Task<IActionResult> ReceivePo(int po_id)
    {
        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            var items = await Dapper.SqlMapper.QueryAsync<dynamic>(conn,
                "SELECT ingredient_id, quantity, unit_cost FROM purchase_order_items WHERE po_id=@id",
                new { id = po_id }, tx);

            foreach (var item in items)
            {
                await Dapper.SqlMapper.ExecuteAsync(conn,
                    "UPDATE ingredients SET current_stock=current_stock+@q, cost_per_unit=@uc, updated_at=NOW() WHERE ingredient_id=@id",
                    new { q = (decimal)item.quantity, uc = (decimal)item.unit_cost, id = (int)item.ingredient_id }, tx);
            }

            await Dapper.SqlMapper.ExecuteAsync(conn,
                "UPDATE purchase_orders SET status='Received', received_at=NOW() WHERE po_id=@id",
                new { id = po_id }, tx);
            tx.Commit();
            TempData["Success"] = "تم استلام أمر الشراء وتحديث المخزون";
        }
        catch { tx.Rollback(); }
        return RedirectToAction("Index");
    }
}

public class PoItem { public int IngredientId { get; set; } public string IngredientName { get; set; } = ""; public decimal Quantity { get; set; } public decimal UnitCost { get; set; } }
public class CreatePoRequest { public int SupplierId { get; set; } public string? Notes { get; set; } public List<PoItem>? Items { get; set; } }
