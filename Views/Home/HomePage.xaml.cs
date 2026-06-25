using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RestaurantMS.Desktop.Views.Home;

public partial class HomePage : Page
{
    private readonly DispatcherTimer _timer = new();

    public HomePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object s, RoutedEventArgs e)
    {
        var u = App.CurrentUser;
        if (u != null)
        {
            TxtWelcome.Text = $"مرحباً، {u.FullName}";
            TxtUserInfo.Text = $"{u.RoleName}  •  {u.BranchName}";
        }

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => UpdateClock();
        _timer.Start();
        UpdateClock();

        // تحميل اسم المطعم من قاعدة البيانات
        try
        {
            var db = new DbHelper(App.ConnectionString);
            var name = await db.ExecuteScalarAsync<string>(
                "SELECT TOP 1 value FROM settings WHERE setting_key = 'restaurant_name'");
            TxtLicenseInfo.Text = string.IsNullOrEmpty(name)
                ? "هذا النظام مرخص لـ —"
                : $"هذا النظام مرخص لـ  {name}";
        }
        catch { TxtLicenseInfo.Text = "نظام إدارة المطاعم — itQAN Soft"; }
    }

    private void UpdateClock()
        => TxtDateTime.Text = DateTime.Now.ToString("dddd، dd/MM/yyyy — HH:mm:ss",
            new System.Globalization.CultureInfo("ar-SA"));

    public void StopTimer() => _timer.Stop();
}
