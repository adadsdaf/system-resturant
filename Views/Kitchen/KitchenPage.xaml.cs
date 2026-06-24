using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RestaurantMS.Desktop.Views.Kitchen;

public partial class KitchenPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);

    public KitchenPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var orders = (await _db.QueryAsync<dynamic>(
            @"SELECT ko.kitchen_order_id, ko.order_id, ko.table_number,
                     ko.customer_name, ko.status, ko.notes, ko.created_at
              FROM kitchen_orders ko
              WHERE ko.status NOT IN ('Served','Cancelled')
              ORDER BY ko.created_at ASC")).ToList();

        TxtPending.Text = orders.Count.ToString();
        OrdersPanel.Children.Clear();

        foreach (var order in orders)
        {
            var items = await _db.QueryAsync<dynamic>(
                "SELECT item_name, quantity FROM kitchen_order_items WHERE kitchen_order_id=@id",
                new { id = (int)order.kitchen_order_id });

            OrdersPanel.Children.Add(MakeOrderCard(order, items));
        }
    }

    private Border MakeOrderCard(dynamic order, IEnumerable<dynamic> items)
    {
        var status = (string)order.status;
        var statusColor = status switch
        {
            "Pending"   => "#f59e0b",
            "Preparing" => "#3b82f6",
            "Ready"     => "#22c55e",
            _           => "#64748b"
        };
        var statusText = status switch
        {
            "Pending"   => "⏳ انتظار",
            "Preparing" => "🔥 جاري التحضير",
            "Ready"     => "✅ جاهز",
            _           => status
        };

        var card = new Border
        {
            Width = 260, Margin = new Thickness(0, 0, 12, 12),
            Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(2),
            BorderBrush  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor))
        };

        var sp = new StackPanel { Margin = new Thickness(16) };

        // رأس البطاقة
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var orderInfo = new StackPanel();
        orderInfo.Children.Add(new TextBlock
        {
            Text = $"طلب #{order.order_id}",
            Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.Bold
        });
        if (!string.IsNullOrEmpty((string)order.table_number))
            orderInfo.Children.Add(new TextBlock
            {
                Text = $"طاولة: {order.table_number}",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                FontSize = 12
            });

        var statusBadge = new Border
        {
            Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor + "33")),
            CornerRadius = new CornerRadius(20), Padding = new Thickness(10, 4, 10, 4),
            VerticalAlignment = VerticalAlignment.Top
        };
        statusBadge.Child = new TextBlock
        {
            Text = statusText,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor)),
            FontSize = 11
        };

        Grid.SetColumn(statusBadge, 1);
        header.Children.Add(orderInfo);
        header.Children.Add(statusBadge);
        sp.Children.Add(header);

        sp.Children.Add(new Separator
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
            Margin = new Thickness(0, 10, 0, 10)
        });

        // عناصر الطلب
        foreach (var item in items)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = (string)item.item_name, Foreground = Brushes.White, FontSize = 13 });
            var qtyBlock = new TextBlock
            {
                Text = $"×{item.quantity}",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                FontSize = 13, FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(qtyBlock, 1);
            row.Children.Add(qtyBlock);
            sp.Children.Add(row);
        }

        // أزرار تغيير الحالة
        sp.Children.Add(new Separator
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
            Margin = new Thickness(0, 10, 0, 10)
        });

        var btns = new StackPanel { Orientation = Orientation.Horizontal };
        if (status == "Pending")
        {
            btns.Children.Add(MakeStatusButton("🔥 بدء التحضير", "#3b82f6", (int)order.kitchen_order_id, "Preparing"));
        }
        else if (status == "Preparing")
        {
            btns.Children.Add(MakeStatusButton("✅ جاهز", "#22c55e", (int)order.kitchen_order_id, "Ready"));
        }
        else if (status == "Ready")
        {
            btns.Children.Add(MakeStatusButton("🍽️ تم التقديم", "#64748b", (int)order.kitchen_order_id, "Served"));
        }

        sp.Children.Add(btns);

        var timeAgo = DateTime.Now - (DateTime)order.created_at;
        sp.Children.Add(new TextBlock
        {
            Text = $"⏱ منذ {(int)timeAgo.TotalMinutes} دقيقة",
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
            FontSize = 11, Margin = new Thickness(0, 6, 0, 0)
        });

        card.Child = sp;
        return card;
    }

    private Button MakeStatusButton(string text, string color, int id, string newStatus)
    {
        var btn = new Button
        {
            Content = text, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(12, 6, 12, 6),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand, FontSize = 12
        };
        btn.Click += async (_, _) =>
        {
            await _db.ExecuteAsync(
                "UPDATE kitchen_orders SET status=@s, updated_at=GETDATE() WHERE kitchen_order_id=@id",
                new { s = newStatus, id });
            await LoadAsync();
        };
        return btn;
    }

    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await LoadAsync();
}
