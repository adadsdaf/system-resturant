using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class RolePermissionsWindow : Window
{
    private readonly DbHelper _db = new(App.ConnectionString);

    private record RoleItem(int RoleId, string RoleName);

    private List<RoleItem> _roles = new();
    private Dictionary<string, CheckBox> _checkboxes = new();

    public RolePermissionsWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadRolesAsync();
    }

    private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            try { DragMove(); } catch { }
    }

    private void BtnClose_Click(object s, RoutedEventArgs e) => Close();

    // ===== تحميل الأدوار =====
    private async Task LoadRolesAsync()
    {
        try
        {
            var rows = (await _db.QueryAsync<dynamic>("SELECT role_id, role_name FROM roles ORDER BY role_id"))
                       .Select(r => new RoleItem((int)r.role_id, (string)r.role_name))
                       .ToList();
            _roles = rows;

            CmbRoles.ItemsSource   = _roles.Select(r => r.RoleName).ToList();
            CmbRoles.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الأدوار:\n{ex.Message}");
        }
    }

    // ===== عند تغيير الدور =====
    private async void CmbRoles_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (CmbRoles.SelectedIndex < 0 || CmbRoles.SelectedIndex >= _roles.Count) return;
        var role = _roles[CmbRoles.SelectedIndex];
        await BuildPermissionsUiAsync(role);
    }

    private async Task BuildPermissionsUiAsync(RoleItem role)
    {
        PermissionsPanel.Children.Clear();
        _checkboxes.Clear();

        var perms = await PermissionsService.LoadPermissionsAsync(_db, role.RoleId, role.RoleName);

        foreach (var page in PermissionsService.AllPages)
        {
            var arabicName = PermissionsService.GetPageArabicName(page);
            var isAllowed  = perms.TryGetValue(page, out var v) && v;

            var row = new Border
            {
                Background    = isAllowed
                    ? new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4))
                    : new SolidColorBrush(Color.FromRgb(0xFF, 0xF1, 0xF1)),
                BorderBrush   = isAllowed
                    ? new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC))
                    : new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(14, 10, 14, 10),
                Margin          = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new TextBlock
            {
                Text              = GetPageIcon(page),
                FontSize          = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(icon, 0);

            var nameBlock = new TextBlock
            {
                Text              = arabicName,
                FontSize          = 14,
                FontWeight        = FontWeights.SemiBold,
                Foreground        = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameBlock, 1);

            var chk = new CheckBox
            {
                IsChecked         = isAllowed,
                VerticalAlignment = VerticalAlignment.Center,
                LayoutTransform   = new System.Windows.Media.ScaleTransform(1.3, 1.3)
            };
            _checkboxes[page] = chk;
            Grid.SetColumn(chk, 2);

            // تحديث لون الصف عند تغيير الاختيار
            chk.Checked   += (_, _) => UpdateRowStyle(row, true);
            chk.Unchecked += (_, _) => UpdateRowStyle(row, false);

            grid.Children.Add(icon);
            grid.Children.Add(nameBlock);
            grid.Children.Add(chk);
            row.Child = grid;
            PermissionsPanel.Children.Add(row);
        }
    }

    private void UpdateRowStyle(Border row, bool allowed)
    {
        row.Background    = allowed
            ? new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4))
            : new SolidColorBrush(Color.FromRgb(0xFF, 0xF1, 0xF1));
        row.BorderBrush   = allowed
            ? new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC))
            : new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5));
    }

    // ===== حفظ الصلاحيات =====
    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (CmbRoles.SelectedIndex < 0) return;
        var role = _roles[CmbRoles.SelectedIndex];

        var perms = _checkboxes.ToDictionary(kv => kv.Key, kv => kv.Value.IsChecked == true);

        try
        {
            await PermissionsService.SavePermissionsAsync(_db, role.RoleId, perms);
            MessageBox.Show($"✅ تم حفظ صلاحيات دور '{role.RoleName}' بنجاح!\n" +
                             "ستُطبّق على المستخدمين عند تسجيل دخولهم القادم.",
                "حفظ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== إعادة التعيين للافتراضي =====
    private async void BtnReset_Click(object s, RoutedEventArgs e)
    {
        if (CmbRoles.SelectedIndex < 0) return;
        var role = _roles[CmbRoles.SelectedIndex];

        if (MessageBox.Show($"هل تريد إعادة صلاحيات '{role.RoleName}' إلى القيم الافتراضية؟",
                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        // حذف الصلاحيات الحالية وإعادة تعيين الافتراضي
        try
        {
            await _db.ExecuteAsync("DELETE FROM role_permissions WHERE role_id=@rid", new { rid = role.RoleId });
            var defaults = PermissionsService.GetDefaultPermissions(role.RoleName);
            await PermissionsService.SavePermissionsAsync(_db, role.RoleId, defaults);
            await BuildPermissionsUiAsync(role);
            MessageBox.Show("✅ تمت إعادة التعيين للقيم الافتراضية.", "إعادة تعيين",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ:\n{ex.Message}");
        }
    }

    private static string GetPageIcon(string page) => page switch
    {
        "Dashboard"    => "📊",
        "Pos"          => "🛒",
        "Kitchen"      => "👨‍🍳",
        "Menu"         => "📋",
        "Inventory"    => "📦",
        "Customers"    => "👥",
        "Suppliers"    => "🏭",
        "Sales"        => "🧾",
        "Reservations" => "📅",
        "Reports"      => "📈",
        "Admin"        => "⚙️",
        "UserMgmt"     => "👤",
        _              => "📄"
    };
}
