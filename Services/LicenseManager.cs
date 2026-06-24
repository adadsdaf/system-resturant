using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace RestaurantMS.Desktop.Services;

public static class LicenseManager
{
    private static readonly string RegPath = @"SOFTWARE\itQAN Soft\RestaurantMS";
    private static readonly string RegKey  = "LicenseData";

    private static readonly byte[] _aesKey = DeriveKey("itQAN-RestaurantMS-2025-SecretKey-AES256!");
    private static readonly byte[] _hmacKey = DeriveKey("itQAN-RestaurantMS-2025-HMAC-Secret!");

    private static byte[] DeriveKey(string seed)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
    }

    public static string GetDeviceFingerprint()
    {
        var sb = new StringBuilder();
        try
        {
            sb.Append(Environment.MachineName ?? "UNK"); } catch { sb.Append("UNK"); }
        try { sb.Append("|").Append(Environment.UserName ?? "UNK"); } catch { sb.Append("|UNK"); }
        try { sb.Append("|").Append(Environment.ProcessorCount); } catch { sb.Append("|0"); }
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            sb.Append("|").Append(key?.GetValue("ProcessorNameString")?.ToString() ?? "UNK");
        }
        catch { sb.Append("|UNK"); }
        try
        {
            var sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!string.IsNullOrEmpty(sysDir))
            {
                var driveRoot = System.IO.Path.GetPathRoot(sysDir);
                if (!string.IsNullOrEmpty(driveRoot))
                {
                    var drive = new System.IO.DriveInfo(driveRoot);
                    sb.Append("|").Append(drive.VolumeLabel ?? "UNK");
                }
            }
        }
        catch { sb.Append("|UNK"); }

        var raw = sb.ToString();
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..16].ToUpperInvariant();
    }

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _aesKey;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        using var hmac = new HMACSHA256(_hmacKey);
        var sig = hmac.ComputeHash(cipherBytes);

        var result = new byte[aes.IV.Length + cipherBytes.Length + sig.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        Buffer.BlockCopy(sig, 0, result, aes.IV.Length + cipherBytes.Length, sig.Length);

        return "RMS-" + Convert.ToBase64String(result);
    }

    public static string? Decrypt(string encrypted)
    {
        try
        {
            if (!encrypted.StartsWith("RMS-")) return null;
            var raw = Convert.FromBase64String(encrypted[4..]);

            if (raw.Length < 64) return null;
            var iv = new byte[16];
            var sig = new byte[32];
            var cipher = new byte[raw.Length - 16 - 32];

            Buffer.BlockCopy(raw, 0, iv, 0, 16);
            Buffer.BlockCopy(raw, 16, cipher, 0, cipher.Length);
            Buffer.BlockCopy(raw, 16 + cipher.Length, sig, 0, 32);

            using var hmac = new HMACSHA256(_hmacKey);
            var expectedSig = hmac.ComputeHash(cipher);
            if (!sig.SequenceEqual(expectedSig)) return null;

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch { return null; }
    }

    public static void SaveLicense(string encryptedLicense)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegPath);
            key?.SetValue(RegKey, encryptedLicense, RegistryValueKind.String);
        }
        catch { }
    }

    public static string? LoadLicense()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath);
            return key?.GetValue(RegKey)?.ToString();
        }
        catch { return null; }
    }

    public static void RemoveLicense()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKey(RegPath, false);
        }
        catch { }
    }
}
