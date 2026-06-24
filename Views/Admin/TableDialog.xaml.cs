using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class TableDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _table;

    public TableDialog(DbHelper db, dynamic? table)
    {
        InitializeComponent();
        _db    = db;
        _table = table;
        if (table != null)
        {
            TxtTitle.Text    = "تعديل الطاولة";
            TxtNumber.Text   = ((int)table.table_number).ToString();
            TxtCapacity.Text = ((int)table.capacity).ToString();
            TxtLocation.Text = (string)(table.location ?? "");
        }
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNumber.Text)) { MessageBox.Show("أدخل رقم الطاولة"); return; }
        var num  = int.TryParse(TxtNumber.Text,   out var n) ? n : 0;
        var cap  = int.TryParse(TxtCapacity.Text, out var c) ? c : 4;
        var loc  = TxtLocation.Text.Trim();
        var bid  = App.CurrentUser!.BranchId;

        if (_table == null)
            await _db.ExecuteAsync(
                "INSERT INTO tables (branch_id,table_number,capacity,location) VALUES (@bid,@n,@c,@l)",
                new { bid, n = num, c = cap, l = loc });
        else
            await _db.ExecuteAsync(
                "UPDATE tables SET table_number=@n,capacity=@c,location=@l WHERE table_id=@id",
                new { n = num, c = cap, l = loc, id = (int)_table.table_id });

        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
