using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Menu;

public partial class ItemDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _item;

    public ItemDialog(DbHelper db, List<dynamic> categories, dynamic? item)
    {
        InitializeComponent();
        _db   = db;
        _item = item;
        CmbCategory.ItemsSource = categories;

        if (item != null)
        {
            TxtTitle.Text = "تعديل الصنف";
            TxtName.Text  = (string)item.item_name;
            TxtPrice.Text = ((decimal)item.price).ToString();
            TxtCost.Text  = ((decimal)item.cost_price).ToString();
            TxtDesc.Text  = (string)(item.description ?? "");
            CmbCategory.SelectedValue = item.category_id;
        }
        else
        {
            CmbCategory.SelectedIndex = 0;
        }
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text) || CmbCategory.SelectedValue == null)
        { MessageBox.Show("يرجى ملء الحقول المطلوبة"); return; }

        var catId = (int)CmbCategory.SelectedValue;
        var name  = TxtName.Text.Trim();
        var price = decimal.TryParse(TxtPrice.Text, out var p) ? p : 0;
        var cost  = decimal.TryParse(TxtCost.Text,  out var c) ? c : 0;
        var desc  = TxtDesc.Text;

        if (_item == null)
            await _db.ExecuteAsync(
                "INSERT INTO menu_items (category_id,item_name,description,price,cost_price) VALUES (@c,@n,@d,@p,@cp)",
                new { c = catId, n = name, d = desc, p = price, cp = cost });
        else
            await _db.ExecuteAsync(
                "UPDATE menu_items SET category_id=@c,item_name=@n,description=@d,price=@p,cost_price=@cp,updated_at=GETDATE() WHERE item_id=@id",
                new { c = catId, n = name, d = desc, p = price, cp = cost, id = (int)_item.item_id });

        DialogResult = true;
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
