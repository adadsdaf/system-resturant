using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Inventory;

public partial class StockHistoryDialog : Window
{
    public StockHistoryDialog(DbHelper db)
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            var rows = await db.QueryAsync<dynamic>(
                @"SELECT TOP 200 im.movement_type, im.quantity, im.notes, im.created_at,
                         i.ingredient_name AS ingredient, i.unit,
                         u.full_name AS user_name
                  FROM inventory_movements im
                  JOIN ingredients i ON im.ingredient_id = i.ingredient_id
                  JOIN users u       ON im.performed_by  = u.user_id
                  ORDER BY im.created_at DESC");

            GridHistory.ItemsSource = rows.Select(r => new
            {
                date_fmt   = ((DateTime)r.created_at).ToString("dd/MM/yyyy HH:mm"),
                ingredient = (string)r.ingredient,
                type_txt   = ((string)r.movement_type) == "IN" ? "📥 وارد" : "📤 صادر",
                qty_fmt    = $"{r.quantity} {r.unit}",
                r.user_name,
                notes      = (string)(r.notes ?? "")
            }).ToList();
        };
    }
}
