using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RestaurantMS.Desktop.Views.Pos;

public partial class PosPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private readonly List<CartItemModel> _cart = new();
    private List<dynamic> _allItems = new();
    private int? _customerId;
    private string _currency = "ريال";
    private double _taxRate;
    private bool _taxEnabled;

    public PosPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _currency   = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";
        var taxEn   = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='tax_enabled'") ?? "0";
        var taxR    = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='tax_rate'") ?? "0";
        _taxEnabled = taxEn == "1";
        _taxRate    = double.TryParse(taxR, out var r) ? r : 0;

        // تحميل التصنيفات
        var cats = await _db.QueryAsync<dynamic>(
            @"SELECT DISTINCT mc.category_id, mc.category_name
              FROM menu_categories mc
              JOIN menu_items mi ON mc.category_id = mi.category_id
              WHERE mi.is_available = 1
              ORDER BY mc.category_name");

        CatPanel.Children.Clear();
        var allBtn = MakeCatButton("الكل", null);
        CatPanel.Children.Add(allBtn);
        foreach (var c in cats)
            CatPanel.Children.Add(MakeCatButton((string)c.category_name, (int)c.category_id));

        // تحميل الأصناف
        _allItems = (await _db.QueryAsync<dynamic>(
            @"SELECT mi.item_id, mi.item_name, mc.category_name, mc.category_id,
                     mi.price, mi.description
              FROM menu_items mi
              JOIN menu_categories mc ON mi.category_id = mc.category_id
              WHERE mi.is_available = 1
              ORDER BY mc.category_name, mi.item_name")).ToList();
        RenderItems(_allItems);

        // تحميل الطاولات
        var tables = await _db.QueryAsync<dynamic>(
            "SELECT table_id, table_number FROM tables WHERE is_active=1 ORDER BY table_number");
        CmbTable.Items.Add("بدون طاولة");
        foreach (var t in tables)
            CmbTable.Items.Add($"طاولة {t.table_number}");
        CmbTable.SelectedIndex = 0;
    }

    private Button MakeCatButton(string name, int? catId)
    {
        var btn = new Button
        {
            Content = name,
            Tag     = catId,
            Margin  = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(14, 6, 14, 6),
            Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
            Foreground  = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
            Cursor      = Cursors.Hand,
            FontSize    = 13
        };
        btn.Template = CreateRoundedButtonTemplate();
        btn.Click   += (s, _) =>
        {
            var filtered = catId == null ? _allItems
                : _allItems.Where(i => (int)i.category_id == catId).ToList();
            RenderItems(filtered);
        };
        return btn;
    }

    private static ControlTemplate CreateRoundedButtonTemplate()
    {
        var tpl = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(cp);
        tpl.VisualTree = border;
        return tpl;
    }

    private void RenderItems(IEnumerable<dynamic> items)
    {
        ItemsPanel.Children.Clear();
        foreach (var item in items)
            ItemsPanel.Children.Add(MakeItemCard(item));
    }

    private Border MakeItemCard(dynamic item)
    {
        var card = new Border
        {
            Width  = 160, Height = 120,
            Margin = new Thickness(0, 0, 10, 10),
            Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
            CornerRadius = new CornerRadius(10),
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"))
        };

        var sp = new StackPanel { Margin = new Thickness(12), VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock
        {
            Text = (string)item.item_name, Foreground = Brushes.White,
            FontSize = 13, FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{item.price:N2} {_currency}",
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
            FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 6, 0, 0)
        });
        sp.Children.Add(new TextBlock
        {
            Text = (string)item.category_name,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
            FontSize = 11
        });
        card.Child = sp;

        card.MouseLeftButtonUp += (_, _) => AddToCart((int)item.item_id, (string)item.item_name, (decimal)item.price);
        return card;
    }

    private void AddToCart(int id, string name, decimal price)
    {
        var existing = _cart.FirstOrDefault(i => i.ItemId == id);
        if (existing != null) { existing.Quantity++; }
        else _cart.Add(new CartItemModel { ItemId = id, Name = name, Price = price, Quantity = 1 });
        RenderCart();
    }

    private void RenderCart()
    {
        CartPanel.Children.Clear();
        foreach (var item in _cart)
        {
            var row = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(10, 8, 10, 8)
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock { Text = item.Name, Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.SemiBold });
            left.Children.Add(new TextBlock { Text = $"{item.Price:N2} × {item.Quantity} = {item.Price * item.Quantity:N2}", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")), FontSize = 11 });

            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var minus = new Button { Content = "−", Width = 28, Height = 28, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, FontSize = 16 };
            var qty   = new TextBlock { Text = item.Quantity.ToString(), Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.Bold, Width = 30, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var plus  = new Button { Content = "+", Width = 28, Height = 28, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, FontSize = 16 };

            var capturedItem = item;
            minus.Click += (_, _) => { if (capturedItem.Quantity > 1) capturedItem.Quantity--; else _cart.Remove(capturedItem); RenderCart(); };
            plus.Click  += (_, _) => { capturedItem.Quantity++; RenderCart(); };

            right.Children.Add(minus);
            right.Children.Add(qty);
            right.Children.Add(plus);

            Grid.SetColumn(right, 1);
            g.Children.Add(left);
            g.Children.Add(right);
            row.Child = g;
            CartPanel.Children.Add(row);
        }
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        var subtotal    = _cart.Sum(i => i.Price * i.Quantity);
        var discountPct = double.TryParse(TxtDiscount.Text, out var d) ? d : 0;
        var discountAmt = Math.Round((double)subtotal * discountPct / 100, 2);
        var taxable     = (double)subtotal - discountAmt;
        var taxAmt      = _taxEnabled ? Math.Round(taxable * _taxRate / 100, 2) : 0;
        var total       = taxable + taxAmt;

        TxtSubtotal.Text    = $"{subtotal:N2}";
        TxtDiscountAmt.Text = $"-{discountAmt:N2}";
        TxtTotal.Text       = $"{total:N2} {_currency}";
    }

    private void TxtSearch_Changed(object s, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
        var q = TxtSearch.Text.Trim();
        RenderItems(string.IsNullOrEmpty(q) ? _allItems : _allItems.Where(i => ((string)i.item_name).Contains(q)));
    }

    private void TxtDiscount_Changed(object s, TextChangedEventArgs e) => UpdateTotals();
    private void Payment_Changed(object s, RoutedEventArgs e) { }

    private void BtnClearCart_Click(object s, RoutedEventArgs e)
    {
        _cart.Clear();
        _customerId = null;
        TxtCustomerInfo.Text = "زبون عادي";
        RenderCart();
    }

    private async void BtnSearchCustomer_Click(object s, RoutedEventArgs e)
    {
        var dlg = new CustomerSearchDialog(_db);
        if (dlg.ShowDialog() == true && dlg.SelectedCustomer != null)
        {
            _customerId = (int)dlg.SelectedCustomer.customer_id;
            TxtCustomerInfo.Text = $"{dlg.SelectedCustomer.full_name} ({dlg.SelectedCustomer.phone})";
        }
    }

    private async void BtnConfirmOrder_Click(object s, RoutedEventArgs e)
    {
        if (_cart.Count == 0) { MessageBox.Show("السلة فارغة!", "تنبيه"); return; }

        var payMethod = RbCash.IsChecked == true ? "Cash" : RbCard.IsChecked == true ? "Card" : "Transfer";
        var subtotal    = _cart.Sum(i => i.Price * i.Quantity);
        var discountPct = double.TryParse(TxtDiscount.Text, out var d) ? d : 0;
        var discountAmt = Math.Round((double)subtotal * discountPct / 100, 2);
        var taxable     = (double)subtotal - discountAmt;
        var taxAmt      = _taxEnabled ? Math.Round(taxable * _taxRate / 100, 2) : 0;
        var total       = Math.Round(taxable + taxAmt, 2);

        var userId   = App.CurrentUser!.UserId;
        var branchId = App.CurrentUser!.BranchId;
        var table    = CmbTable.SelectedItem?.ToString()?.Replace("طاولة ", "") ?? "";

        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            var orderId = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn,
                @"INSERT INTO orders (customer_name,customer_id,served_by,branch_id,subtotal,
                   discount_amount,tax_amount,total_amount,payment_method,payment_status,order_status)
                  OUTPUT INSERTED.order_id
                  VALUES (@cn,@cid,@uid,@bid,@sub,@disc,@tax,@total,@pm,'Paid','Completed')",
                new { cn = TxtCustomerInfo.Text, cid = _customerId, uid = userId, bid = branchId,
                      sub = subtotal, disc = (decimal)discountAmt, tax = (decimal)taxAmt,
                      total = (decimal)total, pm = payMethod }, tx);

            foreach (var item in _cart)
                await Dapper.SqlMapper.ExecuteAsync(conn,
                    "INSERT INTO order_items(order_id,menu_item_id,item_name,quantity,unit_price,subtotal) VALUES(@oid,@iid,@name,@qty,@price,@sub)",
                    new { oid = orderId, iid = item.ItemId, name = item.Name, qty = item.Quantity, price = item.Price, sub = item.Price * item.Quantity }, tx);

            await Dapper.SqlMapper.ExecuteAsync(conn,
                "INSERT INTO payments(order_id,amount_paid,payment_method) VALUES(@oid,@amt,@pm)",
                new { oid = orderId, amt = (decimal)total, pm = payMethod }, tx);

            var kid = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn,
                "INSERT INTO kitchen_orders(order_id,table_number,customer_name) OUTPUT INSERTED.kitchen_order_id VALUES(@oid,@tbl,@cn)",
                new { oid = orderId, tbl = table, cn = TxtCustomerInfo.Text }, tx);

            foreach (var item in _cart)
                await Dapper.SqlMapper.ExecuteAsync(conn,
                    "INSERT INTO kitchen_order_items(kitchen_order_id,item_name,quantity) VALUES(@kid,@name,@qty)",
                    new { kid, name = item.Name, qty = item.Quantity }, tx);

            tx.Commit();
            MessageBox.Show($"✅ تم حفظ الطلب رقم #{orderId}\nالإجمالي: {total:N2} {_currency}", "تم بنجاح");
            BtnClearCart_Click(s, e);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ");
        }
    }
}

public class CartItemModel
{
    public int     ItemId   { get; set; }
    public string  Name     { get; set; } = "";
    public decimal Price    { get; set; }
    public int     Quantity { get; set; }
}
