using RestaurantMS.Desktop.Data;
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
            case "Users":    PanelUsers.Visibility    = Visibility.Visible; await LoadUsersAsync(); break;
            case "Tables":   PanelTables.Visibility   = Visibility.Visible; await LoadTablesAsync(); break;
            case "Settings": PanelSettings.Visibility = Visibility.Visible; await LoadSettingsAsync(); break;
            case "Logs":     PanelLogs.Visibility     = Visibility.Visible; await LoadLogsAsync(); break;
        }
    }

    private void HideAll()
    {
        PanelUsers.Visibility    = Visibility.Collapsed;
        PanelTables.Visibility   = Visibility.Collapsed;
        PanelSettings.Visibility = Visibility.Collapsed;
        PanelLogs.Visibility     = Visibility.Collapsed;
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
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94a3b8")),
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

            var sep = new Separator
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155")),
                Margin = new Thickness(0, 16, 0, 0)
            };
            SettingsPanel.Children.Add(row);
            SettingsPanel.Children.Add(sep);
        }
    }

    // ===== سجل الأحداث =====
    private async Task LoadLogsAsync()
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT TOP 200 al.created_at, al.action, al.details, u.full_name AS user_name
              FROM audit_logs al
              LEFT JOIN users u ON al.user_id = u.user_id
              ORDER BY al.created_at DESC");

        PanelLogs.ItemsSource = rows.Select(r => new
        {
            date_fmt  = ((DateTime)r.created_at).ToString("dd/MM/yyyy HH:mm"),
            user_name = (string)(r.user_name ?? "نظام"),
            action    = (string)r.action,
            details   = (string)(r.details ?? "")
        }).ToList();
    }
}
