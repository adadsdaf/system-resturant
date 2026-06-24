using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Inventory;

public partial class StockMovementDialog : Window
{
    private readonly DbHelper _db;
    private readonly string   _type;

    public StockMovementDialog(DbHelper db, string type)
    {
        InitializeComponent();
        _db   = db;
        _type = type;
        TxtTitle.Text = type == "IN" ? "📥 وارد مخزون" : "📤 صادر مخزون";
        Loaded += async (_, _) =>
        {
            var items = await _db.QueryAsync<dynamic>(
                "SELECT ingredient_id, ingredient_name AS name, unit FROM ingredients WHERE is_active=1 ORDER BY ingredient_name");
            CmbItem.ItemsSource = items.Select(i => new
            {
                i.ingredient_id,
                Display = $"{i.name} ({i.unit})"
            }).ToList();
        };
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (CmbItem.SelectedValue is not int id) { MessageBox.Show("اختر مادة"); return; }
        if (!decimal.TryParse(TxtQty.Text, out var qty) || qty <= 0) { MessageBox.Show("أدخل كمية صحيحة"); return; }

        var userId  = App.CurrentUser!.UserId;
        var branchId = App.CurrentUser!.BranchId;
        var notes   = TxtNotes.Text;
        var sign    = _type == "IN" ? "+" : "-";

        await _db.ExecuteAsync(
            @"UPDATE ingredients SET current_stock = current_stock + @delta, updated_at=GETDATE()
              WHERE ingredient_id=@id",
            new { delta = _type == "IN" ? qty : -qty, id });

        await _db.ExecuteAsync(
            @"INSERT INTO inventory_movements (ingredient_id,movement_type,quantity,notes,performed_by,branch_id)
              VALUES (@id,@t,@q,@n,@uid,@bid)",
            new { id, t = _type, q = qty, n = notes, uid = userId, bid = branchId });

        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
