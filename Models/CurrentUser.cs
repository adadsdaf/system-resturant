namespace RestaurantMS.Desktop.Models;

public class CurrentUser
{
    public int    UserId     { get; set; }
    public string Username   { get; set; } = "";
    public string FullName   { get; set; } = "";
    public string RoleName   { get; set; } = "";
    public string BranchName { get; set; } = "";
    public int    BranchId   { get; set; }

    public bool IsOwner   => RoleName == "Owner";
    public bool IsAdmin   => RoleName is "Owner" or "Admin";
    public bool IsManager => RoleName is "Owner" or "Admin" or "Manager";
    public bool IsKitchen => RoleName == "Kitchen";
}
