using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;

namespace RestaurantMS.Controllers;

[Authorize]
public class BranchesController : Controller
{
    private readonly DbHelper _db;

    public BranchesController(DbHelper db) => _db = db;

    [HttpGet("/branches")]
    public async Task<IActionResult> Index()
    {
        var branches = await _db.QueryAsync<dynamic>("SELECT * FROM branches ORDER BY branch_code");
        ViewBag.Branches = branches;
        return View();
    }

    [HttpPost("/branches/create")]
    public async Task<IActionResult> Create(string branch_code, string arabic_name, string? foreign_name, string? arabic_address, string? phone, string? email, bool is_main = false)
    {
        await _db.ExecuteAsync(
            @"INSERT INTO branches (branch_code, arabic_name, foreign_name, arabic_address, phone, email, is_main, is_active)
              VALUES (@bc, @an, @fn, @aa, @ph, @em, @im, TRUE)",
            new { bc = branch_code, an = arabic_name, fn = foreign_name ?? "", aa = arabic_address ?? "", ph = phone ?? "", em = email ?? "", im = is_main });
        TempData["Success"] = "تم إضافة الفرع";
        return RedirectToAction("Index");
    }

    [HttpPost("/branches/update")]
    public async Task<IActionResult> Update(int branch_id, string branch_code, string arabic_name, string? foreign_name, string? arabic_address, string? phone, string? email)
    {
        await _db.ExecuteAsync(
            @"UPDATE branches SET branch_code=@bc, arabic_name=@an, foreign_name=@fn, arabic_address=@aa,
              phone=@ph, email=@em, updated_at=NOW() WHERE branch_id=@id",
            new { bc = branch_code, an = arabic_name, fn = foreign_name ?? "", aa = arabic_address ?? "", ph = phone ?? "", em = email ?? "", id = branch_id });
        TempData["Success"] = "تم تحديث بيانات الفرع";
        return RedirectToAction("Index");
    }

    [HttpPost("/branches/delete")]
    public async Task<IActionResult> Delete(int branch_id)
    {
        await _db.ExecuteAsync("UPDATE branches SET is_active=FALSE WHERE branch_id=@id", new { id = branch_id });
        return Json(new { success = true });
    }

    [HttpGet("/branches/{id}")]
    public async Task<IActionResult> GetBranch(int id)
    {
        var branch = await _db.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM branches WHERE branch_id=@id", new { id });
        return Json(branch);
    }
}
