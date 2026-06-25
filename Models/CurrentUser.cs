namespace RestaurantMS.Desktop.Models;

public class CurrentUser
{
    public int    UserId     { get; set; }
    public int    RoleId     { get; set; }
    public string Username   { get; set; } = "";
    public string FullName   { get; set; } = "";
    public string RoleName   { get; set; } = "";
    public string BranchName { get; set; } = "";
    public int    BranchId   { get; set; }

    public bool IsOwner        => RoleName == "Owner";
    public bool IsAdmin        => RoleName is "Owner" or "Admin";
    public bool IsManager      => RoleName is "Owner" or "Admin" or "Manager";
    public bool IsStrictManager=> RoleName == "Manager";
    public bool IsKitchen      => RoleName == "Kitchen";

    public Dictionary<string, bool> Permissions { get; set; } = new();

    public bool CanAccess(string pageKey)
    {
        if (IsOwner) return true;
        if (Permissions.TryGetValue(pageKey, out var val)) return val;
        return IsAdmin;
    }

    /// <summary>
    /// هل يمكن لهذا المستخدم إدارة المستخدم الآخر؟
    /// المالك: يدير الجميع | المسؤول: يدير الجميع ما عدا المالك | المدير: يدير من أنشأهم فقط
    /// </summary>
    public bool CanManageUser(string targetRole, int targetCreatedBy)
    {
        if (IsOwner) return true;
        if (IsAdmin) return targetRole != "Owner";
        if (IsStrictManager)
            return targetCreatedBy == UserId
                   && targetRole is not ("Owner" or "Admin" or "Manager");
        return false;
    }
}
