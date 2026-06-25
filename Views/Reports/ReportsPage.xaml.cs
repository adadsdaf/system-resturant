using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Reports;

public partial class ReportsPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private string _activeTab = "Sales";
    private string _currency = "ريال";

    public ReportsPage()
    {
        InitializeComponent();
        DpFrom.SelectedDate = DateTime.Today.AddDays(-30);
        DpTo.SelectedDate   = DateTime.Today;
        Loaded += async (_, _) =>
        {
            _currency = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";
            await LoadCurrentTab();
        };
    }

    private void TabBtn_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not string tag) return;
        _activeTab = tag;
        _ = LoadCurrentTab();
    }

    private async void BtnLoad_Click(object s, RoutedEventArgs e) => await LoadCurrentTab();

    private async Task LoadCurrentTab()
    {
        try
        {
            var from = DpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
            var to   = DpTo.SelectedDate   ?? DateTime.Today;

            HideAll();
            switch (_activeTab)
            {
                case "Sales":     await LoadSales(from, to);     GridSales.Visibility    = Visibility.Visible; break;
                case "TopItems":  await LoadTopItems(from, to);  GridTopItems.Visibility = Visibility.Visible; break;
                case "Payment":   await LoadPayment(from, to);   GridPayment.Visibility  = Visibility.Visible; break;
                case "Staff":     await LoadStaff(from, to);     GridStaff.Visibility    = Visibility.Visible; break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل التقارير:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void HideAll()
    {
        GridSales.Visibility    = Visibility.Collapsed;
        GridTopItems.Visibility = Visibility.Collapsed;
        GridPayment.Visibility  = Visibility.Collapsed;
        GridStaff.Visibility    = Visibility.Collapsed;
    }

    private async Task LoadSales(DateTime from, DateTime to)
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT CAST(created_at AS DATE) AS sale_date,
                     COUNT(*) AS orders_count,
                     SUM(total_amount) AS total_sales,
                     SUM(discount_amount) AS total_discounts,
                     SUM(tax_amount) AS total_taxes,
                     SUM(total_amount - tax_amount) AS net_sales
              FROM orders
              WHERE CAST(created_at AS DATE) BETWEEN @from AND @to
                AND order_status='Completed'
              GROUP BY CAST(created_at AS DATE)
              ORDER BY sale_date DESC",
            new { from, to });

        GridSales.ItemsSource = rows.Select(r => new
        {
            sale_date    = ((DateTime)r.sale_date).ToString("dd/MM/yyyy"),
            orders_count = (int)r.orders_count,
            total        = $"{r.total_sales:N2} {_currency}",
            discounts    = $"{r.total_discounts:N2}",
            taxes        = $"{r.total_taxes:N2}",
            net          = $"{r.net_sales:N2} {_currency}"
        }).ToList();
    }

    private async Task LoadTopItems(DateTime from, DateTime to)
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT TOP 20 oi.item_name, mc.category_name,
                     SUM(oi.quantity) AS qty_sold,
                     SUM(oi.subtotal) AS revenue,
                     ROW_NUMBER() OVER (ORDER BY SUM(oi.quantity) DESC) AS rank
              FROM order_items oi
              JOIN orders o ON oi.order_id = o.order_id
              JOIN menu_items mi ON oi.menu_item_id = mi.item_id
              JOIN menu_categories mc ON mi.category_id = mc.category_id
              WHERE CAST(o.created_at AS DATE) BETWEEN @from AND @to
                AND o.order_status='Completed'
              GROUP BY oi.item_name, mc.category_name
              ORDER BY qty_sold DESC",
            new { from, to });

        GridTopItems.ItemsSource = rows.Select(r => new
        {
            rank      = (long)r.rank,
            item_name = (string)r.item_name,
            category  = (string)r.category_name,
            qty_sold  = (int)r.qty_sold,
            revenue   = $"{r.revenue:N2} {_currency}"
        }).ToList();
    }

    private async Task LoadPayment(DateTime from, DateTime to)
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT payment_method AS method, COUNT(*) AS cnt, SUM(total_amount) AS total
              FROM orders
              WHERE CAST(created_at AS DATE) BETWEEN @from AND @to AND order_status='Completed'
              GROUP BY payment_method",
            new { from, to });

        var list  = rows.ToList();
        var grand = list.Sum(r => (decimal)r.total);
        GridPayment.ItemsSource = list.Select(r => new
        {
            method = (string)r.method,
            count  = (int)r.cnt,
            total  = $"{r.total:N2} {_currency}",
            pct    = grand > 0 ? $"{(decimal)r.total / grand * 100:N1}%" : "0%"
        }).ToList();
    }

    private async Task LoadStaff(DateTime from, DateTime to)
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT u.full_name AS staff_name, COUNT(*) AS orders_count,
                     SUM(o.total_amount) AS total_sales
              FROM orders o JOIN users u ON o.served_by = u.user_id
              WHERE CAST(o.created_at AS DATE) BETWEEN @from AND @to
                AND o.order_status='Completed'
              GROUP BY u.full_name
              ORDER BY total_sales DESC",
            new { from, to });

        GridStaff.ItemsSource = rows.Select(r => new
        {
            staff_name = (string)r.staff_name,
            orders     = (int)r.orders_count,
            total      = $"{r.total_sales:N2} {_currency}",
            avg        = (int)r.orders_count > 0 ? $"{(decimal)r.total_sales / (int)r.orders_count:N2}" : "0.00"
        }).ToList();
    }
}
