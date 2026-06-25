using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Views;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Dashboard;

public partial class DashboardPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);

    public DashboardPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var today = DateTime.Today;

            // ===== معلومات الترخيص والفرع =====
            var restName    = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='restaurant_name'") ?? "مطعمي";
            var licHolder   = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='license_holder'") ?? restName;
            var itqanContact = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='itqan_contact'") ?? "7774b3b7";
            var financialYear = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='financial_year'") ?? DateTime.Now.Year.ToString();
            var version     = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='system_version'") ?? "1.0.0";

            // بيانات الفرع الأول
            var branch = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 arabic_name, address, phone FROM branches WHERE is_active=1 ORDER BY branch_id");

            TxtLicenseMsg.Text    = $"هذا النظام مرخص لـ";
            TxtRestaurantName.Text = string.IsNullOrEmpty(licHolder) ? restName : licHolder;
            TxtBranchAddress.Text  = (string?)(branch?.address) ?? "—";
            TxtBranchPhone.Text    = (string?)(branch?.phone)   ?? "—";
            TxtItqanContact.Text   = itqanContact;
            TxtFinancialYear.Text  = financialYear;
            TxtVersion.Text        = $"V{version}";

            // ===== التحية واليوم =====
            var greet = DateTime.Now.Hour switch
            {
                < 12 => "صباح الخير ☀️",
                < 17 => "مساء الخير 🌤️",
                _     => "مساء النور 🌙"
            };
            TxtGreeting.Text = $"{greet}، {App.CurrentUser?.FullName}";
            TxtDate.Text     = DateTime.Now.ToString("dddd، dd MMMM yyyy — HH:mm",
                               new CultureInfo("ar-SA"));

            // ===== مبيعات اليوم =====
            var currency = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";
            var currSymbol = await _db.ExecuteScalarAsync<string>(
                @"SELECT c.currency_symbol FROM currencies c
                  JOIN settings s ON s.value=c.currency_code
                  WHERE s.setting_key='default_currency'") ?? currency;

            var salesToday = await _db.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(total_amount),0) FROM orders
                  WHERE CAST(created_at AS DATE)=@today AND order_status='Completed'",
                new { today });
            var ordersToday = await _db.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM orders
                  WHERE CAST(created_at AS DATE)=@today AND order_status='Completed'",
                new { today });
            var pendingKitchen = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM kitchen_orders WHERE status NOT IN ('Served','Cancelled')");
            var lowStock = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ingredients WHERE current_stock<=min_stock AND is_active=1");
            var activeItems = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM menu_items WHERE is_available=1");
            var todayRes = await _db.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM reservations
                  WHERE reservation_date=@today AND status NOT IN ('Cancelled','No Show')",
                new { today });
            var customers   = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM customers WHERE is_active=1");
            var ingredients = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ingredients WHERE is_active=1");

            TxtSalesToday.Text     = $"{salesToday:N2} {currSymbol}";
            TxtOrdersToday.Text    = $"{ordersToday} طلب مكتمل";
            TxtPendingKitchen.Text = pendingKitchen.ToString();
            TxtLowStock.Text       = lowStock.ToString();
            TxtActiveItems.Text    = activeItems.ToString();
            TxtReservations.Text   = todayRes.ToString();
            TxtCustomers.Text      = customers.ToString();
            TxtIngredients.Text    = ingredients.ToString();

            // ===== آخر الطلبات =====
            var orders = await _db.QueryAsync<dynamic>(
                @"SELECT TOP 20 o.order_id, o.customer_name, o.total_amount,
                         o.payment_method, o.created_at,
                         ISNULL(u.full_name,'—') AS served_by
                  FROM orders o
                  LEFT JOIN users u ON o.served_by = u.user_id
                  ORDER BY o.created_at DESC");

            var list = orders.Select(o => new
            {
                order_id       = o.order_id,
                customer_name  = o.customer_name,
                total_fmt      = $"{o.total_amount:N2} {currSymbol}",
                payment_method = PaymentLabel((string)o.payment_method),
                served_by      = o.served_by,
                time_fmt       = ((DateTime)o.created_at).ToString("dd/MM HH:mm")
            }).ToList();

            GridOrders.ItemsSource = list;
            TxtOrderCount.Text     = list.Count.ToString();

            // ===== العملات =====
            await LoadCurrenciesAsync();
        }
        catch { }
    }

    private async Task LoadCurrenciesAsync()
    {
        try
        {
            var tableExists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='currencies'");
            if (tableExists == 0) return;

            // العملة المحلية
            var local = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT currency_name, currency_symbol FROM currencies WHERE is_local=1");
            if (local != null)
                TxtLocalCurrency.Text = $"العملة الأساسية: {local.currency_name} ({local.currency_symbol})";

            var currencies = await _db.QueryAsync<dynamic>(
                @"SELECT currency_name AS name, currency_code AS code,
                         currency_symbol AS symbol, exchange_rate AS rate, is_local
                  FROM currencies WHERE is_active=1 ORDER BY is_local DESC, currency_name");

            CurrencyList.ItemsSource = currencies.Select(c => new
            {
                name     = (string)c.name,
                code     = (string)c.code,
                rate_fmt = (bool)c.is_local
                    ? $"{c.symbol} محلية"
                    : $"1 {c.code} = {(decimal)c.rate:N3} ر.س"
            }).ToList();
        }
        catch { }
    }

    private static string PaymentLabel(string m) => m switch
    {
        "Cash"     => "💵 كاش",
        "Card"     => "💳 بطاقة",
        "Transfer" => "🏦 تحويل",
        _          => m
    };

    private async void BtnRefresh_Click(object s, RoutedEventArgs e)
        => await LoadAsync();

    private void QuickNav_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string tag)
        {
            // الوصول للـ MainWindow من الصفحة
            var mainWin = Window.GetWindow(this) as MainWindow;
            mainWin?.Navigate(tag);
        }
    }
}
