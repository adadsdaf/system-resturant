using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RestaurantMS.Desktop.Services;

public class LicenseGenerator
{
    public static string GenerateLicense(string customerName, string customerPhone, string customerEmail,
        string restaurantName, string expiryDate, string edition,
        int maxPos, int maxKitchen, int maxCashier, int maxTotal,
        bool canUsePOS, bool canUseKitchen, bool canUseInventory,
        bool canUseCustomers, bool canUseSuppliers, bool canUseSales,
        bool canUseReservations, bool canUseReports, bool canUseAdmin, bool canUseMenu)
    {
        var deviceFp = "PENDING"; // سيتم استبداله ببصمة الجهاز الفعلي عند التنشيط

        var license = new LicenseData
        {
            LicenseKey      = "PENDING",
            CustomerName    = customerName,
            CustomerPhone   = customerPhone,
            CustomerEmail   = customerEmail,
            RestaurantName  = restaurantName,
            ExpiryDate      = expiryDate,
            CreatedAt       = DateTime.Now.ToString("yyyy-MM-dd"),
            MaxPosDevices   = maxPos,
            MaxKitchenDevices = maxKitchen,
            MaxCashierDevices = maxCashier,
            MaxTotalDevices = maxTotal,
            CanUsePOS       = canUsePOS,
            CanUseKitchen   = canUseKitchen,
            CanUseInventory = canUseInventory,
            CanUseCustomers = canUseCustomers,
            CanUseSuppliers = canUseSuppliers,
            CanUseSales     = canUseSales,
            CanUseReservations = canUseReservations,
            CanUseReports   = canUseReports,
CanUseAdmin      = canUseAdmin,
        CanUseMenu      = canUseMenu,
        Edition         = edition,
            DeviceFingerprint = deviceFp
        };

        var json = JsonSerializer.Serialize(license);
        return LicenseManager.Encrypt(json);
    }
}
