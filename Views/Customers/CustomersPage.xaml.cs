using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Customers;

public partial class CustomersPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private List<dynamic> _allData = new();
    private string _currency = "ريال";

    public CustomersPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _currency = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";
        _allData = (await _db.QueryAsync<dynamic>(
            @"SELECT c.customer_id, c.full_name, c.phone, c.email, c.is_active,
                     COALESCE(la.points_balance,0) AS points,
                     COALESCE((SELECT SUM(total_amount) FROM orders o WHERE o.customer_id=c.customer_id),0) AS total_spent
              FROM customers c
              LEFT JOIN loyalty_accounts la ON c.customer_id = la.customer_id
              ORDER BY c.full_name")).ToList();
        Render(_allData);
    }

    private void Render(IEnumerable<dynamic> data)
    {
        GridCustomers.ItemsSource = data.Select(c => new
        {
            c.customer_id, c.full_name, c.phone,
            email      = (string)(c.email ?? ""),
            points     = (int)c.points,
            spent_fmt  = $"{c.total_spent:N2} {_currency}",
            status_txt = ((bool)c.is_active) ? "✅ نشط" : "⛔ موقوف",
            c.is_active
        }).ToList();
    }

    private void TxtSearch_Changed(object s, TextChangedEventArgs e)
    {
        SearchPh.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
        var q = TxtSearch.Text.Trim();
        Render(string.IsNullOrEmpty(q) ? _allData
            : _allData.Where(c => ((string)c.full_name).Contains(q) || ((string)c.phone).Contains(q)));
    }

    private void BtnAdd_Click(object s, RoutedEventArgs e)
    {
        var dlg = new CustomerDialog(_db, null);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private void BtnEdit_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not { } item) return;
        var dlg = new CustomerDialog(_db, item);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private async void BtnToggle_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        await _db.ExecuteAsync(
            "UPDATE customers SET is_active=CASE WHEN is_active=1 THEN 0 ELSE 1 END WHERE customer_id=@id",
            new { id });
        await LoadAsync();
    }

    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await LoadAsync();
}
