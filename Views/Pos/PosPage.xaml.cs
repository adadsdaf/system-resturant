using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
    private readonly DispatcherTimer _clock = new();

    // ألوان الأصناف الملونة (كما في الصورة)
    private static readonly string[] ItemColors =
    {
        "#D4AF37", // ذهبي
        "#5DBB63", // أخضر
        "#A8D5A2", // أخضر فاتح
        "#F0C040", // أصفر
        "#7BC67E", // أخضر متوسط
        "#C8E6C9", // أخضر شاحب
        "#FFD54F", // أصفر ذهبي
        "#81C784", // أخضر
        "#B5D5A5", // أخضر زيتوني
    };

    public PosPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();

        // ساعة حية في صفحة الكاشير
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick    += (_, _) => TxtClock.Text = DateTime.Now.ToString("HH:mm:ss — dd/MM/yyyy");
        _clock.Start();
        TxtClock.Text = DateTime.Now.ToString("HH:mm:ss — dd/MM/yyyy");

        // إيقاف الساعة عند مغادرة الصفحة
        Unloaded += (_, _) => _clock.Stop();
    }

    // ===================================================================
    //  تحميل البيانات
    // ===================================================================
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
            MessageBox.Show(
                $"خطأ في تحميل بيانات الكاشير:\n{ex.Message}\n\nتأكد من اتصال قاعدة البيانات.",
                "خطأ التحميل", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadSettings()
    {
        try
        {
            _currency   = await _db.ExecuteScalarAsync<string>(
                              "SELECT TOP 1 value FROM settings WHERE setting_key='currency'") ?? "ريال";
            var taxEn   = await _db.ExecuteScalarAsync<string>(
                              "SELECT TOP 1 value FROM settings WHERE setting_key='tax_enabled'") ?? "0";
            var taxR    = await _db.ExecuteScalarAsync<string>(
                              "SELECT TOP 1 value FROM settings WHERE setting_key='tax_rate'") ?? "0";
            _taxEnabled = taxEn == "1";
            _taxRate    = double.TryParse(taxR, out var r) ? r : 0;

            if (TxtCurrencyLabel1 != null) TxtCurrencyLabel1.Text = $" {_currency}";
            if (TxtCurrencyLabel2 != null) TxtCurrencyLabel2.Text = $" {_currency}";
            if (TxtCurrencyLabel3 != null) TxtCurrencyLabel3.Text = $" {_currency}";
            if (TxtCurrencyLabel4 != null) TxtCurrencyLabel4.Text = $" {_currency}";
            if (TaxRow   != null) TaxRow.Visibility  = _taxEnabled ? Visibility.Visible : Visibility.Collapsed;
            if (TxtTaxLabel != null) TxtTaxLabel.Text = $"الضريبة ({_taxRate:0}%)";

            _receiptSettings = await LoadReceiptSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تحذير: فشل تحميل الإعدادات:\n{ex.Message}", "تحذير");
        }
    }

    private async Task<ReceiptSettings> LoadReceiptSettings()
    {
        var settings = new ReceiptSettings
        {
            Currency   = _currency,
            TaxEnabled = _taxEnabled,
            TaxRate    = _taxRate
        };

        try
        {
            var rows = await _db.QueryAsync<dynamic>(
                "SELECT setting_key, value FROM settings WHERE setting_key LIKE 'receipt_%'");

            foreach (var row in rows)
            {
                string? key = row?.setting_key as string;
                if (key == null) continue;
                string val = row?.value as string ?? "";
                switch (key)
                {
                    case "receipt_restaurant_name":   settings.RestaurantName     = val; break;
                    case "receipt_phone":             settings.Phone              = val; break;
                    case "receipt_address":           settings.Address            = val; break;
                    case "receipt_tax_number":        settings.TaxNumber          = val; break;
                    case "receipt_footer":            settings.Footer             = val; break;
                    case "receipt_show_logo":         settings.ShowLogo           = val == "1"; break;
                    case "receipt_large_customer":    settings.PrintLargeCustomer = val == "1"; break;
                    case "receipt_large_staff":       settings.PrintLargeStaff    = val == "1"; break;
                    case "receipt_small_slips":       settings.PrintSmallSlips    = val == "1"; break;
                    case "receipt_slip_by":           settings.SlipSplitBy        = val; break;
                    case "receipt_printer_name":      settings.ThermalPrinterName = val; break;
                    case "receipt_show_order_number": settings.ShowOrderNumber    = val == "1"; break;
                    case "receipt_show_datetime":     settings.ShowDateTime       = val == "1"; break;
                    case "receipt_show_cashier":      settings.ShowCashierName    = val == "1"; break;
                    case "receipt_show_table":        settings.ShowTableNumber    = val == "1"; break;
                }
            }

            if (string.IsNullOrEmpty(settings.RestaurantName))
            {
                settings.RestaurantName = await _db.ExecuteScalarAsync<string>(
                    "SELECT TOP 1 value FROM settings WHERE setting_key='restaurant_name'") ?? "مطعم الإتقان";
            }
        }
        catch { /* استخدام القيم الافتراضية */ }

        return settings;
    }

    private async Task LoadCategories()
    {
        try
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
            {
                string? catName = c?.category_name as string;
                int?    catId   = c?.category_id   is int cid ? cid : (int?)null;
                if (catName != null && catId != null)
                    CatPanel.Children.Add(MakeCatButton(catName, catId, false));
            }
        }
        catch { /* تجاهل خطأ التصنيفات */ }
    }

    private async Task LoadAllItems()
    {
        try
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
        catch { /* تجاهل خطأ الأصناف */ }
    }

    private async Task LoadTables()
    {
        try
        {
            var tables = await _db.QueryAsync<dynamic>(
                "SELECT table_id, table_number FROM tables WHERE is_active=1 ORDER BY table_number");
            CmbTable.Items.Clear();
            CmbTable.Items.Add("بدون طاولة");
            foreach (var t in tables)
                CmbTable.Items.Add($"طاولة {t.table_number}");
            CmbTable.SelectedIndex = 0;
        }
        catch
        {
            CmbTable.Items.Clear();
            CmbTable.Items.Add("بدون طاولة");
            CmbTable.SelectedIndex = 0;
        }
    }

    private async Task LoadTodayStats()
    {
        try
        {
            var todaySales = await _db.ExecuteScalarAsync<decimal?>(
                "SELECT ISNULL(SUM(total_amount),0) FROM orders WHERE CAST(created_at AS DATE)=CAST(GETDATE() AS DATE)");
            var todayCount = await _db.ExecuteScalarAsync<int?>(
                "SELECT ISNULL(COUNT(*),0) FROM orders WHERE CAST(created_at AS DATE)=CAST(GETDATE() AS DATE)");
            TxtTodaySales.Text  = $"مبيعات اليوم: {todaySales ?? 0:N2} {_currency}";
            TxtTodayOrders.Text = $"طلبات: {todayCount ?? 0}";
        }
        catch { /* تجاهل خطأ الإحصائيات */ }
    }

    // ===================================================================
    //  أزرار التصنيفات
    // ===================================================================
    private Button MakeCatButton(string name, int? catId, bool active)
    {
        var btn = new Button
        {
            Content         = name,
            Tag             = catId,
            Margin          = new Thickness(0, 0, 6, 0),
            Padding         = new Thickness(12, 6, 12, 6),
            Background      = active
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
            Foreground      = active ? Brushes.White
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
            BorderThickness = new Thickness(1),
            BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString(active ? "#F7941D" : "#334155")),
            Cursor          = Cursors.Hand,
            FontSize        = 12,
            FontWeight      = active ? FontWeights.Bold : FontWeights.Normal
        };

        btn.Template = CreateRoundedButtonTemplate();
        btn.Click   += (_, _) =>
        {
            foreach (Button b in CatPanel.Children.OfType<Button>())
            {
                b.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b"));
                b.Foreground  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                b.FontWeight  = FontWeights.Normal;
                b.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            }
            btn.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D"));
            btn.Foreground  = Brushes.White;
            btn.FontWeight  = FontWeights.Bold;
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D"));

            var filtered = catId == null ? _allItems
                : _allItems.Where(i => i?.category_id is int cid && cid == catId.Value).ToList();
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
        border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness")
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

    // ===================================================================
    //  عرض الأصناف بتصميم الصورة (ملونة)
    // ===================================================================
    private void RenderItems(IEnumerable<dynamic> items)
    {
        ItemsPanel.Children.Clear();
        int idx = 0;
        foreach (var item in items)
        {
            ItemsPanel.Children.Add(MakeItemCard(item, idx));
            idx++;
        }
    }

    private Border MakeItemCard(dynamic item, int index)
    {
        // استخراج القيم بأمان من الكائن الديناميكي
        int    itemId   = item?.item_id      is int    id  ? id  : 0;
        string itemName = item?.item_name    as string     ?? "—";
        decimal price   = item?.price        is decimal p  ? p
                        : item?.price        is double  pd ? (decimal)pd
                        : item?.price        is float   pf ? (decimal)pf
                        : item?.price        is long    pl ? (decimal)pl
                        : item?.price        is int     pi ? (decimal)pi : 0m;
        int    catId    = item?.category_id  is int    cid ? cid : 0;
        string catName  = item?.category_name as string    ?? "";

        // اختيار اللون من المصفوفة بشكل دوري
        var colorHex = ItemColors[index % ItemColors.Length];
        var bgColor  = (Color)ColorConverter.ConvertFromString(colorHex);
        var darkColor = Color.FromRgb(
            (byte)Math.Max(bgColor.R - 30, 0),
            (byte)Math.Max(bgColor.G - 30, 0),
            (byte)Math.Max(bgColor.B - 30, 0));

        var card = new Border
        {
            Width           = 140,
            Height          = 90,
            Margin          = new Thickness(3),
            Background      = new SolidColorBrush(bgColor),
            CornerRadius    = new CornerRadius(8),
            Cursor          = Cursors.Hand,
            BorderThickness = new Thickness(0)
        };

        // تأثير hover
        card.MouseEnter += (_, _) => card.Background = new SolidColorBrush(darkColor);
        card.MouseLeave += (_, _) => card.Background = new SolidColorBrush(bgColor);

        // محتوى البطاقة
        var grid = new Grid { Margin = new Thickness(8, 6, 8, 6) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // الصف الأول: رقم الصنف والسعر
        var topGrid = new Grid();
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lblId = new TextBlock
        {
            Text       = $"({itemId})",
            FontSize   = 10,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Top
        };
        var lblPrice = new TextBlock
        {
            Text       = $"{price:N0}",
            FontSize   = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            VerticalAlignment = VerticalAlignment.Top,
            TextAlignment = TextAlignment.Left
        };
        Grid.SetColumn(lblPrice, 1);
        topGrid.Children.Add(lblId);
        topGrid.Children.Add(lblPrice);
        Grid.SetRow(topGrid, 0);
        grid.Children.Add(topGrid);

        // الصف الثاني: اسم الصنف
        var lblName = new TextBlock
        {
            Text          = itemName,
            FontSize      = 12,
            FontWeight    = FontWeights.SemiBold,
            Foreground    = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
            TextWrapping  = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0)
        };
        Grid.SetRow(lblName, 1);
        grid.Children.Add(lblName);

        // الصف الثالث: اسم التصنيف
        var lblCat = new TextBlock
        {
            Text      = catName,
            FontSize  = 9,
            Foreground = new SolidColorBrush(Color.FromArgb(160, 20, 20, 20)),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetRow(lblCat, 2);
        grid.Children.Add(lblCat);

        card.Child = grid;

        card.MouseLeftButtonUp += (_, _) =>
            AddToCart(itemId, itemName, price, catId, catName);
        return card;
    }

    // ===================================================================
    //  السلة
    // ===================================================================
    private void AddToCart(int id, string name, decimal price, int catId, string catName)
    {
        var existing = _cart.FirstOrDefault(i => i.ItemId == id);
        if (existing != null)
            existing.Quantity++;
        else
            _cart.Add(new CartItemModel
            {
                ItemId = id, Name = name, Price = price,
                Quantity = 1, CategoryId = catId, CategoryName = catName
            });
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
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                FontSize = 12, TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(10)
            });
            UpdateTotals();
            return;
        }

        foreach (var item in _cart)
        {
            var capturedItem = item;

            var row = new Border
            {
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131c2b")),
                BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(8, 6, 8, 6)
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

            // الاسم
            var nameBlock = new TextBlock
            {
                Text          = item.Name,
                Foreground    = Brushes.White,
                FontSize      = 12,
                FontWeight    = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            // الكمية مع أزرار ±
            var qtyPanel = new StackPanel
            {
                Orientation       = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var minus = MakeSmallBtn("−", "#ef4444");
            var qtyTxt = new TextBlock
            {
                Text              = item.Quantity.ToString(),
                Foreground        = Brushes.White,
                FontSize          = 13,
                FontWeight        = FontWeights.Bold,
                Width             = 22,
                TextAlignment     = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var plus = MakeSmallBtn("+", "#22c55e");

            minus.Click += (_, _) =>
            {
                if (capturedItem.Quantity > 1) capturedItem.Quantity--;
                else _cart.Remove(capturedItem);
                RenderCart();
            };
            plus.Click  += (_, _) => { capturedItem.Quantity++; RenderCart(); };

            qtyPanel.Children.Add(minus);
            qtyPanel.Children.Add(qtyTxt);
            qtyPanel.Children.Add(plus);

            // السعر الإجمالي للصف
            var priceBlock = new TextBlock
            {
                Text              = $"{item.LineTotal:N0}",
                Foreground        = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D")),
                FontSize          = 12,
                FontWeight        = FontWeights.Bold,
                TextAlignment     = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // زر الحذف
            var delBtn = MakeSmallBtn("✕", "#475569");
            delBtn.Click += (_, _) => { _cart.Remove(capturedItem); RenderCart(); };

            Grid.SetColumn(qtyPanel, 1);
            Grid.SetColumn(priceBlock, 2);
            Grid.SetColumn(delBtn, 3);

            g.Children.Add(nameBlock);
            g.Children.Add(qtyPanel);
            g.Children.Add(priceBlock);
            g.Children.Add(delBtn);

            row.Child = g;
            CartPanel.Children.Add(row);
        }
        UpdateTotals();
    }

    private static Button MakeSmallBtn(string content, string colorHex)
    {
        var btn = new Button
        {
            Content = content, Width = 22, Height = 22, FontSize = 13,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
        };
        var tpl    = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(cp);
        tpl.VisualTree = border;
        btn.Template   = tpl;
        return btn;
    }

    // ===================================================================
    //  حساب المجاميع
    // ===================================================================
    private void UpdateTotals()
    {
        var subtotal    = _cart.Sum(i => i.LineTotal);
        var discountVal = double.TryParse(TxtDiscountVal.Text, out var d) ? d : 0;
        var isPercent   = CmbDiscountType.SelectedIndex == 0;
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
            TxtChange.Text       = $"{(change >= 0 ? change : 0):N2}";
            TxtChange.Foreground = change >= 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
        }
    }

    // ===================================================================
    //  طرق الدفع — إصلاح bug عدم إعادة ضبط لون النص
    // ===================================================================
    private void SetPaymentMethod(string method, Border active)
    {
        _payMethod = method;

        // إعادة ضبط جميع أزرار الدفع
        BrdCash.Background     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2332"));
        BrdCard.Background     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2332"));
        BrdTransfer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2332"));

        BrdCash.BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
        BrdCard.BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
        BrdTransfer.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));

        BrdCash.BorderThickness     = new Thickness(1);
        BrdCard.BorderThickness     = new Thickness(1);
        BrdTransfer.BorderThickness = new Thickness(1);

        // إعادة ضبط لون نص جميع الأزرار
        TxtCash.Foreground     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
        TxtCard.Foreground     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
        TxtTransfer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

        // تفعيل الزر المختار
        active.Background     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7941D"));
        active.BorderBrush    = Brushes.Transparent;
        active.BorderThickness = new Thickness(0);

        // تحديد نص الزر النشط بالأبيض
        if (active == BrdCash)     TxtCash.Foreground     = Brushes.White;
        else if (active == BrdCard)    TxtCard.Foreground     = Brushes.White;
        else if (active == BrdTransfer) TxtTransfer.Foreground = Brushes.White;
    }

    private void PayCash_Click(object s, MouseButtonEventArgs e)
    {
        SetPaymentMethod("Cash", BrdCash);
        TxtAmountPaid.Text = TxtTotal.Text;
    }
    private void PayCard_Click(object s, MouseButtonEventArgs e)     => SetPaymentMethod("Card", BrdCard);
    private void PayTransfer_Click(object s, MouseButtonEventArgs e) => SetPaymentMethod("Transfer", BrdTransfer);

    // ===================================================================
    //  لوحة الأرقام
    // ===================================================================
    private void NumPad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string digit)
        {
            var current = TxtAmountPaid.Text;
            if (digit == "." && current.Contains('.')) return;
            if (current == "0" && digit != ".") TxtAmountPaid.Text = digit;
            else TxtAmountPaid.Text = current + digit;
        }
    }

    private void NumPadClear_Click(object sender, RoutedEventArgs e)
    {
        if (TxtAmountPaid.Text.Length > 1)
            TxtAmountPaid.Text = TxtAmountPaid.Text[..^1];
        else
            TxtAmountPaid.Text = "0";
    }

    // ===================================================================
    //  أحداث البحث والخصم
    // ===================================================================
    private void TxtSearch_Changed(object s, TextChangedEventArgs e)
    {
        if (SearchPlaceholder != null)
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch?.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        var q = TxtSearch?.Text?.Trim() ?? "";
        RenderItems(string.IsNullOrEmpty(q) ? _allItems
            : _allItems.Where(i =>
            {
                string name = i?.item_name as string ?? "";
                return name.Contains(q, StringComparison.OrdinalIgnoreCase);
            }));
    }

    private void TxtDiscount_Changed(object s, object e) => UpdateTotals();
    private void TxtAmountPaid_Changed(object s, TextChangedEventArgs e) => UpdateChange();

    // ===================================================================
    //  مسح الطلب
    // ===================================================================
    private void BtnClearCart_Click(object s, RoutedEventArgs e)
    {
        _cart.Clear();
        _customerId = null;
        TxtCustomerInfo.Text   = "زبون عادي";
        TxtDiscountVal.Text    = "0";
        TxtAmountPaid.Text     = "0";
        TxtNotes.Text          = "";
        if (CmbTable.Items.Count > 0)    CmbTable.SelectedIndex    = 0;
        if (CmbOrderType.Items.Count > 0) CmbOrderType.SelectedIndex = 0;
        SetPaymentMethod("Cash", BrdCash);
        RenderCart();
    }

    // ===================================================================
    //  البحث عن عميل
    // ===================================================================
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
            MessageBox.Show($"خطأ في البحث عن العملاء:\n{ex.Message}", "خطأ");
        }
    }

    // ===================================================================
    //  إتمام الطلب
    // ===================================================================
    private async void BtnConfirmOrder_Click(object s, RoutedEventArgs e)
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("السلة فارغة!\nيرجى إضافة أصناف قبل إتمام الطلب.",
                "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var subtotal    = _cart.Sum(i => i.LineTotal);
        var discountVal = double.TryParse(TxtDiscountVal.Text, out var d) ? d : 0;
        var isPercent   = CmbDiscountType.SelectedIndex == 0;
        var discountAmt = isPercent
            ? (decimal)Math.Round((double)subtotal * discountVal / 100, 2)
            : (decimal)Math.Min(discountVal, (double)subtotal);
        var taxable     = (double)(subtotal - discountAmt);
        var taxAmt      = (decimal)(_taxEnabled ? Math.Round(taxable * _taxRate / 100, 2) : 0);
        var total       = Math.Round(subtotal - discountAmt + taxAmt, 2);

        var userId    = App.CurrentUser!.UserId;
        var branchId  = App.CurrentUser!.BranchId;
        var tableStr  = CmbTable.SelectedItem?.ToString()?.Replace("طاولة ", "") ?? "";
        var orderType = (CmbOrderType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "داخل المطعم";
        var notes     = TxtNotes.Text.Trim();

        BtnConfirm.IsEnabled = false;
        BtnConfirm.Content   = "⏳ جاري الحفظ...";

        using var conn = _db.OpenConnection();
        var tx = conn.BeginTransaction();
        try
        {
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

            foreach (var item in _cart)
                await Dapper.SqlMapper.ExecuteAsync(conn,
                    @"INSERT INTO order_items
                        (order_id, menu_item_id, item_name, quantity, unit_price, subtotal)
                      VALUES (@oid, @iid, @name, @qty, @price, @sub)",
                    new { oid = orderId, iid = item.ItemId, name = item.Name,
                          qty = item.Quantity, price = item.Price, sub = item.LineTotal }, tx);

            await Dapper.SqlMapper.ExecuteAsync(conn,
                "INSERT INTO payments (order_id, amount_paid, payment_method) VALUES (@oid, @amt, @pm)",
                new { oid = orderId, amt = total, pm = _payMethod }, tx);

            var kitchenIds = (await Dapper.SqlMapper.QueryAsync<int>(conn,
                @"INSERT INTO kitchen_orders (order_id, table_number, customer_name, notes)
                  OUTPUT INSERTED.kitchen_order_id
                  VALUES (@oid, @tbl, @cn, @notes)",
                new
                {
                    oid   = orderId,
                    tbl   = tableStr,
                    cn    = TxtCustomerInfo.Text,
                    notes = $"{orderType} | {notes}".Trim('|', ' ')
                }, tx)).ToList();

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

            _receiptSettings = await LoadReceiptSettings();
            var orderDetail  = await BuildOrderDetail(orderId);
            PrintOrder(orderDetail);

            await LoadTodayStats();

            BtnClearCart_Click(s, e);

            MessageBox.Show(
                $"✅ تم حفظ الطلب رقم #{orderId}\nالإجمالي: {total:N2} {_currency}",
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
            BtnConfirm.Content   = "✅ إتمام الطلب والطباعة";
        }
    }

    // ===================================================================
    //  الطباعة
    // ===================================================================
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
        var groups = order.Items
            .GroupBy(i => i.CategoryName)
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
            OrderId        = orderId,
            CustomerName   = order?.customer_name   as string ?? "زبون عادي",
            PaymentMethod  = order?.payment_method  as string ?? "Cash",
            Subtotal       = order?.subtotal       is decimal sub  ? sub  : 0m,
            DiscountAmount = order?.discount_amount is decimal disc ? disc : 0m,
            TaxAmount      = order?.tax_amount      is decimal tax  ? tax  : 0m,
            TotalAmount    = order?.total_amount    is decimal tot  ? tot  : 0m,
            OrderStatus    = order?.order_status   as string ?? "",
            CreatedAt      = order?.created_at     is DateTime dt  ? dt   : DateTime.Now,
            ServedBy       = order?.served_by_name as string ?? "",
            TableNumber    = order?.table_number   as string ?? "",
            Items          = items.Select(i => new OrderItemDetailModel
            {
                ItemName     = i?.item_name     as string  ?? "",
                Quantity     = i?.quantity      is int qty ? qty  : 0,
                UnitPrice    = i?.unit_price    is decimal up  ? up   :
                               i?.unit_price    is double  upd ? (decimal)upd : 0m,
                Subtotal     = i?.subtotal      is decimal st  ? st   :
                               i?.subtotal      is double  std ? (decimal)std : 0m,
                CategoryName = i?.category_name as string  ?? ""
            }).ToList()
        };
    }

    // ===================================================================
    //  آخر فاتورة وإعادة الطباعة
    // ===================================================================
    private async void BtnLastInvoice_Click(object s, RoutedEventArgs e)
    {
        try
        {
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
            MessageBox.Show($"خطأ في تحميل آخر فاتورة:\n{ex.Message}", "خطأ");
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
            MessageBox.Show($"خطأ في إعادة الطباعة:\n{ex.Message}", "خطأ");
        }
    }

    // ===================================================================
    //  إعدادات الكاشير
    // ===================================================================
    private async void BtnSettings_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var win = new PosSettingsWindow(_db);
            win.Owner = Window.GetWindow(this);
            if (win.ShowDialog() == true)
                await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح الإعدادات:\n{ex.Message}", "خطأ");
        }
    }
}
