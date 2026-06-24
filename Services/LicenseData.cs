namespace RestaurantMS.Desktop.Services;

public class LicenseData
{
    public string LicenseKey          { get; set; } = "";
    public string CustomerName        { get; set; } = "";
    public string CustomerPhone       { get; set; } = "";
    public string CustomerEmail       { get; set; } = "";
    public string RestaurantName      { get; set; } = "";
    public string ExpiryDate          { get; set; } = "";
    public string CreatedAt           { get; set; } = "";
    public int    MaxPosDevices       { get; set; }
    public int    MaxKitchenDevices   { get; set; }
    public int    MaxCashierDevices   { get; set; }
    public int    MaxTotalDevices     { get; set; }
    public bool   CanUsePOS           { get; set; } = true;
    public bool   CanUseKitchen       { get; set; } = true;
    public bool   CanUseInventory     { get; set; } = true;
    public bool   CanUseCustomers     { get; set; } = true;
    public bool   CanUseSuppliers     { get; set; } = true;
    public bool   CanUseSales         { get; set; } = true;
    public bool   CanUseReservations  { get; set; } = true;
    public bool   CanUseReports       { get; set; } = true;
    public bool   CanUseAdmin         { get; set; } = true;
    public bool   CanUseMenu          { get; set; } = true;
    public string Edition             { get; set; } = "Full";
    public string DeviceFingerprint   { get; set; } = "ANY";

    public bool IsExpired => !string.IsNullOrEmpty(ExpiryDate)
                             && DateTime.TryParse(ExpiryDate, out var d)
                             && d < DateTime.Now;

    public int RemainingDays
    {
        get
        {
            if (string.IsNullOrEmpty(ExpiryDate)) return 9999;
            if (!DateTime.TryParse(ExpiryDate, out var d)) return 9999;
            var days = (d - DateTime.Now).Days;
            return days > 0 ? days : 0;
        }
    }

    public bool IsDeviceMatch(string currentFingerprint)
        => DeviceFingerprint == "ANY"
           || DeviceFingerprint == "PENDING"
           || DeviceFingerprint == currentFingerprint;

    public bool IsValid => !IsExpired && IsDeviceMatch(LicenseManager.GetDeviceFingerprint());
}
