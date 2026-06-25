using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Services;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Pos;

public partial class PosSettingsWindow : Window
{
    private readonly DbHelper _db;
    private int? _selectedCategoryId;
    private int? _selectedItemId;
    private List<dynamic> _allCategories = new();

    public PosSettingsWindow(DbHelper db)
    {
        InitializeComponent();
        _db = db;
        Loaded += async (_, _) => await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        await LoadCategories();
        await LoadItems();
        await LoadReceiptSettings();
    }

    // ==============================
    //  تبويب التصنيفات
    // ==============================
    private async Task LoadCategories()
    {
        _allCategories = (await _db.QueryAsync<dynamic>(
            "SELECT category_id, category_name, sort_order, is_active FROM menu_categories ORDER BY sort_order, category_name")).ToList();
        GridCategories.ItemsSource = _allCategories;

        // تحديث قائمة التصنيفات في تبويب المنتجات
        CmbCatFilter.Items.Clear();
        CmbCatFilter.Items.Add(new ComboBoxItem { Content = "كل التصنيفات", Tag = (int?)null });
        CmbItemCategory.Items.Clear();
        foreach (var c in _allCategories)
        {
            CmbCatFilter.Items.Add(new ComboBoxItem { Content = (string)c.category_name, Tag = (int?)c.category_id });
            CmbItemCategory.Items.Add(new ComboBoxItem { Content = (string)c.category_name, Tag = (int)c.category_id });
        }
        CmbCatFilter.SelectedIndex = 0;
    }

    private void GridCategories_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (GridCategories.SelectedItem is not IDictionary<string, object> row) return;
        _selectedCategoryId = (int?)row["category_id"];
        TxtCatName.Text     = (string)row["category_name"];
        TxtCatOrder.Text    = ((int)row["sort_order"]).ToString();
        ChkCatActive.IsChecked = (bool)row["is_active"];
    }

    private void BtnNewCategory_Click(object s, RoutedEventArgs e)
    {
        _selectedCategoryId = null;
        TxtCatName.Text     = "";
        TxtCatOrder.Text    = "0";
        ChkCatActive.IsChecked = true;
        GridCategories.SelectedItem = null;
    }

    private async void BtnSaveCategory_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCatName.Text))
        {
            MessageBox.Show("يرجى إدخال اسم التصنيف.", "تنبيه");
            return;
        }

        var name    = TxtCatName.Text.Trim();
        var order   = int.TryParse(TxtCatOrder.Text, out var o) ? o : 0;
        var isActive = ChkCatActive.IsChecked == true;

        try
        {
            if (_selectedCategoryId == null)
            {
                await _db.ExecuteAsync(
                    "INSERT INTO menu_categories (category_name, sort_order, is_active) VALUES (@name, @order, @active)",
                    new { name, order, active = isActive });
                MessageBox.Show("✅ تم إضافة التصنيف بنجاح.", "نجاح");
            }
            else
            {
                await _db.ExecuteAsync(
                    "UPDATE menu_categories SET category_name=@name, sort_order=@order, is_active=@active WHERE category_id=@id",
                    new { name, order, active = isActive, id = _selectedCategoryId });
                MessageBox.Show("✅ تم تحديث التصنيف بنجاح.", "نجاح");
            }
            await LoadCategories();
            await LoadItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ التصنيف: {ex.Message}", "خطأ");
        }
    }

    private async void BtnDeleteCategory_Click(object s, RoutedEventArgs e)
    {
        if (_selectedCategoryId == null)
        {
            MessageBox.Show("يرجى اختيار تصنيف أولاً.", "تنبيه");
            return;
        }

        var result = MessageBox.Show(
            "هل أنت متأكد من حذف هذا التصنيف؟\nسيؤثر ذلك على جميع الأصناف المرتبطة به.",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            // تعطيل بدلاً من الحذف لحماية الروابط
            await _db.ExecuteAsync(
                "UPDATE menu_categories SET is_active=0 WHERE category_id=@id",
                new { id = _selectedCategoryId });
            MessageBox.Show("✅ تم تعطيل التصنيف.", "نجاح");
            _selectedCategoryId = null;
            BtnNewCategory_Click(s, e);
            await LoadCategories();
            await LoadItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حذف التصنيف: {ex.Message}", "خطأ");
        }
    }

    // ==============================
    //  تبويب المنتجات
    // ==============================
    private List<dynamic> _allItems = new();

    private async Task LoadItems()
    {
        _allItems = (await _db.QueryAsync<dynamic>(
            @"SELECT mi.item_id, mi.item_name, mc.category_name, mi.category_id,
                     mi.price, mi.cost_price, mi.description, mi.is_available
              FROM menu_items mi
              JOIN menu_categories mc ON mi.category_id = mc.category_id
              ORDER BY mc.sort_order, mc.category_name, mi.item_name")).ToList();
        FilterAndDisplayItems();
    }

    private void FilterAndDisplayItems()
    {
        var q       = TxtItemSearch.Text.Trim();
        var catItem = CmbCatFilter.SelectedItem as ComboBoxItem;
        var catId   = catItem?.Tag as int?;

        var filtered = _allItems.AsEnumerable();
        if (!string.IsNullOrEmpty(q))
            filtered = filtered.Where(i => ((string)i.item_name).Contains(q, StringComparison.OrdinalIgnoreCase));
        if (catId.HasValue)
            filtered = filtered.Where(i => (int)i.category_id == catId.Value);

        GridItems.ItemsSource = filtered.ToList();
    }

    private void TxtItemSearch_Changed(object s, TextChangedEventArgs e) => FilterAndDisplayItems();
    private void CmbCatFilter_Changed(object s, SelectionChangedEventArgs e) => FilterAndDisplayItems();

    private void GridItems_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (GridItems.SelectedItem is not IDictionary<string, object> row) return;
        _selectedItemId    = (int?)row["item_id"];
        TxtItemName.Text   = (string)row["item_name"];
        TxtItemPrice.Text  = ((decimal)row["price"]).ToString("N2");
        TxtItemCost.Text   = ((decimal)row["cost_price"]).ToString("N2");
        TxtItemDesc.Text   = (string)(row["description"] ?? "");
        ChkItemAvailable.IsChecked = (bool)row["is_available"];

        // اختيار التصنيف
        var catId = (int)row["category_id"];
        foreach (ComboBoxItem item in CmbItemCategory.Items)
        {
            if (item.Tag is int id && id == catId)
            {
                CmbItemCategory.SelectedItem = item;
                break;
            }
        }
    }

    private void BtnNewItem_Click(object s, RoutedEventArgs e)
    {
        _selectedItemId    = null;
        TxtItemName.Text   = "";
        TxtItemPrice.Text  = "0";
        TxtItemCost.Text   = "0";
        TxtItemDesc.Text   = "";
        ChkItemAvailable.IsChecked = true;
        CmbItemCategory.SelectedIndex = CmbItemCategory.Items.Count > 0 ? 0 : -1;
        GridItems.SelectedItem = null;
    }

    private async void BtnSaveItem_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtItemName.Text))
        {
            MessageBox.Show("يرجى إدخال اسم الصنف.", "تنبيه");
            return;
        }
        if (CmbItemCategory.SelectedItem == null)
        {
            MessageBox.Show("يرجى اختيار تصنيف.", "تنبيه");
            return;
        }
        if (!decimal.TryParse(TxtItemPrice.Text, out var price) || price < 0)
        {
            MessageBox.Show("يرجى إدخال سعر صحيح.", "تنبيه");
            return;
        }

        var name      = TxtItemName.Text.Trim();
        var catId     = (int)((ComboBoxItem)CmbItemCategory.SelectedItem).Tag;
        var cost      = decimal.TryParse(TxtItemCost.Text, out var c) ? c : 0;
        var desc      = TxtItemDesc.Text.Trim();
        var available = ChkItemAvailable.IsChecked == true;

        try
        {
            if (_selectedItemId == null)
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO menu_items (category_id, item_name, price, cost_price, description, is_available)
                      VALUES (@catId, @name, @price, @cost, @desc, @avail)",
                    new { catId, name, price, cost, desc, avail = available });
                MessageBox.Show("✅ تم إضافة الصنف بنجاح.", "نجاح");
            }
            else
            {
                await _db.ExecuteAsync(
                    @"UPDATE menu_items SET category_id=@catId, item_name=@name, price=@price,
                        cost_price=@cost, description=@desc, is_available=@avail,
                        updated_at=GETDATE()
                      WHERE item_id=@id",
                    new { catId, name, price, cost, desc, avail = available, id = _selectedItemId });
                MessageBox.Show("✅ تم تحديث الصنف بنجاح.", "نجاح");
            }
            await LoadItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ الصنف: {ex.Message}", "خطأ");
        }
    }

    private async void BtnDeleteItem_Click(object s, RoutedEventArgs e)
    {
        if (_selectedItemId == null)
        {
            MessageBox.Show("يرجى اختيار صنف أولاً.", "تنبيه");
            return;
        }

        var result = MessageBox.Show(
            "هل تريد تعطيل هذا الصنف؟ (لن يظهر في قائمة الكاشير)",
            "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _db.ExecuteAsync(
                "UPDATE menu_items SET is_available=0 WHERE item_id=@id",
                new { id = _selectedItemId });
            MessageBox.Show("✅ تم تعطيل الصنف.", "نجاح");
            BtnNewItem_Click(s, e);
            await LoadItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ: {ex.Message}", "خطأ");
        }
    }

    // ==============================
    //  تبويب إعدادات الفاتورة
    // ==============================
    private async Task LoadReceiptSettings()
    {
        var rows = await _db.QueryAsync<dynamic>(
            "SELECT setting_key, value FROM settings WHERE setting_key LIKE 'receipt_%' OR setting_key='restaurant_name'");

        var dict = new Dictionary<string, string>();
        foreach (var row in rows)
            dict[(string)row.setting_key] = (string)(row.value ?? "");

        TxtRestaurantName.Text = dict.GetValueOrDefault("receipt_restaurant_name",
                                    dict.GetValueOrDefault("restaurant_name", "مطعم الإتقان"));
        TxtReceiptPhone.Text   = dict.GetValueOrDefault("receipt_phone",   "");
        TxtReceiptAddress.Text = dict.GetValueOrDefault("receipt_address", "");
        TxtTaxNumber.Text      = dict.GetValueOrDefault("receipt_tax_number", "");
        TxtReceiptFooter.Text  = dict.GetValueOrDefault("receipt_footer",  "شكراً لزيارتكم");
        TxtPrinterName.Text    = dict.GetValueOrDefault("receipt_printer_name", "");

        ChkShowLogo.IsChecked     = dict.GetValueOrDefault("receipt_show_logo",     "1") == "1";
        ChkShowOrderNum.IsChecked = dict.GetValueOrDefault("receipt_show_order_number", "1") == "1";
        ChkShowDateTime.IsChecked = dict.GetValueOrDefault("receipt_show_datetime", "1") == "1";
        ChkShowCashier.IsChecked  = dict.GetValueOrDefault("receipt_show_cashier",  "1") == "1";
        ChkShowTable.IsChecked    = dict.GetValueOrDefault("receipt_show_table",    "1") == "1";

        ChkLargeCustomer.IsChecked = dict.GetValueOrDefault("receipt_large_customer", "1") == "1";
        ChkLargeStaff.IsChecked    = dict.GetValueOrDefault("receipt_large_staff",    "1") == "1";
        ChkSmallSlips.IsChecked    = dict.GetValueOrDefault("receipt_small_slips",    "1") == "1";

        var slipBy = dict.GetValueOrDefault("receipt_slip_by", "category");
        RbSlipByCategory.IsChecked = slipBy == "category";
        RbSlipByGroup.IsChecked    = slipBy == "group";
    }

    private async void BtnSaveSettings_Click(object s, RoutedEventArgs e)
    {
        var settings = new Dictionary<string, string>
        {
            ["receipt_restaurant_name"]   = TxtRestaurantName.Text.Trim(),
            ["receipt_phone"]             = TxtReceiptPhone.Text.Trim(),
            ["receipt_address"]           = TxtReceiptAddress.Text.Trim(),
            ["receipt_tax_number"]        = TxtTaxNumber.Text.Trim(),
            ["receipt_footer"]            = TxtReceiptFooter.Text.Trim(),
            ["receipt_printer_name"]      = TxtPrinterName.Text.Trim(),
            ["receipt_show_logo"]         = ChkShowLogo.IsChecked == true ? "1" : "0",
            ["receipt_show_order_number"] = ChkShowOrderNum.IsChecked == true ? "1" : "0",
            ["receipt_show_datetime"]     = ChkShowDateTime.IsChecked == true ? "1" : "0",
            ["receipt_show_cashier"]      = ChkShowCashier.IsChecked == true ? "1" : "0",
            ["receipt_show_table"]        = ChkShowTable.IsChecked == true ? "1" : "0",
            ["receipt_large_customer"]    = ChkLargeCustomer.IsChecked == true ? "1" : "0",
            ["receipt_large_staff"]       = ChkLargeStaff.IsChecked == true ? "1" : "0",
            ["receipt_small_slips"]       = ChkSmallSlips.IsChecked == true ? "1" : "0",
            ["receipt_slip_by"]           = RbSlipByCategory.IsChecked == true ? "category" : "group",
        };

        try
        {
            foreach (var kv in settings)
            {
                var exists = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM settings WHERE setting_key=@key", new { key = kv.Key });

                if (exists > 0)
                    await _db.ExecuteAsync(
                        "UPDATE settings SET value=@val WHERE setting_key=@key",
                        new { val = kv.Value, key = kv.Key });
                else
                    await _db.ExecuteAsync(
                        "INSERT INTO settings (setting_key, value) VALUES (@key, @val)",
                        new { key = kv.Key, val = kv.Value });
            }

            MessageBox.Show("✅ تم حفظ إعدادات الفاتورة بنجاح.", "نجاح");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ");
        }
    }

    private async void BtnPreviewReceipt_Click(object s, RoutedEventArgs e)
    {
        // إنشاء فاتورة تجريبية للمعاينة
        var demoOrder = new OrderDetailModel
        {
            OrderId       = 999,
            CustomerName  = "أحمد العميل",
            PaymentMethod = "Cash",
            Subtotal      = 85.00m,
            DiscountAmount = 5.00m,
            TaxAmount     = 12.00m,
            TotalAmount   = 92.00m,
            OrderStatus   = "Completed",
            CreatedAt     = DateTime.Now,
            ServedBy      = App.CurrentUser?.FullName ?? "الكاشير",
            TableNumber   = "5",
            Items = new List<OrderItemDetailModel>
            {
                new() { ItemName="برغر دجاج", Quantity=2, UnitPrice=28, Subtotal=56, CategoryName="وجبات رئيسية" },
                new() { ItemName="كابتشينو",  Quantity=2, UnitPrice=15, Subtotal=30, CategoryName="مشروبات ساخنة" },
                new() { ItemName="سلطة خضراء", Quantity=1, UnitPrice=12, Subtotal=12, CategoryName="مقبلات" },
            }
        };

        var demoSettings = new ReceiptSettings
        {
            RestaurantName     = TxtRestaurantName.Text.Trim(),
            Phone              = TxtReceiptPhone.Text.Trim(),
            Address            = TxtReceiptAddress.Text.Trim(),
            TaxNumber          = TxtTaxNumber.Text.Trim(),
            Footer             = TxtReceiptFooter.Text.Trim(),
            ShowLogo           = ChkShowLogo.IsChecked == true,
            ShowOrderNumber    = ChkShowOrderNum.IsChecked == true,
            ShowDateTime       = ChkShowDateTime.IsChecked == true,
            ShowCashierName    = ChkShowCashier.IsChecked == true,
            ShowTableNumber    = ChkShowTable.IsChecked == true,
            PrintLargeCustomer = ChkLargeCustomer.IsChecked == true,
            PrintLargeStaff    = ChkLargeStaff.IsChecked == true,
            PrintSmallSlips    = ChkSmallSlips.IsChecked == true,
            SlipSplitBy        = RbSlipByCategory.IsChecked == true ? "category" : "group",
            ThermalPrinterName = TxtPrinterName.Text.Trim(),
            Currency           = "ريال",
            TaxEnabled         = true,
            TaxRate            = 15
        };

        var win = new PrintReceiptWindow(demoOrder, demoSettings, "نسخة تجريبية");
        win.Owner = this;
        win.ShowDialog();
    }

    private void BtnClose_Click(object s, RoutedEventArgs e) => Close();
}
