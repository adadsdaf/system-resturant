using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Sales;

public partial class SalesPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private string _currency = "ريال";

    public SalesPage()
    {
        InitializeComponent();
        DpFrom.SelectedDate = DateTime.Today.AddDays(-30);
        DpTo.SelectedDate   = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _currency = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";
        var from = DpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
        var to   = DpTo.SelectedDate   ?? DateTime.Today;

        var orders = await _db.QueryAsync<dynamic>(
            @"SELECT o.order_id, o.customer_name, o.total_amount, o.discount_amount,
                     o.tax_amount, o.payment_method, o.order_status, o.created_at,
                     u.full_name AS served_by
              FROM orders o
              JOIN users u ON o.served_by = u.user_id
              WHERE CAST(o.created_at AS DATE) BETWEEN @from AND @to
                AND o.order_status = 'Completed'
              ORDER BY o.created_at DESC",
            new { from, to });

        var list = orders.Select(o => new
        {
            o.order_id, o.customer_name, o.payment_method, o.served_by,
            total_fmt = $"{o.total_amount:N2} {_currency}",
            disc_fmt  = $"{o.discount_amount:N2}",
            tax_fmt   = $"{o.tax_amount:N2}",
            date_fmt  = ((DateTime)o.created_at).ToString("dd/MM/yyyy HH:mm")
        }).ToList();

        GridOrders.ItemsSource = list;

        TxtCount.Text = list.Count.ToString();
        var total = orders.Sum(o => (decimal)o.total_amount);
        var tax   = orders.Sum(o => (decimal)o.tax_amount);
        TxtTotal.Text = $"{total:N2} {_currency}";
        TxtTax.Text   = $"{tax:N2}";
        TxtAvg.Text   = list.Count > 0 ? $"{total / list.Count:N2}" : "0.00";
    }

    private async void Filter_Changed(object s, SelectionChangedEventArgs e) => await LoadAsync();
    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await LoadAsync();

    private async void BtnDetails_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        var dlg = new OrderDetailsDialog(_db, id, _currency);
        dlg.ShowDialog();
    }
}
