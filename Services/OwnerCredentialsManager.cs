using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RestaurantMS.Desktop.Services;

public class OwnerCredentials
{
    public string OwnerName      { get; set; } = "";
    public string PasswordHash   { get; set; } = "";
    public string SetupDate      { get; set; } = "";
    public string SoftwareName   { get; set; } = "itQAN Soft";
    public string ContactEmail   { get; set; } = "";
    public string ContactPhone   { get; set; } = "";
}

public static class OwnerCredentialsManager
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "itQAN Soft", "RestaurantMS");

    private static readonly string CredFile = Path.Combine(AppDataDir, "owner.dat");

    private static byte[] DeriveKey(string seed)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(seed + "itQAN-Owner-Key-2025!"));
    }

    private static string Encrypt(string plainText)
    {
        var key = DeriveKey("OwnerSecretKey");
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
        var result = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(result);
    }

    private static string? Decrypt(string encrypted)
    {
        try
        {
            var raw = Convert.FromBase64String(encrypted);
            var key = DeriveKey("OwnerSecretKey");
            var iv = new byte[16];
            var cipher = new byte[raw.Length - 16];
            Buffer.BlockCopy(raw, 0, iv, 0, 16);
            Buffer.BlockCopy(raw, 16, cipher, 0, cipher.Length);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV  = iv;
            using var dec = aes.CreateDecryptor();
            var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }
        catch { return null; }
    }

    public static bool IsFirstRun() => !File.Exists(CredFile);

    public static void SaveOwner(OwnerCredentials creds)
    {
        Directory.CreateDirectory(AppDataDir);
        var json = JsonSerializer.Serialize(creds);
        File.WriteAllText(CredFile, Encrypt(json));
    }

    public static OwnerCredentials? LoadOwner()
    {
        if (!File.Exists(CredFile)) return null;
        var enc = File.ReadAllText(CredFile);
        var json = Decrypt(enc);
        if (json == null) return null;
        return JsonSerializer.Deserialize<OwnerCredentials>(json);
    }

    public static bool ValidateOwnerPassword(string password)
    {
        var creds = LoadOwner();
        if (creds == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, creds.PasswordHash);
    }

    public static void SetupOwner(string ownerName, string password, string email, string phone, string software)
    {
        var creds = new OwnerCredentials
        {
            OwnerName    = ownerName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            SetupDate    = DateTime.Now.ToString("yyyy-MM-dd"),
            SoftwareName = string.IsNullOrEmpty(software) ? "itQAN Soft" : software,
            ContactEmail = email,
            ContactPhone = phone
        };
        SaveOwner(creds);
    }

    public static void UpdateOwnerInfo(string ownerName, string email, string phone, string software)
    {
        var creds = LoadOwner() ?? new OwnerCredentials();
        creds.OwnerName    = ownerName;
        creds.ContactEmail = email;
        creds.ContactPhone = phone;
        creds.SoftwareName = string.IsNullOrEmpty(software) ? "itQAN Soft" : software;
        SaveOwner(creds);
    }

    public static void ChangePassword(string newPassword)
    {
        var creds = LoadOwner() ?? new OwnerCredentials();
        creds.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        SaveOwner(creds);
    }
}
