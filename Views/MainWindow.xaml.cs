using RestaurantMS.Desktop.Views.Dashboard;
using RestaurantMS.Desktop.Views.Pos;
using RestaurantMS.Desktop.Views.Kitchen;
using RestaurantMS.Desktop.Views.Menu;
using RestaurantMS.Desktop.Views.Inventory;
using RestaurantMS.Desktop.Views.Customers;
using RestaurantMS.Desktop.Views.Suppliers;
using RestaurantMS.Desktop.Views.Sales;
using RestaurantMS.Desktop.Views.Reservations;
using RestaurantMS.Desktop.Views.Reports;
using RestaurantMS.Desktop.Views.Admin;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace RestaurantMS.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clock = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded       += MainWindow_Loaded;
        StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var u = App.CurrentUser;
        if (u == null) { Close(); return; }

        TxtUserName.Text    = u.FullName;
        TxtUserRole.Text    = u.RoleName;
        TxtUserInitial.Text = u.FullName.Length > 0 ? u.FullName[0].ToString() : "م";

        // تطبيق الصلاحيات على قائمة التنقل
        ApplyPermissions();

        // ساعة حية
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick    += (_, _) => TxtClock.Text = DateTime.Now.ToString("HH:mm:ss — dd/MM/yyyy");
        _clock.Start();
        TxtClock.Text = DateTime.Now.ToString("HH:mm:ss — dd/MM/yyyy");

        // الصفحة الافتراضية
        var startPage = GetStartPage();
        Navigate(startPage);
    }

    /// <summary>
    /// تحديد أول صفحة متاحة للمستخدم
    /// </summary>
    private string GetStartPage()
    {
        var u = App.CurrentUser;
        if (u == null) return "Dashboard";

        var pages = new[] { "Dashboard", "Pos", "Kitchen", "Menu", "Inventory",
                            "Customers", "Suppliers", "Sales", "Reservations", "Reports", "Admin" };
        return pages.FirstOrDefault(p => u.CanAccess(p)) ?? "Dashboard";
    }

    /// <summary>
    /// إخفاء أزرار التنقل للصفحات التي لا يملك المستخدم صلاحية الوصول إليها
    /// </summary>
    private void ApplyPermissions()
    {
        var u = App.CurrentUser;
        if (u == null) return;

        foreach (var btn in NavPanel.Children.OfType<Button>())
        {
            if (btn.Tag is string tag)
                btn.Visibility = u.CanAccess(tag) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // ===== إدارة حالة النافذة (تكبير / استعادة / تصغير) =====
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            // منع تغطية شريط المهام عند التكبير مع WindowStyle=None
            MaxHeight = SystemParameters.WorkArea.Height + 16;
            OuterBorder.Margin       = new Thickness(0);
            OuterBorder.CornerRadius = new CornerRadius(0);
            OuterBorder.Effect       = null;
            BtnMaximizeToggle.Content = "❐";
            BtnMaximizeToggle.ToolTip = "استعادة الحجم الطبيعي";
        }
        else if (WindowState == WindowState.Normal)
        {
            MaxHeight = double.PositiveInfinity;
            OuterBorder.Margin       = new Thickness(8);
            OuterBorder.CornerRadius = new CornerRadius(14);
            OuterBorder.Effect       = new DropShadowEffect
            {
                BlurRadius   = 30,
                ShadowDepth  = 8,
                Opacity      = 0.12,
                Color        = System.Windows.Media.Colors.Black
            };
            BtnMaximizeToggle.Content = "□";
            BtnMaximizeToggle.ToolTip = "تكبير";
        }
    }

    // ===== سحب النافذة =====
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                // وضع النافذة عند موقع الضغط
                var mousePos = e.GetPosition(this);
                Left = mousePos.X - (Width / 2);
                Top  = 0;
            }
            try { DragMove(); } catch { }
        }
    }

    // ===== التنقل بين الصفحات =====
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            Navigate(tag);
    }

    public void Navigate(string page)
    {
        var u = App.CurrentUser;
        // التحقق من الصلاحية قبل التنقل
        if (u != null && !u.CanAccess(page)) return;

        Page? content = page switch
        {
            "Dashboard"    => new DashboardPage(),
            "Pos"          => new PosPage(),
            "Kitchen"      => new KitchenPage(),
            "Menu"         => new MenuPage(),
            "Inventory"    => new InventoryPage(),
            "Customers"    => new CustomersPage(),
            "Suppliers"    => new SuppliersPage(),
            "Sales"        => new SalesPage(),
            "Reservations" => new ReservationsPage(),
            "Reports"      => new ReportsPage(),
            "Admin"        => new AdminPage(),
            _              => null
        };

        if (content == null) return;

        MainFrame.Navigate(content);
        TxtPageTitle.Text = GetPageTitle(page);
        UpdateNav(page);
    }

    private static string GetPageTitle(string page) => page switch
    {
        "Dashboard"    => "📊  لوحة التحكم",
        "Pos"          => "🛒  نقطة البيع",
        "Kitchen"      => "👨‍🍳  المطبخ",
        "Menu"         => "📋  إدارة القائمة",
        "Inventory"    => "📦  إدارة المخزون",
        "Customers"    => "👥  إدارة العملاء",
        "Suppliers"    => "🏭  الموردون والمشتريات",
        "Sales"        => "🧾  المبيعات والفواتير",
        "Reservations" => "📅  الحجوزات",
        "Reports"      => "📈  التقارير والإحصائيات",
        "Admin"        => "⚙️  إدارة النظام والإعدادات",
        _              => ""
    };

    private void UpdateNav(string activeTag)
    {
        foreach (var btn in NavPanel.Children.OfType<Button>())
        {
            btn.Style = btn.Tag?.ToString() == activeTag
                ? (Style)Resources["NavBtnActive"]
                : (Style)Resources["NavBtn"];
        }
    }

    // ===== تسجيل الخروج =====
    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        _clock.Stop();
        App.CurrentUser = null;
        new LoginWindow().Show();
        Close();
    }

    // ===== أزرار النافذة =====
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _clock.Stop();
        Application.Current.Shutdown();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
}
