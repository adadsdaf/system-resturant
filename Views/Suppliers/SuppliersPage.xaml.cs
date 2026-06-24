using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Suppliers;

public partial class SuppliersPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);

    public SuppliersPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT supplier_id, company_name, contact_name, phone, email, is_active
              FROM suppliers ORDER BY company_name");

        GridSuppliers.ItemsSource = rows.Select(r => new
        {
            r.supplier_id, r.company_name, r.contact_name,
            phone      = (string)(r.phone ?? ""),
            email      = (string)(r.email ?? ""),
            status_txt = ((bool)r.is_active) ? "✅ نشط" : "⛔ موقوف",
            r.is_active
        }).ToList();
    }

    private void BtnAdd_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SupplierDialog(_db, null);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private void BtnEdit_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not { } item) return;
        var dlg = new SupplierDialog(_db, item);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private void BtnOrder_Click(object s, RoutedEventArgs e)
    {
        new PurchaseOrderDialog(_db).ShowDialog();
        _ = LoadAsync();
    }

    private void BtnOrders_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        new SupplierOrdersDialog(_db, id).ShowDialog();
    }

    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await LoadAsync();
}
