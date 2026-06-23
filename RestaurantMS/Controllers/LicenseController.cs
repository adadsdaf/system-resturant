using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class LicenseController : Controller
{
    private readonly DbHelper _db;

    public LicenseController(DbHelper db) => _db = db;

    [HttpGet("/license")]
    public async Task<IActionResult> Index()
    {
        var settings = await _db.QueryAsync<dynamic>("SELECT setting_key, setting_value FROM license_settings");
        var settingsDict = settings.ToDictionary(s => (string)s.setting_key, s => (string?)s.setting_value ?? "");

        var devices = await _db.QueryAsync<dynamic>(
            "SELECT * FROM licensed_devices ORDER BY created_at DESC");

        ViewBag.Settings = settingsDict;
        ViewBag.Devices = devices;
        return View();
    }

    [HttpPost("/license/add_device")]
    public async Task<IActionResult> AddDevice([FromBody] AddDeviceRequest data)
    {
        var key = GenerateDeviceKey();
        await _db.ExecuteAsync(
            "INSERT INTO licensed_devices (device_name, device_key, mac_address, is_active, notes) VALUES (@n, @k, @m, TRUE, @nt)",
            new { n = data.DeviceName, k = key, m = data.MacAddress ?? "", nt = data.Notes ?? "" });
        return Json(new { success = true, device_key = key });
    }

    [HttpPost("/license/toggle_device")]
    public async Task<IActionResult> ToggleDevice(int device_id)
    {
        await _db.ExecuteAsync("UPDATE licensed_devices SET is_active=NOT is_active WHERE device_id=@id", new { id = device_id });
        return Json(new { success = true });
    }

    [HttpPost("/license/delete_device")]
    public async Task<IActionResult> DeleteDevice(int device_id)
    {
        await _db.ExecuteAsync("DELETE FROM licensed_devices WHERE device_id=@id", new { id = device_id });
        return Json(new { success = true });
    }

    [HttpPost("/license/save_settings")]
    public async Task<IActionResult> SaveSettings([FromBody] Dictionary<string, string> settings)
    {
        foreach (var kv in settings)
        {
            await _db.ExecuteAsync(
                "INSERT INTO license_settings (setting_key, setting_value) VALUES (@k, @v) ON CONFLICT (setting_key) DO UPDATE SET setting_value=@v, updated_at=NOW()",
                new { k = kv.Key, v = kv.Value });
        }
        return Json(new { success = true });
    }

    [HttpGet("/license/generate_key")]
    public IActionResult GenerateKey()
    {
        var key = GenerateDeviceKey();
        return Json(new { key });
    }

    private static string GenerateDeviceKey()
    {
        var guid = Guid.NewGuid().ToString("N").ToUpper();
        return $"IQ-{guid[..8]}-{guid[8..12]}-{guid[12..16]}-{guid[16..20]}";
    }
}

public class AddDeviceRequest
{
    public string DeviceName { get; set; } = "";
    public string? MacAddress { get; set; }
    public string? Notes { get; set; }
}
