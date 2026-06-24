using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Text;

namespace RestaurantMS.Desktop.Services;

public static class DesktopShortcut
{
    public static void Create()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktop, "RestaurantMS.lnk");
            if (File.Exists(shortcutPath)) return;

            var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(exePath)) return;

            var vbsPath = Path.Combine(Path.GetTempPath(), "create_shortcut.vbs");
            var workingDir = Path.GetDirectoryName(exePath) ?? "";
            var vbsContent = $"Set ws = WScript.CreateObject(\"WScript.Shell\")" + "\n" +
                             $"Set s = ws.CreateShortcut(\"{shortcutPath}\")" + "\n" +
                             $"s.TargetPath = \"{exePath}\"" + "\n" +
                             $"s.WorkingDirectory = \"{workingDir}\"" + "\n" +
                             $"s.IconLocation = \"{exePath},0\"" + "\n" +
                             $"s.Save";
            File.WriteAllText(vbsPath, vbsContent, Encoding.GetEncoding(1256));
            
            var psi = new ProcessStartInfo
            {
                FileName = "cscript.exe",
                Arguments = $"//nologo \"{vbsPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = Process.Start(psi);
            p?.WaitForExit(5000);
            
            try { File.Delete(vbsPath); } catch { }
        }
        catch { }
    }
}
