using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

public class AuthController : Controller
{
    private readonly DbHelper _db;

    public AuthController(DbHelper db) => _db = db;

    [HttpGet("/login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "يرجى إدخال اسم المستخدم وكلمة المرور";
            return View();
        }

        var user = await _db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT u.user_id, u.username, u.full_name, u.password_hash, u.is_active,
                     u.branch_id, r.role_name as role
              FROM users u LEFT JOIN roles r ON u.role_id=r.role_id
              WHERE u.username=@username",
            new { username });

        if (user == null)
        {
            ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة";
            return View();
        }

        if (!(bool)user.is_active)
        {
            ViewBag.Error = "هذا الحساب معطل. يرجى التواصل مع المدير";
            return View();
        }

        string passwordHash = (string)user.password_hash;
        bool valid = false;
        try { valid = BCrypt.Net.BCrypt.Verify(password, passwordHash); }
        catch { valid = password == passwordHash; }

        if (!valid)
        {
            ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة";
            return View();
        }

        // Create session record
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var sessionId = await _db.ExecuteScalarAsync<int>(
            @"INSERT INTO user_sessions (user_id, username, ip_address, branch_id, session_status)
              VALUES (@uid, @uname, @ip, @bid, 'active') RETURNING session_id",
            new { uid = (int)user.user_id, uname = (string)user.username, ip, bid = user.branch_id });

        // Update last login
        await _db.ExecuteAsync("UPDATE users SET last_login=NOW() WHERE user_id=@uid",
            new { uid = (int)user.user_id });

        // Sign in with cookie
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, ((int)user.user_id).ToString()),
            new(ClaimTypes.Name, (string)user.username),
            new("FullName", (string)user.full_name),
            new(ClaimTypes.Role, (string)(user.role ?? "Cashier")),
            new("BranchId", (user.branch_id ?? 1).ToString()),
            new("SessionId", sessionId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) });

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet("/logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionIdStr = User.FindFirstValue("SessionId");
        if (int.TryParse(sessionIdStr, out int sid))
        {
            await _db.ExecuteAsync(
                "UPDATE user_sessions SET logout_time=NOW(), session_status='closed' WHERE session_id=@sid",
                new { sid });
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
