using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Inventory;

public partial class InventoryPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);

    public InventoryPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _db.QueryAsync<dynamic>(
            @"SELECT ingredient_id, ingredient_name AS name, unit,
                     current_stock, min_stock, cost_per_unit, is_active,
                     CASE WHEN current_stock <= min_stock THEN 1 ELSE 0 END AS is_low
              FROM ingredients WHERE is_active=1
              ORDER BY is_low DESC, ingredient_name");

        var list = items.Select(i => new
        {
            i.ingredient_id, i.name, i.unit, i.current_stock, i.min_stock, i.cost_per_unit, i.is_low,
            stock_fmt  = $"{i.current_stock:N2} {i.unit}",
            min_fmt    = $"{i.min_stock:N2} {i.unit}",
            cost_fmt   = $"{i.cost_per_unit:N2}",
            status_txt = ((int)i.is_low == 1) ? "⚠️ منخفض" : "✅ كافٍ"
        }).ToList();

        GridInventory.ItemsSource = list;

        var lowCount = list.Count(x => (int)x.is_low == 1);
        if (lowCount > 0)
        {
            LowStockAlert.Visibility = Visibility.Visible;
            TxtLowStockMsg.Text = $"يوجد {lowCount} مادة تحت الحد الأدنى — يُنصح بإعادة الطلب";
        }
        else LowStockAlert.Visibility = Visibility.Collapsed;
    }

    private void BtnAdd_Click(object s, RoutedEventArgs e)
    {
        var dlg = new IngredientDialog(_db, null);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private void BtnEdit_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not { } item) return;
        var dlg = new IngredientDialog(_db, item);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private async void BtnDelete_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        if (MessageBox.Show("حذف هذه المادة؟", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _db.ExecuteAsync("UPDATE ingredients SET is_active=0 WHERE ingredient_id=@id", new { id });
        await LoadAsync();
    }

    private async void BtnIn_Click(object s, RoutedEventArgs e)
    {
        var dlg = new StockMovementDialog(_db, "IN");
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void BtnOut_Click(object s, RoutedEventArgs e)
    {
        var dlg = new StockMovementDialog(_db, "OUT");
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private void BtnHistory_Click(object s, RoutedEventArgs e)
    {
        new StockHistoryDialog(_db).ShowDialog();
    }

    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await LoadAsync();
}
