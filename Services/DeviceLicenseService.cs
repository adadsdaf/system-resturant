using RestaurantMS.Desktop.Data;
using System.Net;
using System.Net.Sockets;

namespace RestaurantMS.Desktop.Services;

public static class DeviceLicenseService
{
    /// <summary>
    /// يتحقق من تسجيل الجهاز ويسجله تلقائياً إذا لم يكن مسجلاً.
    /// يُستدعى عند تسجيل الدخول.
    /// </summary>
    public static async Task<(bool allowed, string message)> EnsureDeviceRegisteredAsync(DbHelper db)
    {
        // التحقق من وجود الجدول أولاً
        try
        {
            var tableExists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='registered_devices'");
            if (tableExists == 0)
                return (true, ""); // الجدول غير موجود بعد، نسمح بالدخول
        }
        catch { return (true, ""); }

        var fp         = LicenseManager.GetDeviceFingerprint();
        var deviceName = Environment.MachineName;
        var ipAddress  = GetLocalIpAddress();

        try
        {
            // هل الجهاز مسجل؟
            var existing = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT device_id, is_active FROM registered_devices WHERE device_fp = @fp",
                new { fp });

            if (existing != null)
            {
                if (!(bool)existing.is_active)
                    return (false, "⛔ هذا الجهاز موقوف من قِبل المسؤول.\nيرجى التواصل مع مسؤول النظام.");

                // تحديث آخر ظهور وعنوان IP
                await db.ExecuteAsync(
                    "UPDATE registered_devices SET last_seen=GETDATE(), ip_address=@ip WHERE device_fp=@fp",
                    new { ip = ipAddress, fp });

                return (true, "");
            }

            // جهاز جديد — تحقق من الحد الأقصى
            var autoRegStr = await db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='device_auto_register'");
            var autoReg = (autoRegStr ?? "1") == "1";

            if (!autoReg)
                return (false, "⛔ التسجيل التلقائي للأجهزة معطّل.\nيرجى التواصل مع المسؤول لتسجيل هذا الجهاز.");

            var maxStr = await db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='max_devices'");
            var maxDevices = int.TryParse(maxStr, out var m) ? m : 5;

            var activeCount = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM registered_devices WHERE is_active=1");

            if (activeCount >= maxDevices)
                return (false,
                    $"⛔ تم الوصول إلى الحد الأقصى للأجهزة المرخصة ({maxDevices} أجهزة).\n" +
                    $"الأجهزة المسجلة حالياً: {activeCount}.\n" +
                    "يرجى التواصل مع مسؤول النظام لتوسيع الترخيص أو إلغاء تفعيل جهاز آخر.");

            // تسجيل الجهاز تلقائياً
            await db.ExecuteAsync(
                @"INSERT INTO registered_devices (device_fp, device_name, ip_address, device_role)
                  VALUES (@fp, @name, @ip, N'General')",
                new { fp, name = deviceName, ip = ipAddress });

            return (true, $"✅ تم تسجيل الجهاز '{deviceName}' تلقائياً.");
        }
        catch
        {
            // لا نمنع الدخول بسبب خطأ في تسجيل الجهاز
            return (true, "");
        }
    }

    /// <summary>
    /// الحصول على عنوان IP المحلي للجهاز
    /// </summary>
    public static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip   = host.AddressList.FirstOrDefault(
                a => a.AddressFamily == AddressFamily.InterNetwork);
            return ip?.ToString() ?? "unknown";
        }
        catch { return "unknown"; }
    }

    /// <summary>
    /// الحصول على بصمة الجهاز الحالي
    /// </summary>
    public static string GetCurrentFingerprint() => LicenseManager.GetDeviceFingerprint();
}
