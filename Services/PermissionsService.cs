using RestaurantMS.Desktop.Data;

namespace RestaurantMS.Desktop.Services;

public static class PermissionsService
{
    public static readonly string[] AllPages =
    {
        "Dashboard", "Pos", "Kitchen", "Menu", "Inventory",
        "Customers", "Suppliers", "Sales", "Reservations", "Reports", "Admin", "UserMgmt"
    };

    /// <summary>
    /// تحميل صلاحيات الدور من قاعدة البيانات.
    /// يرجع إلى القيم الافتراضية إذا لم تكن البيانات موجودة.
    /// </summary>
    public static async Task<Dictionary<string, bool>> LoadPermissionsAsync(DbHelper db, int roleId, string roleName)
    {
        try
        {
            var tableExists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='role_permissions'");
            if (tableExists == 0)
                return GetDefaultPermissions(roleName);

            var rows = (await db.QueryAsync<dynamic>(
                "SELECT page_key, is_allowed FROM role_permissions WHERE role_id=@rid",
                new { rid = roleId })).ToList();

            if (rows.Count == 0)
                return GetDefaultPermissions(roleName);

            var result = rows.ToDictionary(
                r => (string)r.page_key,
                r => (bool)r.is_allowed);

            // تأكد من وجود UserMgmt في القاموس للتوافق مع الإصدارات القديمة
            if (!result.ContainsKey("UserMgmt"))
                result["UserMgmt"] = roleName is "Owner" or "Admin" or "Manager";

            return result;
        }
        catch
        {
            return GetDefaultPermissions(roleName);
        }
    }

    /// <summary>
    /// حفظ صلاحيات دور معين في قاعدة البيانات
    /// </summary>
    public static async Task SavePermissionsAsync(DbHelper db, int roleId, Dictionary<string, bool> permissions)
    {
        foreach (var kv in permissions)
        {
            var exists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM role_permissions WHERE role_id=@rid AND page_key=@pk",
                new { rid = roleId, pk = kv.Key });

            if (exists > 0)
                await db.ExecuteAsync(
                    "UPDATE role_permissions SET is_allowed=@v WHERE role_id=@rid AND page_key=@pk",
                    new { v = kv.Value, rid = roleId, pk = kv.Key });
            else
                await db.ExecuteAsync(
                    "INSERT INTO role_permissions (role_id, page_key, is_allowed) VALUES (@rid, @pk, @v)",
                    new { rid = roleId, pk = kv.Key, v = kv.Value });
        }
    }

    /// <summary>
    /// الصلاحيات الافتراضية لكل دور
    /// </summary>
    public static Dictionary<string, bool> GetDefaultPermissions(string roleName) => roleName switch
    {
        "Owner" or "Admin" =>
            AllPages.ToDictionary(p => p, _ => true),

        "Manager" =>
            AllPages.ToDictionary(p => p, p => p switch
            {
                "Admin"    => true,   // المدير يدخل صفحة الإدارة لإدارة موظفيه
                "UserMgmt" => true,   // صلاحية إدارة الموظفين المُنشَئين من قِبَله
                _          => true
            }),

        "Cashier" => new Dictionary<string, bool>
        {
            ["Dashboard"]   = true,  ["Pos"]          = true,
            ["Kitchen"]     = false, ["Menu"]         = false,
            ["Inventory"]   = false, ["Customers"]    = true,
            ["Suppliers"]   = false, ["Sales"]        = true,
            ["Reservations"]= false, ["Reports"]      = false,
            ["Admin"]       = false, ["UserMgmt"]     = false,
        },

        "Kitchen" => new Dictionary<string, bool>
        {
            ["Dashboard"]   = true,  ["Pos"]          = false,
            ["Kitchen"]     = true,  ["Menu"]         = false,
            ["Inventory"]   = false, ["Customers"]    = false,
            ["Suppliers"]   = false, ["Sales"]        = false,
            ["Reservations"]= false, ["Reports"]      = false,
            ["Admin"]       = false, ["UserMgmt"]     = false,
        },

        "Waiter" => new Dictionary<string, bool>
        {
            ["Dashboard"]   = true,  ["Pos"]          = true,
            ["Kitchen"]     = false, ["Menu"]         = false,
            ["Inventory"]   = false, ["Customers"]    = true,
            ["Suppliers"]   = false, ["Sales"]        = false,
            ["Reservations"]= true,  ["Reports"]      = false,
            ["Admin"]       = false, ["UserMgmt"]     = false,
        },

        _ => AllPages.ToDictionary(p => p, _ => false)
    };

    /// <summary>
    /// الاسم العربي للصفحة
    /// </summary>
    public static string GetPageArabicName(string pageKey) => pageKey switch
    {
        "Dashboard"    => "لوحة التحكم",
        "Pos"          => "نقطة البيع (الكاشير)",
        "Kitchen"      => "المطبخ",
        "Menu"         => "القائمة والأصناف",
        "Inventory"    => "المخزون",
        "Customers"    => "العملاء",
        "Suppliers"    => "الموردون",
        "Sales"        => "المبيعات",
        "Reservations" => "الحجوزات",
        "Reports"      => "التقارير",
        "Admin"        => "الإدارة والإعدادات",
        "UserMgmt"     => "إدارة الموظفين",
        _              => pageKey
    };
}
