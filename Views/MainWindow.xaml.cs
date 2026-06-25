using RestaurantMS.Desktop.Data;
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
using RestaurantMS.Desktop.Views.Home;
using RestaurantMS.Desktop.Views.Profile;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace RestaurantMS.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clock = new();
    private string _activePage = "Home";

    public MainWindow()
    {
        InitializeComponent();
        Loaded       += MainWindow_Loaded;
        StateChanged += MainWindow_StateChanged;
    }

    // ===================================================================
    //  تهيئة النافذة
    // ===================================================================
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var u = App.CurrentUser;
        if (u == null) { Close(); return; }

        // معلومات المستخدم في شريط العنوان
        TxtUserName.Text    = u.FullName;
        TxtUserRole.Text    = u.RoleName;
        TxtUserInitial.Text = u.FullName.Length > 0 ? u.FullName[0].ToString() : "م";

        // شريط الحالة
        TxtStatusUser.Text   = $"👤 {u.FullName}  [{u.RoleName}]";
        TxtStatusBranch.Text = $"🏪 {u.BranchName}";

        // ساعة حية
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick    += (_, _) => UpdateClocks();
        _clock.Start();
        UpdateClocks();

        // بناء واجهة التنقل بناءً على الدور
        BuildLeftSidebar();
        BuildNavTree();

        // تحميل اسم المطعم للترخيص
        await LoadRestaurantNameAsync();

        // الصفحة الافتراضية
        Navigate("Home");
    }

    private void UpdateClocks()
    {
        var now = DateTime.Now;
        TxtClock.Text      = now.ToString("HH:mm:ss");
        TxtStatusTime.Text = now.ToString("dd/MM/yyyy   HH:mm");
    }

    private async Task LoadRestaurantNameAsync()
    {
        try
        {
            var db   = new DbHelper(App.ConnectionString);
            var name = await db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='restaurant_name'");
            if (!string.IsNullOrEmpty(name))
            {
                TxtLicenseBadge.Text    = "هذا النظام مرخص لـ";
                TxtRestaurantBadge.Text = name;
            }
        }
        catch { /* تجاهل — الافتراضي كافٍ */ }
    }

    // ===================================================================
    //  بناء الشريط الجانبي الأيسر (بناءً على الدور)
    // ===================================================================
    private void BuildLeftSidebar()
    {
        LeftSidePanel.Children.Clear();
        var u = App.CurrentUser;
        if (u == null) return;

        switch (u.RoleName)
        {
            case "Cashier":
                AddSectionLabel("حسابي");
                AddSideBtn("🔑", "تغيير كلمة المرور", "ChangePassword");
                AddSectionLabel("العمل اليومي");
                AddSideBtn("🛒", "فتح نقطة الكاشير", "Pos");
                AddSideBtn("🧾", "سجل المبيعات", "Sales");
                AddSideBtn("👥", "العملاء", "Customers");
                break;

            case "Kitchen":
                AddSectionLabel("حسابي");
                AddSideBtn("🔑", "تغيير كلمة المرور", "ChangePassword");
                AddSectionLabel("العمل اليومي");
                AddSideBtn("👨‍🍳", "فتح شاشة المطبخ", "Kitchen");
                break;

            case "Waiter":
                AddSectionLabel("حسابي");
                AddSideBtn("🔑", "تغيير كلمة المرور", "ChangePassword");
                AddSectionLabel("العمل اليومي");
                AddSideBtn("🛒", "نقطة البيع", "Pos");
                AddSideBtn("📅", "الحجوزات", "Reservations");
                AddSideBtn("👥", "العملاء", "Customers");
                break;

            case "Manager":
                AddSectionLabel("الرئيسية");
                AddSideBtn("🏠", "لوحة التحكم", "Dashboard");
                AddSectionLabel("العمليات");
                AddSideBtn("🛒", "نقطة البيع", "Pos");
                AddSideBtn("👨‍🍳", "المطبخ", "Kitchen");
                AddSideBtn("📋", "القائمة والأصناف", "Menu");
                AddSideBtn("📦", "المخزون", "Inventory");
                AddSectionLabel("المالية");
                AddSideBtn("🧾", "المبيعات", "Sales");
                AddSideBtn("📅", "الحجوزات", "Reservations");
                AddSideBtn("📈", "التقارير", "Reports");
                AddSectionLabel("الشركاء");
                AddSideBtn("👥", "العملاء", "Customers");
                AddSideBtn("🏭", "الموردون", "Suppliers");
                if (u.CanAccess("UserMgmt"))
                {
                    AddSectionLabel("إدارة الموظفين");
                    AddSideBtn("👤", "موظفو فريقي", "Admin");
                }
                AddSectionLabel("حسابي");
                AddSideBtn("🔑", "تغيير كلمة المرور", "ChangePassword");
                break;

            default: // Owner, Admin
                AddSectionLabel("الرئيسية");
                AddSideBtn("🏠", "لوحة التحكم", "Dashboard");
                AddSectionLabel("العمليات");
                AddSideBtn("🛒", "نقطة البيع", "Pos");
                AddSideBtn("👨‍🍳", "المطبخ", "Kitchen");
                AddSideBtn("📋", "القائمة والأصناف", "Menu");
                AddSideBtn("📦", "المخزون", "Inventory");
                AddSectionLabel("المالية");
                AddSideBtn("🧾", "المبيعات", "Sales");
                AddSideBtn("📅", "الحجوزات", "Reservations");
                AddSideBtn("📈", "التقارير", "Reports");
                AddSectionLabel("الشركاء");
                AddSideBtn("👥", "العملاء", "Customers");
                AddSideBtn("🏭", "الموردون", "Suppliers");
                if (u.CanAccess("Admin"))
                {
                    AddSectionLabel("النظام");
                    AddSideBtn("⚙️", "الإدارة والإعدادات", "Admin");
                }
                AddSectionLabel("حسابي");
                AddSideBtn("🔑", "تغيير كلمة المرور", "ChangePassword");
                break;
        }
    }

    private void AddSectionLabel(string text)
    {
        LeftSidePanel.Children.Add(new TextBlock
        {
            Text       = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
            FontSize   = 9,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(14, 10, 14, 3)
        });
    }

    private void AddSideBtn(string icon, string label, string tag)
    {
        var u = App.CurrentUser;
        // فلترة بناءً على الصلاحية (ليست ChangePassword)
        if (tag != "ChangePassword" && u != null && !u.CanAccess(tag)) return;

        var btn = new Button
        {
            Tag   = tag,
            Style = (Style)Resources["SideBtn"]
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock { Text = icon, FontSize = 14, Width = 24 });
        sp.Children.Add(new TextBlock { Text = label, FontSize = 12 });
        btn.Content = sp;
        btn.Click  += SideBtn_Click;
        LeftSidePanel.Children.Add(btn);
    }

    private void SideBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var tag = btn.Tag?.ToString() ?? "";

        if (tag == "ChangePassword")
        {
            var dlg = new ChangePasswordDialog { Owner = this };
            dlg.ShowDialog();
            return;
        }

        Navigate(tag);
    }

    // ===================================================================
    //  بناء شجرة التنقل الأيمن
    // ===================================================================
    private void BuildNavTree()
    {
        NavTree.Items.Clear();
        var u = App.CurrentUser;
        if (u == null) return;

        var groups = new[]
        {
            new NavGroup("🏠 الرئيسية", new[]
            {
                new NavItem("📊 لوحة التحكم",    "Dashboard"),
            }),
            new NavGroup("🛒 الكاشير والمبيعات", new[]
            {
                new NavItem("🛒 نقطة البيع",       "Pos"),
                new NavItem("🧾 المبيعات والفواتير","Sales"),
            }),
            new NavGroup("🍳 التشغيل", new[]
            {
                new NavItem("👨‍🍳 شاشة المطبخ",     "Kitchen"),
                new NavItem("📋 إدارة القائمة",     "Menu"),
                new NavItem("📦 إدارة المخزون",     "Inventory"),
            }),
            new NavGroup("💰 المالية والتخطيط", new[]
            {
                new NavItem("📅 الحجوزات",          "Reservations"),
                new NavItem("📈 التقارير",           "Reports"),
            }),
            new NavGroup("👥 العملاء والشركاء", new[]
            {
                new NavItem("👥 إدارة العملاء",     "Customers"),
                new NavItem("🏭 الموردون",           "Suppliers"),
            }),
            new NavGroup("⚙️ النظام", new[]
            {
                new NavItem("⚙️ الإدارة والإعدادات","Admin"),
            }),
        };

        foreach (var group in groups)
        {
            var catItem = new TreeViewItem
            {
                Header     = group.Header,
                Style      = (Style)Resources["TreeCatItem"],
                IsExpanded = true,
            };

            bool hasChild = false;
            foreach (var leaf in group.Items)
            {
                if (!u.CanAccess(leaf.Tag)) continue;
                var leafItem = new TreeViewItem
                {
                    Header = leaf.Label,
                    Tag    = leaf.Tag,
                    Style  = (Style)Resources["TreeLeafItem"],
                };
                catItem.Items.Add(leafItem);
                hasChild = true;
            }

            if (hasChild)
                NavTree.Items.Add(catItem);
        }
    }

    private void NavTree_SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (NavTree.SelectedItem is TreeViewItem item && item.Tag is string page)
            Navigate(page);
    }

    // ===================================================================
    //  التنقل بين الصفحات
    // ===================================================================
    public void Navigate(string page)
    {
        var u = App.CurrentUser;
        if (u == null) return;

        // التحقق من الصلاحية (الصفحة الرئيسية مفتوحة للجميع)
        if (page != "Home" && !u.CanAccess(page)) return;

        Page? content = page switch
        {
            "Home"         => new HomePage(),
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
        _activePage = page;

        var (title, crumb) = GetPageMeta(page);
        TxtPageTitle.Text      = title;
        TxtPageBreadcrumb.Text = crumb;

        UpdateSidebarActive(page);
    }

    private static (string Title, string Crumb) GetPageMeta(string page) => page switch
    {
        "Home"         => ("🏠  الصفحة الرئيسية",          "itQAN Soft  ›  الرئيسية"),
        "Dashboard"    => ("📊  لوحة التحكم",               "الرئيسية  ›  لوحة التحكم"),
        "Pos"          => ("🛒  نقطة البيع (الكاشير)",      "التشغيل  ›  نقطة البيع"),
        "Kitchen"      => ("👨‍🍳  شاشة المطبخ",              "التشغيل  ›  المطبخ"),
        "Menu"         => ("📋  إدارة القائمة والأصناف",    "التشغيل  ›  القائمة"),
        "Inventory"    => ("📦  إدارة المخزون",              "التشغيل  ›  المخزون"),
        "Customers"    => ("👥  إدارة العملاء",             "الشركاء  ›  العملاء"),
        "Suppliers"    => ("🏭  الموردون والمشتريات",       "الشركاء  ›  الموردون"),
        "Sales"        => ("🧾  المبيعات والفواتير",         "المالية  ›  المبيعات"),
        "Reservations" => ("📅  إدارة الحجوزات",            "المالية  ›  الحجوزات"),
        "Reports"      => ("📈  التقارير والإحصائيات",       "المالية  ›  التقارير"),
        "Admin"        => ("⚙️  إدارة النظام والإعدادات",   "النظام  ›  الإدارة"),
        _              => ("",                               "")
    };

    private void UpdateSidebarActive(string page)
    {
        foreach (var child in LeftSidePanel.Children.OfType<Button>())
        {
            child.Style = child.Tag?.ToString() == page
                ? (Style)Resources["SideBtnActive"]
                : (Style)Resources["SideBtn"];
        }
    }

    // ===================================================================
    //  تسجيل الخروج
    // ===================================================================
    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        _clock.Stop();
        App.CurrentUser = null;
        new LoginWindow().Show();
        Close();
    }

    // ===================================================================
    //  أزرار النافذة
    // ===================================================================
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "هل تريد إغلاق النظام؟",
            "تأكيد الإغلاق",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _clock.Stop();
            Application.Current.Shutdown();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
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
                BlurRadius  = 30,
                ShadowDepth = 8,
                Opacity     = 0.2,
                Color       = Colors.Black
            };
            BtnMaximizeToggle.Content = "□";
            BtnMaximizeToggle.ToolTip = "تكبير";
        }
    }

    // سحب النافذة
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                var mp = e.GetPosition(this);
                Left = mp.X - (Width / 2);
                Top  = 0;
            }
            try { DragMove(); } catch { }
        }
    }

    // ===================================================================
    //  أنواع مساعدة للتنقل
    // ===================================================================
    private record NavGroup(string Header, NavItem[] Items);
    private record NavItem(string Label, string Tag);
}
