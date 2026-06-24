using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Suppliers;

public partial class PurchaseOrderDialog : Window
{
    private readonly DbHelper _db;

    public PurchaseOrderDialog(DbHelper db)
    {
        InitializeComponent();
        _db = db;
        DpDelivery.SelectedDate = DateTime.Today.AddDays(3);
        Loaded += async (_, _) =>
        {
            var suppliers = await _db.QueryAsync<dynamic>(
                "SELECT supplier_id, company_name FROM suppliers WHERE is_active=1 ORDER BY company_name");
            CmbSupplier.ItemsSource = suppliers.ToList();
            CmbSupplier.SelectedIndex = 0;

            var ingredients = await _db.QueryAsync<dynamic>(
                "SELECT ingredient_id, ingredient_name AS name, unit FROM ingredients WHERE is_active=1 ORDER BY ingredient_name");
            CmbIngredient.ItemsSource = ingredients.Select(i => new
            {
                i.ingredient_id, Display = $"{i.name} ({i.unit})"
            }).ToList();
            CmbIngredient.SelectedIndex = 0;
        };
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (CmbSupplier.SelectedValue is not int suppId || CmbIngredient.SelectedValue is not int ingId)
        { MessageBox.Show("اختر المورد والمادة"); return; }

        var qty   = decimal.TryParse(TxtQty.Text,   out var q) ? q : 0;
        var price = decimal.TryParse(TxtPrice.Text, out var p) ? p : 0;

        var poId = await _db.ExecuteScalarAsync<int>(
            @"INSERT INTO purchase_orders (supplier_id,ordered_by,total_amount,expected_delivery_date,notes,status)
              OUTPUT INSERTED.po_id
              VALUES (@sid,@uid,@total,@del,@notes,'Pending')",
            new { sid = suppId, uid = App.CurrentUser!.UserId, total = qty * price,
                  del = DpDelivery.SelectedDate, notes = TxtNotes.Text });

        await _db.ExecuteAsync(
            "INSERT INTO purchase_order_items (po_id,ingredient_id,quantity_ordered,unit_price,subtotal) VALUES (@po,@ing,@q,@p,@sub)",
            new { po = poId, ing = ingId, q = qty, p = price, sub = qty * price });

        MessageBox.Show($"✅ تم إنشاء طلب الشراء رقم #{poId}", "تم");
        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
