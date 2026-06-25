using RestaurantMS.Desktop.Data;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class PrinterSettingsWindow : Window
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private string _selectedPrinter = "";

    public PrinterSettingsWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadSettingsAsync();
        LoadAvailablePrinters();
    }

    private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            try { DragMove(); } catch { }
    }

    private void BtnClose_Click(object s, RoutedEventArgs e) => Close();

    // ===== تحميل الإعدادات من قاعدة البيانات =====
    private async Task LoadSettingsAsync()
    {
        try
        {
            var keys = new[] {
                "printer_thermal_name", "printer_thermal_type", "printer_thermal_address", "printer_thermal_port",
                "printer_large_name",   "printer_large_type",   "printer_large_address"
            };

            var settings = new Dictionary<string, string>();
            foreach (var k in keys)
            {
                var val = await _db.ExecuteScalarAsync<string>(
                    "SELECT value FROM settings WHERE setting_key=@k", new { k });
                settings[k] = val ?? "";
            }

            TxtThermalName.Text    = settings["printer_thermal_name"];
            TxtLargeName.Text      = settings["printer_large_name"];
            TxtThermalAddress.Text = settings["printer_thermal_address"];
            TxtThermalPort.Text    = settings.GetValueOrDefault("printer_thermal_port", "9100");
            TxtLargeAddress.Text   = settings.GetValueOrDefault("printer_large_address", "");

            // إعداد نوع الاتصال للطابعة الحرارية
            SetComboByTag(CmbThermalType, settings["printer_thermal_type"]);
            SetComboByTag(CmbLargeType,   settings["printer_large_type"]);

            UpdateThermalNetworkPanel();
            UpdateLargeNetworkPanel();
        }
        catch { }
    }

    private void SetComboByTag(ComboBox cmb, string tag)
    {
        foreach (ComboBoxItem item in cmb.Items)
        {
            if (item.Tag?.ToString() == tag)
            { cmb.SelectedItem = item; return; }
        }
        if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
    }

    // ===== قائمة الطابعات المتاحة =====
    private void LoadAvailablePrinters()
    {
        try
        {
            var printers = new List<string>();
            foreach (string p in PrinterSettings.InstalledPrinters)
                printers.Add(p);
            LstPrinters.ItemsSource = printers;
        }
        catch
        {
            LstPrinters.ItemsSource = new[] { "(تعذّر تحميل قائمة الطابعات)" };
        }
    }

    private void BtnRefreshPrinters_Click(object s, RoutedEventArgs e) => LoadAvailablePrinters();

    private void LstPrinters_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (LstPrinters.SelectedItem is string selected)
            _selectedPrinter = selected;
    }

    // ===== تبديل لوحة شبكة الطابعة الحرارية =====
    private void CmbThermalType_SelectionChanged(object s, SelectionChangedEventArgs e)
        => UpdateThermalNetworkPanel();

    private void UpdateThermalNetworkPanel()
    {
        var tag = ((CmbThermalType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Windows");
        PanelThermalNetwork.Visibility = tag == "Windows" ? Visibility.Collapsed : Visibility.Visible;
    }

    // ===== تبديل لوحة شبكة طابعة A4 =====
    private void CmbLargeType_SelectionChanged(object s, SelectionChangedEventArgs e)
        => UpdateLargeNetworkPanel();

    private void UpdateLargeNetworkPanel()
    {
        var tag = ((CmbLargeType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Windows");
        PanelLargeNetwork.Visibility = tag == "Windows" ? Visibility.Collapsed : Visibility.Visible;
    }

    // ===== اختبار الطباعة - الطابعة الحرارية =====
    private void BtnTestThermal_Click(object s, RoutedEventArgs e)
    {
        var printerName = TxtThermalName.Text.Trim();
        if (string.IsNullOrEmpty(printerName))
        {
            MessageBox.Show("يرجى إدخال اسم الطابعة الحرارية أولاً.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        PrintTestPage(printerName, "حرارية 80mm");
    }

    // ===== اختبار الطباعة - طابعة A4 =====
    private void BtnTestLarge_Click(object s, RoutedEventArgs e)
    {
        var printerName = TxtLargeName.Text.Trim();
        if (string.IsNullOrEmpty(printerName))
        {
            MessageBox.Show("يرجى إدخال اسم طابعة A4 أولاً.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        PrintTestPage(printerName, "A4");
    }

    private void PrintTestPage(string printerName, string type)
    {
        try
        {
            var pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = printerName;

            if (!pd.PrinterSettings.IsValid)
            {
                MessageBox.Show($"الطابعة '{printerName}' غير موجودة أو غير متاحة.\n" +
                                "تأكد من الاسم أو اختر من القائمة أدناه.",
                    "خطأ في الطابعة", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            pd.PrintPage += (_, args) =>
            {
                var g    = args.Graphics!;
                var font = new System.Drawing.Font("Arial", 12);
                var bold = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold);
                float y  = 20;

                g.DrawString("صفحة اختبار — RestaurantMS", bold, System.Drawing.Brushes.Black, 20, y); y += 30;
                g.DrawString($"نوع الطابعة: {type}", font, System.Drawing.Brushes.Black, 20, y); y += 22;
                g.DrawString($"اسم الطابعة: {printerName}", font, System.Drawing.Brushes.Black, 20, y); y += 22;
                g.DrawString($"التاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}", font, System.Drawing.Brushes.Black, 20, y); y += 22;
                g.DrawString("itQAN Soft — نظام إدارة المطاعم", font, System.Drawing.Brushes.DarkGray, 20, y);
            };

            pd.Print();
            MessageBox.Show($"✅ تم إرسال صفحة الاختبار إلى طابعة '{printerName}'.",
                "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الطباعة:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== حفظ الإعدادات =====
    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var thermalType  = (CmbThermalType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Windows";
            var largeType    = (CmbLargeType.SelectedItem  as ComboBoxItem)?.Tag?.ToString() ?? "Windows";

            var kvPairs = new Dictionary<string, string>
            {
                ["printer_thermal_name"]    = TxtThermalName.Text.Trim(),
                ["printer_thermal_type"]    = thermalType,
                ["printer_thermal_address"] = TxtThermalAddress.Text.Trim(),
                ["printer_thermal_port"]    = TxtThermalPort.Text.Trim(),
                ["printer_large_name"]      = TxtLargeName.Text.Trim(),
                ["printer_large_type"]      = largeType,
                ["printer_large_address"]   = TxtLargeAddress.Text.Trim(),
            };

            foreach (var kv in kvPairs)
            {
                var exists = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM settings WHERE setting_key=@k", new { k = kv.Key });
                if (exists > 0)
                    await _db.ExecuteAsync(
                        "UPDATE settings SET value=@v WHERE setting_key=@k",
                        new { v = kv.Value, k = kv.Key });
                else
                    await _db.ExecuteAsync(
                        "INSERT INTO settings (setting_key, value, description) VALUES (@k, @v, @k)",
                        new { k = kv.Key, v = kv.Value });
            }

            MessageBox.Show("✅ تم حفظ إعدادات الطابعات بنجاح!", "حفظ",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
