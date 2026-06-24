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
using System.Windows.Threading;

namespace RestaurantMS.Desktop.Views;

public partial class MainWindow : Window
{
    private Button? _activeBtn;
    private readonly DispatcherTimer _clock = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var u = App.CurrentUser;
        if (u == null) { Close(); return; }

        TxtUserName.Text    = u.FullName;
        TxtUserRole.Text    = u.RoleName;
        TxtUserInitial.Text = u.FullName.Length > 0 ? u.FullName[0].ToString() : "م";

        // ساعة حية
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick    += (_, _) => TxtClock.Text = DateTime.Now.ToString("HH:mm:ss — dd/MM/yyyy");
        _clock.Start();
        TxtClock.Text = DateTime.Now.ToString("HH:mm:ss — dd/MM/yyyy");

        Navigate("Dashboard");
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            try { DragMove(); } catch { }
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            Navigate(tag);
    }

    public void Navigate(string page)
    {
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

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        _clock.Stop();
        App.CurrentUser = null;
        new LoginWindow().Show();
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _clock.Stop();
        Application.Current.Shutdown();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;
}
