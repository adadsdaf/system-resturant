using RestaurantMS.Desktop.Data;
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
        var today    = DateTime.Today;
        var currency = await _db.ExecuteScalarAsync<string>(
                           "SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";

        var greet = DateTime.Now.Hour switch
        {
            < 12 => "صباح الخير ☀️",
            < 17 => "مساء الخير 🌤️",
            _     => "مساء النور 🌙"
        };
        TxtGreeting.Text = $"{greet}، {App.CurrentUser?.FullName}";
        TxtDate.Text     = DateTime.Now.ToString("dddd, dd MMMM yyyy — HH:mm");

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

        var customers = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM customers WHERE is_active=1");

        var ingredients = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ingredients WHERE is_active=1");

        TxtSalesToday.Text     = $"{salesToday:N2} {currency}";
        TxtOrdersToday.Text    = $"{ordersToday} طلب مكتمل";
        TxtPendingKitchen.Text = pendingKitchen.ToString();
        TxtLowStock.Text       = lowStock.ToString();
        TxtActiveItems.Text    = activeItems.ToString();
        TxtReservations.Text   = todayRes.ToString();
        TxtCustomers.Text      = customers.ToString();
        TxtIngredients.Text    = ingredients.ToString();

        var orders = await _db.QueryAsync<dynamic>(
            @"SELECT TOP 20 o.order_id, o.customer_name, o.total_amount,
                     o.payment_method, o.created_at, u.full_name AS served_by
              FROM orders o
              JOIN users u ON o.served_by = u.user_id
              ORDER BY o.created_at DESC");

        var list = orders.Select(o => new
        {
            order_id      = o.order_id,
            customer_name = o.customer_name,
            total_fmt     = $"{o.total_amount:N2} {currency}",
            payment_method = PaymentLabel((string)o.payment_method),
            served_by     = o.served_by,
            time_fmt      = ((DateTime)o.created_at).ToString("dd/MM HH:mm")
        }).ToList();

        GridOrders.ItemsSource = list;
        TxtOrderCount.Text     = list.Count.ToString();
    }

    private static string PaymentLabel(string m) => m switch
    {
        "Cash"     => "💵 كاش",
        "Card"     => "💳 بطاقة",
        "Transfer" => "🏦 تحويل",
        _          => m
    };

    private async void BtnRefresh_Click(object s, System.Windows.RoutedEventArgs e)
        => await LoadAsync();
}
