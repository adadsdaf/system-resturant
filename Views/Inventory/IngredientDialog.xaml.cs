using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Inventory;

public partial class IngredientDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _item;

    public IngredientDialog(DbHelper db, dynamic? item)
    {
        InitializeComponent();
        _db   = db;
        _item = item;
        if (item != null)
        {
            TxtTitle.Text  = "تعديل المادة";
            TxtName.Text   = (string)item.name;
            TxtUnit.Text   = (string)item.unit;
            TxtMin.Text    = ((decimal)item.min_stock).ToString();
            TxtStock.Text  = ((decimal)item.current_stock).ToString();
            TxtCost.Text   = ((decimal)item.cost_per_unit).ToString();
        }
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text)) { MessageBox.Show("أدخل اسم المادة"); return; }
        var name  = TxtName.Text.Trim();
        var unit  = TxtUnit.Text.Trim();
        var min   = decimal.TryParse(TxtMin.Text,   out var m) ? m : 0;
        var stock = decimal.TryParse(TxtStock.Text, out var st) ? st : 0;
        var cost  = decimal.TryParse(TxtCost.Text,  out var c) ? c : 0;

        if (_item == null)
            await _db.ExecuteAsync(
                "INSERT INTO ingredients (ingredient_name,unit,current_stock,min_stock,cost_per_unit) VALUES (@n,@u,@st,@m,@c)",
                new { n = name, u = unit, st = stock, m = min, c = cost });
        else
            await _db.ExecuteAsync(
                "UPDATE ingredients SET ingredient_name=@n,unit=@u,current_stock=@st,min_stock=@m,cost_per_unit=@c,updated_at=GETDATE() WHERE ingredient_id=@id",
                new { n = name, u = unit, st = stock, m = min, c = cost, id = (int)_item.ingredient_id });

        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
