using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Menu;

public partial class MenuPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private List<dynamic> _categories = new();

    public MenuPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _categories = (await _db.QueryAsync<dynamic>(
                "SELECT category_id, category_name FROM menu_categories ORDER BY sort_order, category_name")).ToList();

            var items = await _db.QueryAsync<dynamic>(
                @"SELECT mi.item_id, mi.item_name, mc.category_name, mc.category_id,
                         mi.price, mi.cost_price, mi.description, mi.is_available
                  FROM menu_items mi
                  JOIN menu_categories mc ON mi.category_id = mc.category_id
                  ORDER BY mc.category_name, mi.item_name");

            var currency = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";

            GridItems.ItemsSource = items.Select(i => new
            {
                i.item_id, i.item_name, i.category_name, i.category_id,
                i.price, i.cost_price, i.description, i.is_available,
                price_fmt  = $"{i.price:N2} {currency}",
                cost_fmt   = $"{i.cost_price:N2} {currency}",
                status_txt = ((bool)i.is_available) ? "✅ متاح" : "⛔ موقوف"
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل قائمة الأصناف:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GridItems_SelectionChanged(object s, SelectionChangedEventArgs e) { }

    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await LoadAsync();

    private void BtnAddItem_Click(object s, RoutedEventArgs e)
    {
        var dlg = new ItemDialog(_db, _categories, null);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private void BtnEdit_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not { } item) return;
        var dlg = new ItemDialog(_db, _categories, item);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private async void BtnToggle_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        await _db.ExecuteAsync(
            "UPDATE menu_items SET is_available = CASE WHEN is_available=1 THEN 0 ELSE 1 END WHERE item_id=@id",
            new { id });
        await LoadAsync();
    }

    private async void BtnDelete_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        if (MessageBox.Show("هل تريد حذف هذا الصنف؟", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _db.ExecuteAsync("DELETE FROM menu_items WHERE item_id=@id", new { id });
        await LoadAsync();
    }

    private void BtnAddCategory_Click(object s, RoutedEventArgs e)
    {
        var dlg = new CategoryDialog();
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.CategoryName))
        {
            _ = _db.ExecuteAsync("INSERT INTO menu_categories (category_name) VALUES (@n)", new { n = dlg.CategoryName });
            _ = LoadAsync();
        }
    }
}
