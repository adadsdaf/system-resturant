using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly DbHelper _db;

    public AdminController(DbHelper db) => _db = db;

    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        var users = await _db.QueryAsync<dynamic>(
            @"SELECT u.user_id, u.username, u.full_name, u.email, u.is_active, u.last_login, u.created_at,
                     r.role_name, b.arabic_name as branch_name
              FROM users u
              LEFT JOIN roles r ON u.role_id=r.role_id
              LEFT JOIN branches b ON u.branch_id=b.branch_id
              ORDER BY u.username");

        var roles = await _db.QueryAsync<dynamic>("SELECT role_id, role_name FROM roles ORDER BY role_name");
        var branches = await _db.QueryAsync<dynamic>("SELECT branch_id, arabic_name FROM branches WHERE is_active=true ORDER BY arabic_name");

        var settings = await _db.QueryAsync<dynamic>("SELECT key, value FROM settings ORDER BY key");
        var settingsDict = settings.ToDictionary(s => (string)s.key, s => (string?)s.value ?? "");

        var auditLogs = await _db.QueryAsync<dynamic>(
            @"SELECT al.log_id, al.username, al.action, al.table_name, al.details, al.ip_address, al.created_at
              FROM audit_logs al ORDER BY al.created_at DESC LIMIT 100");

        var tables = await _db.QueryAsync<dynamic>("SELECT * FROM tables ORDER BY table_number");

        ViewBag.Users = users;
        ViewBag.Roles = roles;
        ViewBag.Branches = branches;
        ViewBag.Settings = settingsDict;
        ViewBag.AuditLogs = auditLogs;
        ViewBag.Tables = tables;
        return View();
    }

    [HttpPost("/admin/create_user")]
    public async Task<IActionResult> CreateUser(string username, string full_name, string? email, string password, int role_id, int branch_id)
    {
        var exists = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM users WHERE username=@u", new { u = username });
        if (exists > 0) { TempData["Error"] = "اسم المستخدم مستخدم بالفعل"; return RedirectToAction("Index"); }

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        await _db.ExecuteAsync(
            "INSERT INTO users (username, full_name, email, password_hash, role_id, branch_id, is_active) VALUES (@u, @fn, @e, @h, @r, @b, TRUE)",
            new { u = username, fn = full_name, e = email ?? "", h = hash, r = role_id, b = branch_id });
        TempData["Success"] = "تم إنشاء المستخدم";
        return RedirectToAction("Index");
    }

    [HttpPost("/admin/update_user")]
    public async Task<IActionResult> UpdateUser(int user_id, string full_name, string? email, int role_id, int branch_id)
    {
        await _db.ExecuteAsync(
            "UPDATE users SET full_name=@fn, email=@e, role_id=@r, branch_id=@b WHERE user_id=@id",
            new { fn = full_name, e = email ?? "", r = role_id, b = branch_id, id = user_id });
        TempData["Success"] = "تم تحديث بيانات المستخدم";
        return RedirectToAction("Index");
    }

    [HttpPost("/admin/reset_password")]
    public async Task<IActionResult> ResetPassword(int user_id, string new_password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(new_password);
        await _db.ExecuteAsync("UPDATE users SET password_hash=@h WHERE user_id=@id", new { h = hash, id = user_id });
        return Json(new { success = true });
    }

    [HttpPost("/admin/toggle_user")]
    public async Task<IActionResult> ToggleUser(int user_id)
    {
        await _db.ExecuteAsync("UPDATE users SET is_active=NOT is_active WHERE user_id=@id", new { id = user_id });
        return Json(new { success = true });
    }

    [HttpPost("/admin/save_setting")]
    public async Task<IActionResult> SaveSetting(string key, string value)
    {
        await _db.ExecuteAsync(
            "INSERT INTO settings (key, value) VALUES (@k, @v) ON CONFLICT (key) DO UPDATE SET value=@v",
            new { k = key, v = value });
        return Json(new { success = true });
    }

    [HttpPost("/admin/add_table")]
    public async Task<IActionResult> AddTable(string table_number, int capacity, int? branch_id)
    {
        var bid = branch_id ?? int.Parse(User.FindFirstValue("BranchId") ?? "1");
        await _db.ExecuteAsync(
            "INSERT INTO tables (table_number, capacity, branch_id, is_active) VALUES (@t, @c, @b, TRUE)",
            new { t = table_number, c = capacity, b = bid });
        TempData["Success"] = "تم إضافة الطاولة";
        return RedirectToAction("Index");
    }

    [HttpPost("/admin/delete_table")]
    public async Task<IActionResult> DeleteTable(int table_id)
    {
        await _db.ExecuteAsync("DELETE FROM tables WHERE table_id=@id", new { id = table_id });
        return Json(new { success = true });
    }
}
