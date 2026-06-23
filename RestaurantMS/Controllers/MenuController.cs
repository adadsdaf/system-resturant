using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize]
public class MenuController : Controller
{
    private readonly DbHelper _db;

    public MenuController(DbHelper db) => _db = db;

    [HttpGet("/menu")]
    public async Task<IActionResult> Index()
    {
        var categories = await _db.QueryAsync<dynamic>("SELECT * FROM menu_categories ORDER BY sort_order, category_name");
        var items = await _db.QueryAsync<dynamic>(
            @"SELECT mi.item_id, mi.item_name, mc.category_name, mc.category_id, mi.price, mi.cost_price,
                     mi.description, mi.is_available
              FROM menu_items mi
              JOIN menu_categories mc ON mi.category_id=mc.category_id
              ORDER BY mc.category_name, mi.item_name");
        ViewBag.Categories = categories;
        ViewBag.Items = items;
        return View();
    }

    [HttpPost("/menu/add_item")]
    public async Task<IActionResult> AddItem(int category_id, string item_name, string? description, decimal price, decimal cost_price)
    {
        await _db.ExecuteAsync(
            "INSERT INTO menu_items (category_id, item_name, description, price, cost_price) VALUES (@c, @n, @d, @p, @cp)",
            new { c = category_id, n = item_name, d = description ?? "", p = price, cp = cost_price });
        TempData["Success"] = "تم إضافة الصنف بنجاح";
        return RedirectToAction("Index");
    }

    [HttpPost("/menu/update_item")]
    public async Task<IActionResult> UpdateItem(int item_id, int category_id, string item_name, string? description, decimal price, decimal cost_price)
    {
        await _db.ExecuteAsync(
            "UPDATE menu_items SET category_id=@c, item_name=@n, description=@d, price=@p, cost_price=@cp, updated_at=NOW() WHERE item_id=@id",
            new { c = category_id, n = item_name, d = description ?? "", p = price, cp = cost_price, id = item_id });
        TempData["Success"] = "تم تحديث الصنف";
        return RedirectToAction("Index");
    }

    [HttpPost("/menu/toggle_item")]
    public async Task<IActionResult> ToggleItem(int item_id)
    {
        await _db.ExecuteAsync("UPDATE menu_items SET is_available=NOT is_available WHERE item_id=@id", new { id = item_id });
        return Json(new { success = true });
    }

    [HttpPost("/menu/delete_item")]
    public async Task<IActionResult> DeleteItem(int item_id)
    {
        await _db.ExecuteAsync("DELETE FROM menu_items WHERE item_id=@id", new { id = item_id });
        return Json(new { success = true });
    }

    [HttpPost("/menu/add_category")]
    public async Task<IActionResult> AddCategory(string category_name, string? description)
    {
        await _db.ExecuteAsync(
            "INSERT INTO menu_categories (category_name, description) VALUES (@n, @d)",
            new { n = category_name, d = description ?? "" });
        TempData["Success"] = "تم إضافة التصنيف";
        return RedirectToAction("Index");
    }

    [HttpPost("/menu/delete_category")]
    public async Task<IActionResult> DeleteCategory(int category_id)
    {
        var count = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM menu_items WHERE category_id=@id", new { id = category_id });
        if (count > 0)
            return Json(new { success = false, error = "لا يمكن حذف تصنيف يحتوي على أصناف" });
        await _db.ExecuteAsync("DELETE FROM menu_categories WHERE category_id=@id", new { id = category_id });
        return Json(new { success = true });
    }
}
