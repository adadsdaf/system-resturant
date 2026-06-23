using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private readonly DbHelper _db;

    public ReservationsController(DbHelper db) => _db = db;

    [HttpGet("/reservations")]
    public async Task<IActionResult> Index()
    {
        var reservations = await _db.QueryAsync<dynamic>(
            @"SELECT r.reservation_id, r.customer_name, r.phone, r.party_size, r.reservation_date,
                     r.reservation_time, r.table_number, r.status, r.notes, r.created_at,
                     u.full_name as created_by_name
              FROM reservations r
              LEFT JOIN users u ON r.created_by=u.user_id
              ORDER BY r.reservation_date DESC, r.reservation_time DESC");
        ViewBag.Reservations = reservations;
        return View();
    }

    [HttpPost("/reservations/add")]
    public async Task<IActionResult> Add(string customer_name, string? phone, int party_size, string reservation_date, string reservation_time, string? table_number, string? notes)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        await _db.ExecuteAsync(
            @"INSERT INTO reservations (customer_name, phone, party_size, reservation_date, reservation_time, table_number, notes, created_by)
              VALUES (@cn, @ph, @ps, @rd, @rt, @tn, @nt, @uid)",
            new { cn = customer_name, ph = phone ?? "", ps = party_size, rd = reservation_date, rt = reservation_time, tn = table_number ?? "", nt = notes ?? "", uid = userId });
        TempData["Success"] = "تم إضافة الحجز";
        return RedirectToAction("Index");
    }

    [HttpPost("/reservations/update_status")]
    public async Task<IActionResult> UpdateStatus(int reservation_id, string status)
    {
        await _db.ExecuteAsync(
            "UPDATE reservations SET status=@s WHERE reservation_id=@id",
            new { s = status, id = reservation_id });
        return Json(new { success = true });
    }

    [HttpPost("/reservations/delete")]
    public async Task<IActionResult> Delete(int reservation_id)
    {
        await _db.ExecuteAsync("DELETE FROM reservations WHERE reservation_id=@id", new { id = reservation_id });
        return Json(new { success = true });
    }
}
