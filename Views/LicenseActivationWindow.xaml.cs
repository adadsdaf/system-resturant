using RestaurantMS.Desktop.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantMS.Desktop.Views;

public partial class LicenseActivationWindow : Window
{
    public LicenseActivationWindow()
    {
        InitializeComponent();
        TxtDeviceId.Text = $"معرف الجهاز: {LicenseManager.GetDeviceFingerprint()}";
        Loaded += (_, _) => TxtLicenseKey.Focus();
    }

    private void TxtLicenseKey_Changed(object sender, TextChangedEventArgs e)
    {
        ErrorPanel.Visibility   = Visibility.Collapsed;
        SuccessPanel.Visibility = Visibility.Collapsed;
    }

    private void BtnActivate_Click(object sender, RoutedEventArgs e)
    {
        var key = TxtLicenseKey.Text.Trim();
        if (string.IsNullOrEmpty(key))
        {
            ShowError("يرجى إدخال مفتاح الترخيص");
            return;
        }

        var decrypted = LicenseManager.Decrypt(key);
        if (decrypted == null)
        {
            ShowError("مفتاح الترخيص غير صالح أو تالف");
            return;
        }

        LicenseData? license = null;
        try
        {
            license = System.Text.Json.JsonSerializer.Deserialize<LicenseData>(decrypted);
        }
        catch
        {
            ShowError("خطأ في قراءة بيانات الترخيص");
            return;
        }

        if (license == null)
        {
            ShowError("بيانات الترخيص غير صالحة");
            return;
        }

        if (license.IsExpired)
        {
            ShowError($"انتهت صلاحية الترخيص في {license.ExpiryDate}");
            return;
        }

        var deviceFp = LicenseManager.GetDeviceFingerprint();
        if (!license.IsDeviceMatch(deviceFp))
        {
            ShowError("هذا الترخيص مسجل لجهاز آخر\nلا يمكن استخدامه على هذا الجهاز");
            return;
        }

            LicenseManager.SaveLicense(key);
            DesktopShortcut.Create();
        SuccessPanel.Visibility = Visibility.Visible;
        TxtSuccess.Text = $"العميل: {license.CustomerName}\nالمطعم: {license.RestaurantName}\nالنسخة: {license.Edition}\nتبقى: {license.RemainingDays} يوم";

        BtnActivate.IsEnabled = false;
        BtnActivate.Content = "✅  تم التنشيط";

        _ = Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(2000);
            new MainWindow().Show();
            Close();
        });
    }

    private void ShowError(string msg)
    {
        ErrorPanel.Visibility   = Visibility.Visible;
        SuccessPanel.Visibility = Visibility.Collapsed;
        TxtError.Text = "❌ " + msg;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
