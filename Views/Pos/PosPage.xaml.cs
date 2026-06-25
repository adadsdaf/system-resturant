using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Services;
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
    private string _payMethod = "Cash";
    private int? _lastOrderId;
    private ReceiptSettings _receiptSettings = new();

    public PosPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            await LoadSettings();
            await LoadCategories();
            await LoadAllItems();
            await LoadTables();
            await LoadTodayStats();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}\n\nتأكد من اتصال قاعدة البيانات.", "خطأ التحميل");
        }
    }

    private async Task LoadSettings()
    {
        _currency   = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='currency'") ?? "ريال";
        var taxEn   = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='tax_enabled'") ?? "0";
        var taxR    = await _db.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE setting_key='tax_rate'") ?? "0";
        _taxEnabled = taxEn == "1";
        _taxRate    = double.TryParse(taxR, out var r) ? r : 0;

        TxtCurrencyLabel1.Text = $" {_currency}";
        TxtCurrencyLabel2.Text = $" {_currency}";
        TxtCurrencyLabel3.Text = $" {_currency}";
        TxtCurrencyLabel4.Text = $" {_currency}";
        TaxRow.Visibility      = _taxEnabled ? Visibility.Visible : Visibility.Collapsed;
        TxtTaxLabel.Text       = $"الضريبة ({_taxRate:0}%)";

        // تحميل إعدادات الفاتورة
        _receiptSettings = await LoadReceiptSettings();
    }

    private async Task<ReceiptSettings> LoadReceiptSettings()
    {
        var settings = new ReceiptSettings
        {
            Currency    = _currency,
            TaxEnabled  = _taxEnabled,
            TaxRate     = _taxRate
        };

        var rows = await _db.QueryAsync<dynamic>(
            "SELECT setting_key, value FROM settings WHERE setting_key LIKE 'receipt_%'");

        foreach (var row in rows)
        {
            string key = (string)row.setting_key;
            string val = (string)(row.value ?? "");
            switch (key)
            {
                case "receipt_restaurant_name":     settings.RestaurantName     = val; break;
                case "receipt_phone":               settings.Phone              = val; break;
                case "receipt_address":             settings.Address            = val; break;
                case "receipt_tax_number":          settings.TaxNumber          = val; break;
                case "receipt_footer":              settings.Footer             = val; break;
                case "receipt_show_logo":           settings.ShowLogo           = val == "1"; break;
                case "receipt_large_customer":      settings.PrintLargeCustomer = val == "1"; break;
                case "receipt_large_staff":         settings.PrintLargeStaff   = val == "1"; break;
                case "receipt_small_slips":         settings.PrintSmallSlips    = val == "1"; break;
                case "receipt_slip_by":             settings.SlipSplitBy        = val; break;
                case "receipt_printer_name":        settings.ThermalPrinterName = val; break;
                case "receipt_show_order_number":   settings.ShowOrderNumber    = val == "1"; break;
                case "receipt_show_datetime":       settings.ShowDateTime       = val == "1"; break;
                case "receipt_show_cashier":        settings.ShowCashierName    = val == "1"; break;
                case "receipt_show_table":          settings.ShowTableNumber    = val == "1"; break;
            }
        }

        // إذا لم توجد إعدادات الفاتورة في قاعدة البيانات، استخدم القيم الافتراضية
        if (string.IsNullOrEmpty(settings.RestaurantName))
        {
            settings.RestaurantName = await _db.ExecuteScalarAsync<string>(
                "SELECT value FROM settings WHERE setting_key='restaurant_name'") ?? "مطعم الإتقان";
        }

        return settings;
    }

    private async Task LoadCategories()
    {
        var cats = await _db.QueryAsync<dynamic>(
            @"SELECT DISTINCT mc.category_id, mc.category_name
              FROM menu_categories mc
              JOIN menu_items mi ON mc.category_id = mi.category_id
              WHERE mi.is_available = 1 AND mc.is_active = 1
              ORDER BY mc.sort_order, mc.category_name");

        CatPanel.Children.Clear();
        CatPanel.Children.Add(MakeCatButton("🍽 الكل", null, true));
        foreach (var c in cats)
            CatPanel.Children.Add(MakeCatButton((string)c.category_name, (int)c.category_id, false));
    }

    private async Task LoadAllItems()
    {
        _allItems = (await _db.QueryAsync<dynamic>(
            @"SELECT mi.item_id, mi.item_name, mc.category_name, mc.category_id,
                     mi.price, mi.description
              FROM menu_items mi
              JOIN menu_categories mc ON mi.category_id = mc.category_id
              WHERE mi.is_available = 1 AND mc.is_active = 1
              ORDER BY mc.sort_order, mc.category_name, mi.item_name")).ToList();
        RenderItems(_allItems);
    }

    private async Task LoadTables()
    {
        var tables = await _db.QueryAsync<dynamic>(
            "SELECT table_id, table_number FROM tables WHERE is_active=1 ORDER BY table_number");
        CmbTable.Items.Clear();
        CmbTable.Items.Add("بدون طاولة");
        foreach (var t in tables)
            CmbTable.Items.Add($"طاولة {t.table_number}");
        CmbTable.SelectedIndex = 0;
    }

    private async Task LoadTodayStats()
    {
        try
        {
            var todaySales = await _db.ExecuteScalarAsync<decimal?>(
                "SELECT ISNULL(SUM(total_amount),0) FROM orders WHERE CAST(created_at AS DATE)=CAST(GETDATE() AS DATE)");
            var todayCount = await _db.ExecuteScalarAsync<int?>(
                "SELECT ISNULL(COUNT(*),0) FROM orders WHERE CAST(created_at AS DATE)=CAST(GETDATE() AS DATE)");
            TxtTodaySales.Text  = $"مبيعات اليوم: {todaySales:N2} {_currency}";
            TxtTodayOrders.Text = $"طلبات اليوم: {todayCount}";
        }
        catch { /* ignore stats error */ }
    }

    // ===== صانع أزرار التصنيفات =====
    private Button MakeCatButton(string name, int? catId, bool active)
    {
        var btn = new Button
        {
            Content = name,
            Tag     = catId,
            Margin  = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(14, 8, 14, 8),
            Background  = active
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
            Foreground  = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor      = Cursors.Hand,
            FontSize    = 12,
            FontWeight  = active ? FontWeights.Bold : FontWeights.Normal
        };
        btn.Template = CreateRoundedButtonTemplate();
        btn.Click   += (_, _) =>
        {
            foreach (Button b in CatPanel.Children.OfType<Button>())
            {
                b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b"));
                b.FontWeight = FontWeights.Normal;
            }
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D"));
            btn.FontWeight = FontWeights.Bold;

            var filtered = catId == null ? _allItems
                : _allItems.Where(i => (int)i.category_id == catId).ToList();
            RenderItems(filtered);
        };
        return btn;
    }

    private static ControlTemplate CreateRoundedButtonTemplate()
    {
        var tpl    = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
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
            Width  = 155, Height = 115,
            Margin = new Thickness(0, 0, 10, 10),
            Background    = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
            CornerRadius  = new CornerRadius(10),
            Cursor        = Cursors.Hand,
            BorderThickness = new Thickness(1),
            BorderBrush   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"))
        };

        card.MouseEnter += (_, _) => card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#243447"));
        card.MouseLeave += (_, _) => card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b"));

        var sp = new StackPanel { Margin = new Thickness(12), VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock
        {
            Text = (string)item.item_name, Foreground = Brushes.White,
            FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{item.price:N2} {_currency}",
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
            FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 6, 0, 0)
        });
        sp.Children.Add(new TextBlock
        {
            Text = (string)item.category_name,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
            FontSize = 11
        });
        card.Child = sp;

        card.MouseLeftButtonUp += (_, _) =>
            AddToCart((int)item.item_id, (string)item.item_name,
                      (decimal)item.price, (int)item.category_id, (string)item.category_name);
        return card;
    }

    private void AddToCart(int id, string name, decimal price, int catId, string catName)
    {
        var existing = _cart.FirstOrDefault(i => i.ItemId == id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            _cart.Add(new CartItemModel
            {
                ItemId = id, Name = name, Price = price,
                Quantity = 1, CategoryId = catId, CategoryName = catName
            });
        }
        RenderCart();
    }

    private void RenderCart()
    {
        CartPanel.Children.Clear();
        if (_cart.Count == 0)
        {
            CartPanel.Children.Add(new TextBlock
            {
                Text = "السلة فارغة — اختر أصنافاً من القائمة",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                FontSize = 12, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 0)
            });
            UpdateTotals();
            return;
        }

        foreach (var item in _cart)
        {
            var capturedItem = item;

            var row = new Border
            {
                Background    = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f8fafc")),
                CornerRadius  = new CornerRadius(8),
                Margin        = new Thickness(0, 0, 0, 6),
                Padding       = new Thickness(10, 8, 10, 8),
                BorderThickness = new Thickness(1),
                BorderBrush   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0"))
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = item.Name,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                FontSize = 12, FontWeight = FontWeights.SemiBold
            });
            left.Children.Add(new TextBlock
            {
                Text = $"{item.Price:N2} × {item.Quantity} = {item.LineTotal:N2} {_currency}",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                FontSize = 11
            });

            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var minus = MakeQtyButton("−", "#ef4444");
            var qty   = new TextBlock
            {
                Text = item.Quantity.ToString(), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                FontSize = 14, FontWeight = FontWeights.Bold, Width = 30,
                TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            var plus  = MakeQtyButton("+", "#22c55e");

            minus.Click += (_, _) =>
            {
                if (capturedItem.Quantity > 1) capturedItem.Quantity--;
                else _cart.Remove(capturedItem);
                RenderCart();
            };
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

    private static Button MakeQtyButton(string content, string colorHex)
    {
        var btn = new Button
        {
            Content = content, Width = 28, Height = 28, FontSize = 16,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
        };
        var tpl    = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(cp);
        tpl.VisualTree = border;
        btn.Template   = tpl;
        return btn;
    }

    private void UpdateTotals()
    {
        var subtotal    = _cart.Sum(i => i.LineTotal);
        var discountVal = double.TryParse(TxtDiscountVal.Text, out var d) ? d : 0;
        var isPercent   = (CmbDiscountType.SelectedIndex == 0);
        var discountAmt = isPercent
            ? Math.Round((double)subtotal * discountVal / 100, 2)
            : Math.Min(discountVal, (double)subtotal);
        var taxable     = (double)subtotal - discountAmt;
        var taxAmt      = _taxEnabled ? Math.Round(taxable * _taxRate / 100, 2) : 0;
        var total       = taxable + taxAmt;

        TxtSubtotal.Text    = $"{subtotal:N2}";
        TxtDiscountAmt.Text = $"-{discountAmt:N2}";
        TxtTax.Text         = $"{taxAmt:N2}";
        TxtTotal.Text       = $"{total:N2}";

        UpdateChange();
    }

    private void UpdateChange()
    {
        if (decimal.TryParse(TxtTotal.Text, out var total) &&
            decimal.TryParse(TxtAmountPaid.Text, out var paid))
        {
            var change = paid - total;
            TxtChange.Text      = $"{(change >= 0 ? change : 0):N2}";
            TxtChange.Foreground = change >= 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16a34a"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
        }
    }

    // ===== طرق الدفع =====
    private void SetPaymentMethod(string method, Border active)
    {
        _payMethod = method;
        var inactive = new SolidColorBrush(Colors.White);
        var inactiveBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0"));

        BrdCash.Background     = inactive;
        BrdCard.Background     = inactive;
        BrdTransfer.Background = inactive;
        BrdCash.BorderBrush     = inactiveBorder;
        BrdCard.BorderBrush     = inactiveBorder;
        BrdTransfer.BorderBrush = inactiveBorder;

        foreach (TextBlock tb in GetChildren<TextBlock>(active))
            tb.Foreground = Brushes.White;

        active.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D"));
        active.BorderBrush = Brushes.Transparent;
    }

    private static IEnumerable<T> GetChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) yield return t;
            foreach (var desc in GetChildren<T>(child)) yield return desc;
        }
    }

    private void PayCash_Click(object s, MouseButtonEventArgs e)
    {
        SetPaymentMethod("Cash", BrdCash);
        // ضع المبلغ المستلم = الإجمالي
        TxtAmountPaid.Text = TxtTotal.Text;
    }
    private void PayCard_Click(object s, MouseButtonEventArgs e)     => SetPaymentMethod("Card", BrdCard);
    private void PayTransfer_Click(object s, MouseButtonEventArgs e) => SetPaymentMethod("Transfer", BrdTransfer);

    private void TxtSearch_Changed(object s, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
        var q = TxtSearch.Text.Trim();
        RenderItems(string.IsNullOrEmpty(q) ? _allItems
            : _allItems.Where(i => ((string)i.item_name).Contains(q, StringComparison.OrdinalIgnoreCase)));
    }

    private void TxtDiscount_Changed(object s, object e) => UpdateTotals();
    private void TxtAmountPaid_Changed(object s, TextChangedEventArgs e) => UpdateChange();

    private void BtnClearCart_Click(object s, RoutedEventArgs e)
    {
        _cart.Clear();
        _customerId = null;
        TxtCustomerInfo.Text = "زبون عادي";
        TxtDiscountVal.Text  = "0";
        TxtAmountPaid.Text   = "0";
        TxtNotes.Text        = "";
        CmbTable.SelectedIndex = 0;
        CmbOrderType.SelectedIndex = 0;
        SetPaymentMethod("Cash", BrdCash);
        RenderCart();
    }

    private async void BtnSearchCustomer_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var dlg = new CustomerSearchDialog(_db);
            if (dlg.ShowDialog() == true && dlg.SelectedCustomer != null)
            {
                _customerId = (int)dlg.SelectedCustomer.customer_id;
                TxtCustomerInfo.Text = $"{dlg.SelectedCustomer.full_name} ({dlg.SelectedCustomer.phone})";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في البحث عن العملاء: {ex.Message}", "خطأ");
        }
    }

    private async void BtnConfirmOrder_Click(object s, RoutedEventArgs e)
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("السلة فارغة!\nيرجى إضافة أصناف قبل إتمام الطلب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // حساب المجاميع
        var subtotal    = _cart.Sum(i => i.LineTotal);
        var discountVal = double.TryParse(TxtDiscountVal.Text, out var d) ? d : 0;
        var isPercent   = (CmbDiscountType.SelectedIndex == 0);
        var discountAmt = isPercent
            ? (decimal)Math.Round((double)subtotal * discountVal / 100, 2)
            : (decimal)Math.Min(discountVal, (double)subtotal);
        var taxable     = (double)(subtotal - discountAmt);
        var taxAmt      = (decimal)(_taxEnabled ? Math.Round(taxable * _taxRate / 100, 2) : 0);
        var total       = Math.Round(subtotal - discountAmt + taxAmt, 2);

        var userId   = App.CurrentUser!.UserId;
        var branchId = App.CurrentUser!.BranchId;
        var tableStr = CmbTable.SelectedItem?.ToString()?.Replace("طاولة ", "") ?? "";
        var orderType = (CmbOrderType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "داخل المطعم";
        var notes    = TxtNotes.Text.Trim();

        BtnConfirm.IsEnabled = false;
        BtnConfirm.Content   = "⏳  جاري الحفظ...";

        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
            // حفظ الطلب والحصول على ID بشكل صحيح
            var orderIds = (await Dapper.SqlMapper.QueryAsync<int>(conn,
                @"INSERT INTO orders
                    (customer_name, customer_id, served_by, branch_id,
                     subtotal, discount_amount, tax_amount, total_amount,
                     payment_method, payment_status, order_status)
                  OUTPUT INSERTED.order_id
                  VALUES (@cn, @cid, @uid, @bid, @sub, @disc, @tax, @total, @pm, 'Paid', 'Completed')",
                new
                {
                    cn    = TxtCustomerInfo.Text,
                    cid   = _customerId,
                    uid   = userId,
                    bid   = branchId,
                    sub   = subtotal,
                    disc  = discountAmt,
                    tax   = taxAmt,
                    total,
                    pm    = _payMethod
                }, tx)).ToList();

            if (!orderIds.Any()) throw new Exception("فشل إنشاء الطلب");
            var orderId = orderIds[0];

            // حفظ عناصر الطلب
            foreach (var item in _cart)
                await Dapper.SqlMapper.ExecuteAsync(conn,
                    @"INSERT INTO order_items
                        (order_id, menu_item_id, item_name, quantity, unit_price, subtotal)
                      VALUES (@oid, @iid, @name, @qty, @price, @sub)",
                    new { oid = orderId, iid = item.ItemId, name = item.Name,
                          qty = item.Quantity, price = item.Price, sub = item.LineTotal }, tx);

            // تسجيل الدفع
            await Dapper.SqlMapper.ExecuteAsync(conn,
                "INSERT INTO payments (order_id, amount_paid, payment_method) VALUES (@oid, @amt, @pm)",
                new { oid = orderId, amt = total, pm = _payMethod }, tx);

            // طلب المطبخ
            var kitchenIds = (await Dapper.SqlMapper.QueryAsync<int>(conn,
                @"INSERT INTO kitchen_orders (order_id, table_number, customer_name, notes)
                  OUTPUT INSERTED.kitchen_order_id
                  VALUES (@oid, @tbl, @cn, @notes)",
                new { oid = orderId, tbl = tableStr, cn = TxtCustomerInfo.Text, notes = $"{orderType} | {notes}".Trim('|', ' ') }, tx)).ToList();

            if (kitchenIds.Any())
            {
                var kid = kitchenIds[0];
                foreach (var item in _cart)
                    await Dapper.SqlMapper.ExecuteAsync(conn,
                        "INSERT INTO kitchen_order_items (kitchen_order_id, item_name, quantity) VALUES (@kid, @name, @qty)",
                        new { kid, name = item.Name, qty = item.Quantity }, tx);
            }

            tx.Commit();
            _lastOrderId = orderId;

            // إعادة تحميل إعدادات الفاتورة
            _receiptSettings = await LoadReceiptSettings();

            // طباعة الفواتير
            var orderDetail = await BuildOrderDetail(orderId);
            PrintOrder(orderDetail);

            // تحديث الإحصائيات
            await LoadTodayStats();

            // مسح السلة
            BtnClearCart_Click(s, e);

            MessageBox.Show($"✅ تم حفظ الطلب رقم #{orderId}\nالإجمالي: {total:N2} {_currency}",
                            "تم بنجاح", MessageBoxButton.OK, MessageBoxImage.None);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            MessageBox.Show($"❌ خطأ في حفظ الطلب:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnConfirm.IsEnabled = true;
            BtnConfirm.Content   = "✅  إتمام الطلب والطباعة";
        }
    }

    private void PrintOrder(OrderDetailModel order)
    {
        try
        {
            if (_receiptSettings.PrintLargeCustomer)
                PrintService.PrintLargeReceipt(order, _receiptSettings, "نسخة العميل");

            if (_receiptSettings.PrintLargeStaff)
                PrintService.PrintLargeReceipt(order, _receiptSettings, "نسخة المطعم");

            if (_receiptSettings.PrintSmallSlips)
                PrintSmallSlips(order);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تحذير: خطأ في الطباعة:\n{ex.Message}", "تحذير الطباعة");
        }
    }

    private void PrintSmallSlips(OrderDetailModel order)
    {
        // تجميع حسب التصنيف أو المجموعة
        var groups = order.Items
            .GroupBy(i => _receiptSettings.SlipSplitBy == "category"
                ? i.CategoryName
                : i.CategoryName) // يمكن تغييره لاحقاً
            .ToList();

        foreach (var group in groups)
        {
            if (!group.Any()) continue;
            PrintService.PrintSmallSlip(order, _receiptSettings, group.Key, group.ToList());
        }
    }

    private async Task<OrderDetailModel> BuildOrderDetail(int orderId)
    {
        var order = await _db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT o.order_id, o.customer_name, o.payment_method,
                     o.subtotal, o.discount_amount, o.tax_amount, o.total_amount,
                     o.order_status, o.created_at,
                     u.full_name AS served_by_name,
                     ISNULL(k.table_number, '') AS table_number
              FROM orders o
              LEFT JOIN users u ON o.served_by = u.user_id
              LEFT JOIN kitchen_orders k ON o.order_id = k.order_id
              WHERE o.order_id = @id", new { id = orderId });

        var items = (await _db.QueryAsync<dynamic>(
            @"SELECT oi.item_name, oi.quantity, oi.unit_price, oi.subtotal,
                     ISNULL(mc.category_name, N'عام') AS category_name
              FROM order_items oi
              LEFT JOIN menu_items mi ON oi.menu_item_id = mi.item_id
              LEFT JOIN menu_categories mc ON mi.category_id = mc.category_id
              WHERE oi.order_id = @id", new { id = orderId })).ToList();

        return new OrderDetailModel
        {
            OrderId       = orderId,
            CustomerName  = (string)(order?.customer_name ?? "زبون عادي"),
            PaymentMethod = (string)(order?.payment_method ?? "Cash"),
            Subtotal      = (decimal)(order?.subtotal ?? 0),
            DiscountAmount = (decimal)(order?.discount_amount ?? 0),
            TaxAmount     = (decimal)(order?.tax_amount ?? 0),
            TotalAmount   = (decimal)(order?.total_amount ?? 0),
            OrderStatus   = (string)(order?.order_status ?? ""),
            CreatedAt     = (DateTime)(order?.created_at ?? DateTime.Now),
            ServedBy      = (string)(order?.served_by_name ?? ""),
            TableNumber   = (string)(order?.table_number ?? ""),
            Items         = items.Select(i => new OrderItemDetailModel
            {
                ItemName     = (string)i.item_name,
                Quantity     = (int)i.quantity,
                UnitPrice    = (decimal)i.unit_price,
                Subtotal     = (decimal)i.subtotal,
                CategoryName = (string)i.category_name
            }).ToList()
        };
    }

    private async void BtnLastInvoice_Click(object s, RoutedEventArgs e)
    {
        try
        {
            // البحث عن آخر طلب
            var lastId = await _db.ExecuteScalarAsync<int?>(
                "SELECT TOP 1 order_id FROM orders ORDER BY order_id DESC");

            if (lastId == null)
            {
                MessageBox.Show("لا توجد فواتير مسجلة بعد.", "معلومة");
                return;
            }

            var order = await BuildOrderDetail(lastId.Value);
            var dlg   = new LastInvoiceDialog(order, _receiptSettings);
            dlg.Owner = Window.GetWindow(this);
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل آخر فاتورة: {ex.Message}", "خطأ");
        }
    }

    private async void BtnReprintLast_Click(object s, RoutedEventArgs e)
    {
        if (_lastOrderId == null)
        {
            MessageBox.Show("لا توجد فاتورة لإعادة طباعتها في هذه الجلسة.", "تنبيه");
            return;
        }
        try
        {
            var order = await BuildOrderDetail(_lastOrderId.Value);
            var win   = new PrintReceiptWindow(order, _receiptSettings, "نسخة العميل");
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إعادة الطباعة: {ex.Message}", "خطأ");
        }
    }

    private async void BtnSettings_Click(object s, RoutedEventArgs e)
    {
        var win = new PosSettingsWindow(_db);
        win.Owner = Window.GetWindow(this);
        if (win.ShowDialog() == true)
        {
            // إعادة تحميل كل شيء بعد حفظ الإعدادات
            await LoadAsync();
        }
    }
}
