using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Suppliers;

public partial class SupplierOrdersDialog : Window
{
    private readonly DbHelper _db;
    private readonly int _supplierId;

    public SupplierOrdersDialog(DbHelper db, int supplierId)
    {
        InitializeComponent();
        _db        = db;
        _supplierId = supplierId;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var supplier = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT company_name FROM suppliers WHERE supplier_id=@id", new { id = _supplierId });
        TxtTitle.Text = $"طلبات المورد: {supplier?.company_name}";

        var rows = await _db.QueryAsync<dynamic>(
            @"SELECT po_id, total_amount, status, created_at, expected_delivery_date
              FROM purchase_orders WHERE supplier_id=@id ORDER BY created_at DESC",
            new { id = _supplierId });

        GridOrders.ItemsSource = rows.Select(r => new
        {
            r.po_id,
            total        = $"{r.total_amount:N2}",
            status       = (string)r.status,
            created_fmt  = ((DateTime)r.created_at).ToString("dd/MM/yyyy"),
            delivery_fmt = r.expected_delivery_date != null ? ((DateTime)r.expected_delivery_date).ToString("dd/MM/yyyy") : "—"
        }).ToList();
    }

    private async void BtnReceive_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        if (MessageBox.Show("تأكيد استلام الطلب؟ سيتم تحديث المخزون تلقائياً.", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        await _db.ExecuteAsync(
            "UPDATE purchase_orders SET status='Received', received_at=GETDATE() WHERE po_id=@id",
            new { id });

        var items = await _db.QueryAsync<dynamic>(
            "SELECT ingredient_id, quantity_ordered FROM purchase_order_items WHERE po_id=@id",
            new { id });

        foreach (var item in items)
        {
            await _db.ExecuteAsync(
                "UPDATE ingredients SET current_stock=current_stock+@qty,updated_at=GETDATE() WHERE ingredient_id=@iid",
                new { qty = (decimal)item.quantity_ordered, iid = (int)item.ingredient_id });
            await _db.ExecuteAsync(
                "INSERT INTO inventory_movements (ingredient_id,movement_type,quantity,notes,performed_by,branch_id) VALUES (@iid,'IN',@qty,'استلام من مورد - طلب #'+CAST(@id AS NVARCHAR),@uid,@bid)",
                new { iid = (int)item.ingredient_id, qty = (decimal)item.quantity_ordered, id, uid = App.CurrentUser!.UserId, bid = App.CurrentUser!.BranchId });
        }

        MessageBox.Show("✅ تم تأكيد الاستلام وتحديث المخزون");
        await LoadAsync();
    }
}
