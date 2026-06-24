using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Pos;

public partial class CustomerSearchDialog : Window
{
    private readonly DbHelper _db;
    public dynamic? SelectedCustomer { get; private set; }

    public CustomerSearchDialog(DbHelper db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void TxtPhone_Changed(object s, TextChangedEventArgs e)
    {
        Ph.Visibility = string.IsNullOrEmpty(TxtPhone.Text) ? Visibility.Visible : Visibility.Collapsed;
        var q = TxtPhone.Text.Trim();
        if (q.Length < 2) { LstResults.ItemsSource = null; return; }

        var results = await _db.QueryAsync<dynamic>(
            @"SELECT TOP 10 c.customer_id, c.full_name, c.phone,
                     COALESCE(la.points_balance,0) AS points
              FROM customers c
              LEFT JOIN loyalty_accounts la ON c.customer_id = la.customer_id
              WHERE c.phone LIKE @q AND c.is_active=1",
            new { q = $"%{q}%" });

        LstResults.ItemsSource = results.Select(r => new
        {
            r.customer_id, r.full_name, r.phone, r.points,
            Display = $"{r.full_name} — {r.phone} (نقاط: {r.points})"
        }).ToList();
        LstResults.DisplayMemberPath = "Display";
    }

    private void LstResults_Changed(object s, SelectionChangedEventArgs e)
    {
        if (LstResults.SelectedItem is dynamic sel)
            SelectedCustomer = sel;
    }

    private void BtnSelect_Click(object s, RoutedEventArgs e)
    {
        if (SelectedCustomer == null) { MessageBox.Show("اختر عميلاً أولاً"); return; }
        DialogResult = true;
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
