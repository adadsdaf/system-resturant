using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Services;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class AdminPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private string _activeTab = "Users";

    public AdminPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadUsersAsync();
    }

    private async void TabBtn_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not string tag) return;
        _activeTab = tag;
        HideAll();

        switch (tag)
        {
            case "Users":
                PanelUsers.Visibility = Visibility.Visible;
                await LoadUsersAsync();
                break;
            case "Tables":
                PanelTables.Visibility = Visibility.Visible;
                await LoadTablesAsync();
                break;
            case "Settings":
                PanelSettings.Visibility = Visibility.Visible;
                await LoadSettingsAsync();
                break;
            case "Branch":
                PanelBranch.Visibility = Visibility.Visible;
                break;
            case "Currencies":
                PanelCurrencies.Visibility = Visibility.Visible;
                await LoadCurrenciesAsync();
                break;
            case "Devices":
                PanelDevices.Visibility = Visibility.Visible;
                await LoadDevicesAsync();
                break;
            case "Permissions":
                OpenPermissionsWindow();
                break;
            case "Printers":
                OpenPrinterSettings();
                break;
            case "Sessions":
                PanelSessions.Visibility = Visibility.Visible;
                await LoadSessionsAsync();
                break;
            case "Logs":
                PanelLogs.Visibility = Visibility.Visible;
                await LoadLogsAsync();
                break;
        }
    }

    private void HideAll()
    {
        PanelUsers.Visibility      = Visibility.Collapsed;
        PanelTables.Visibility     = Visibility.Collapsed;
        PanelSettings.Visibility   = Visibility.Collapsed;
        PanelBranch.Visibility     = Visibility.Collapsed;
        PanelCurrencies.Visibility = Visibility.Collapsed;
        PanelDevices.Visibility    = Visibility.Collapsed;
        PanelSessions.Visibility   = Visibility.Collapsed;
        PanelLogs.Visibility       = Visibility.Collapsed;
    }

    // ===== المستخدمون =====
    private async Task LoadUsersAsync()
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT u.user_id, u.full_name, u.username, u.is_active, u.last_login,
                     u.role_id, u.branch_id,
                     r.role_name, b.arabic_name AS branch_name
              FROM users u
              JOIN roles    r ON u.role_id   = r.role_id
              JOIN branches b ON u.branch_id = b.branch_id
              ORDER BY u.full_name");

        GridUsers.ItemsSource = rows.Select(r => new
        {
            r.user_id, r.full_name, r.username, r.role_name,
            branch_name = (string)(r.branch_name ?? ""),
            last_login  = r.last_login != null ? ((DateTime)r.last_login).ToString("dd/MM/yyyy HH:mm") : "لم يسجّل",
            status_txt  = ((bool)r.is_active) ? "✅ نشط" : "⛔ موقوف",
            r.is_active
        }).ToList();
    }

    private void BtnAddUser_Click(object s, RoutedEventArgs e)
    {
        var dlg = new UserDialog(_db, null);
        if (dlg.ShowDialog() == true) _ = LoadUsersAsync();
    }

    private void BtnEditUser_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not { } user) return;
        var dlg = new UserDialog(_db, user);
        if (dlg.ShowDialog() == true) _ = LoadUsersAsync();
    }

    private async void BtnToggleUser_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        if (id == App.CurrentUser!.UserId) { MessageBox.Show("لا يمكن إيقاف حسابك الحالي"); return; }
        await _db.ExecuteAsync(
            "UPDATE users SET is_active=CASE WHEN is_active=1 THEN 0 ELSE 1 END WHERE user_id=@id",
            new { id });
        await LoadUsersAsync();
    }

    private async void BtnRefreshUsers_Click(object s, RoutedEventArgs e) => await LoadUsersAsync();

    // ===== الطاولات =====
    private async Task LoadTablesAsync()
    {
        var rows = await _db.QueryAsync<dynamic>(
            "SELECT table_id, table_number, capacity, location, is_active FROM tables ORDER BY table_number");

        GridTables.ItemsSource = rows.Select(r => new
        {
            r.table_id, r.table_number, r.capacity,
            location   = (string)(r.location ?? ""),
            status_txt = ((bool)r.is_active) ? "✅ متاحة" : "⛔ موقوفة",
            r.is_active
        }).ToList();
    }

    private void BtnAddTable_Click(object s, RoutedEventArgs e)
    {
        var dlg = new TableDialog(_db, null);
        if (dlg.ShowDialog() == true) _ = LoadTablesAsync();
    }

    private void BtnEditTable_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not { } table) return;
        var dlg = new TableDialog(_db, table);
        if (dlg.ShowDialog() == true) _ = LoadTablesAsync();
    }

    private async void BtnDeleteTable_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        if (MessageBox.Show("حذف الطاولة؟", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _db.ExecuteAsync("UPDATE tables SET is_active=0 WHERE table_id=@id", new { id });
        await LoadTablesAsync();
    }

    private async void BtnRefreshTables_Click(object s, RoutedEventArgs e) => await LoadTablesAsync();

    // ===== الإعدادات =====
    private async Task LoadSettingsAsync()
    {
        SettingsPanel.Children.Clear();
        var settings = await _db.QueryAsync<dynamic>(
            "SELECT setting_key AS [key], value, description FROM settings ORDER BY setting_key");

        foreach (var setting in settings)
        {
            var row = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            row.Children.Add(new TextBlock
            {
                Text = $"{setting.key} — {setting.description ?? ""}",
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748b")),
                FontSize = 12, Margin = new Thickness(0, 0, 0, 4)
            });

            var key = (string)setting.key;
            var val = (string)(setting.value ?? "");

            var inp = new TextBox
            {
                Text = val, Tag = key,
                Style = (Style)Application.Current.Resources["DarkInput"],
                Height = 40
            };
            row.Children.Add(inp);

            var saveBtn = new Button
            {
                Content = "حفظ", Tag = inp, Margin = new Thickness(0, 8, 0, 0),
                Style = (Style)Application.Current.Resources["GhostButton"], Width = 80
            };
            saveBtn.Click += async (_, _) =>
            {
                var box = (TextBox)((Button)saveBtn).Tag!;
                await _db.ExecuteAsync(
                    "UPDATE settings SET value=@v WHERE setting_key=@k",
                    new { v = box.Text, k = key });
                MessageBox.Show("✅ تم الحفظ");
            };
            row.Children.Add(saveBtn);

            SettingsPanel.Children.Add(row);
            SettingsPanel.Children.Add(new Separator
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E2E8F0")),
                Margin = new Thickness(0, 16, 0, 0)
            });
        }
    }

    // ===== بيانات الفرع =====
    private void BtnOpenBranchSettings_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var win = new BranchSettingsWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}");
        }
    }

    // ===== إدارة العملات =====
    private async Task LoadCurrenciesAsync()
    {
        try
        {
            var tableExists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='currencies'");
            if (tableExists == 0)
            {
                MessageBox.Show("جدول العملات غير موجود.\nيرجى تشغيل Database/update_v3.sql أولاً.",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rows = await _db.QueryAsync<dynamic>(
                @"SELECT currency_id, currency_name, currency_code, currency_symbol,
                         is_local, exchange_rate, is_active
                  FROM currencies ORDER BY is_local DESC, currency_name");

            GridCurrencies.ItemsSource = rows.Select(r => new
            {
                currency_id     = (int)r.currency_id,
                currency_name   = (string)r.currency_name,
                currency_code   = (string)r.currency_code,
                currency_symbol = (string)(r.currency_symbol ?? ""),
                rate_fmt        = ((bool)r.is_local) ? "—  (أساسية)" : $"{(decimal)r.exchange_rate:N4}",
                type_txt        = ((bool)r.is_local) ? "🏠 محلية" : "🌐 أجنبية",
                status_txt      = ((bool)r.is_active) ? "✅ نشطة" : "⛔ موقوفة",
                r.is_local, r.is_active, r.exchange_rate
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل العملات:\n{ex.Message}");
        }
    }

    private async void BtnAddCurrency_Click(object s, RoutedEventArgs e)
    {
        var dlg = new CurrencyDialog(_db, null);
        dlg.Owner = Window.GetWindow(this);
        dlg.ShowDialog();
        if (dlg.Saved) await LoadCurrenciesAsync();
    }

    private async void BtnEditCurrency_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not { } item) return;
        var dlg = new CurrencyDialog(_db, item);
        dlg.Owner = Window.GetWindow(this);
        dlg.ShowDialog();
        if (dlg.Saved) await LoadCurrenciesAsync();
    }

    private async void BtnDeleteCurrency_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        if (MessageBox.Show("حذف هذه العملة؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await _db.ExecuteAsync(
            "UPDATE currencies SET is_active=0 WHERE currency_id=@id", new { id });
        await LoadCurrenciesAsync();
    }

    private async void BtnRefreshCurrencies_Click(object s, RoutedEventArgs e) => await LoadCurrenciesAsync();

    // ===== الأجهزة المسجلة =====
    private async Task LoadDevicesAsync()
    {
        TxtCurrentDevice.Text = DeviceLicenseService.GetCurrentFingerprint();
        try
        {
            var tableExists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='registered_devices'");
            if (tableExists == 0)
            {
                GridDevices.ItemsSource = null;
                MessageBox.Show("جدول الأجهزة غير موجود بعد.\nيرجى تشغيل Database/update_v2.sql أولاً.",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var rows = await _db.QueryAsync<dynamic>(
                @"SELECT device_id, device_fp, device_name, ip_address,
                         CONVERT(NVARCHAR(16), first_seen, 120) AS first_seen,
                         CONVERT(NVARCHAR(16), last_seen,  120) AS last_seen,
                         is_active
                  FROM registered_devices ORDER BY last_seen DESC");

            GridDevices.ItemsSource = rows.Select(r => new
            {
                device_id   = (int)r.device_id,
                device_name = (string)(r.device_name ?? Environment.MachineName),
                device_fp   = (string)r.device_fp,
                ip_address  = (string)(r.ip_address ?? "—"),
                first_seen  = (string)r.first_seen,
                last_seen   = (string)r.last_seen,
                status_txt  = ((bool)r.is_active) ? "✅ نشط" : "⛔ موقوف",
                r.is_active
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الأجهزة:\n{ex.Message}");
        }
    }

    private async void BtnRefreshDevices_Click(object s, RoutedEventArgs e) => await LoadDevicesAsync();

    private async void BtnToggleDevice_Click(object s, RoutedEventArgs e)
    {
        if (GridDevices.SelectedItem == null)
        {
            MessageBox.Show("يرجى تحديد جهاز أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        dynamic item = GridDevices.SelectedItem;
        int deviceId = (int)item.device_id;
        bool isActive = (bool)item.is_active;
        string fp = (string)item.device_fp;
        if (fp == DeviceLicenseService.GetCurrentFingerprint())
        {
            MessageBox.Show("لا يمكن إيقاف الجهاز الحالي.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var action = isActive ? "إيقاف" : "تفعيل";
        if (MessageBox.Show($"هل تريد {action} هذا الجهاز؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await _db.ExecuteAsync(
            "UPDATE registered_devices SET is_active=CASE WHEN is_active=1 THEN 0 ELSE 1 END WHERE device_id=@id",
            new { id = deviceId });
        await LoadDevicesAsync();
    }

    // ===== جلسات الدخول =====
    private async Task LoadSessionsAsync()
    {
        try
        {
            var tableExists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='sessions_log'");
            if (tableExists == 0)
            {
                MessageBox.Show("جدول الجلسات غير موجود.\nيرجى تشغيل Database/update_v3.sql أولاً.",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rows = await _db.QueryAsync<dynamic>(
                @"SELECT TOP 300 s.session_id, s.username, s.device_name, s.ip_address,
                         s.login_time, s.logout_time, s.session_status
                  FROM sessions_log s
                  ORDER BY s.login_time DESC");

            int activeCount = 0;
            var list = rows.Select(r =>
            {
                bool isActive = (string)(r.session_status ?? "نشط") == "نشط" && r.logout_time == null;
                if (isActive) activeCount++;
                return new
                {
                    session_id  = (int)r.session_id,
                    username    = (string)(r.username ?? ""),
                    device_name = (string)(r.device_name ?? "—"),
                    ip_address  = (string)(r.ip_address  ?? "—"),
                    login_time  = r.login_time != null
                        ? ((DateTime)r.login_time).ToString("dd/MM/yyyy HH:mm:ss") : "—",
                    logout_time = r.logout_time != null
                        ? ((DateTime)r.logout_time).ToString("dd/MM/yyyy HH:mm:ss") : "—",
                    status      = isActive ? "🟢 نشط" : "⚫ منتهي"
                };
            }).ToList();

            GridSessions.ItemsSource = list;
            TxtActiveSessionsCount.Text = $"الجلسات النشطة: {activeCount}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الجلسات:\n{ex.Message}");
        }
    }

    private async void BtnRefreshSessions_Click(object s, RoutedEventArgs e) => await LoadSessionsAsync();

    // ===== نافذة الصلاحيات =====
    private void OpenPermissionsWindow()
    {
        try
        {
            var win = new RolePermissionsWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}");
        }
    }

    // ===== نافذة إعدادات الطابعات =====
    private void OpenPrinterSettings()
    {
        try
        {
            var win = new PrinterSettingsWindow();
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}");
        }
    }

    private async void BtnRefreshLogs_Click(object s, RoutedEventArgs e) => await LoadLogsAsync();

    // ===== سجل الأحداث =====
    private async Task LoadLogsAsync()
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT TOP 200 al.created_at, al.action, al.details, u.full_name AS user_name
              FROM audit_logs al
              LEFT JOIN users u ON al.user_id = u.user_id
              ORDER BY al.created_at DESC");

        GridLogs.ItemsSource = rows.Select(r => new
        {
            date_fmt  = ((DateTime)r.created_at).ToString("dd/MM/yyyy HH:mm"),
            user_name = (string)(r.user_name ?? "نظام"),
            action    = (string)r.action,
            details   = (string)(r.details ?? "")
        }).ToList();
    }
}
