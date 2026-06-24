using Dapper;
using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Services;
using RestaurantMS.Desktop.Views;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantMS.Desktop.Views.Owner;

public partial class OwnerPortalWindow : Window
{
    private readonly bool _isFirstRun;
    private List<LicenseData> _storedLicenses = new();
    private static readonly string LicStoreDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "itQAN Soft", "RestaurantMS");
    private static readonly string LicStoreFile = Path.Combine(LicStoreDir, "licenses.json");

    public OwnerPortalWindow(bool isFirstRun = false)
    {
        InitializeComponent();
        _isFirstRun = isFirstRun;

        if (isFirstRun)
        {
            ShowPanel("FirstRun");
            TxtSetupExpiry_SetDefault();
        }
        else
        {
            var creds = OwnerCredentialsManager.LoadOwner();
            TxtLoginOwnerName.Text = $"مرحباً، {creds?.OwnerName ?? "مالك النظام"}";
            ShowPanel("Login");
        }

        MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) try { DragMove(); } catch { } };
        Loaded += (_, _) => TxtLicExpiry.Text = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd");
    }

    private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) try { DragMove(); } catch { }
    }

    private void BtnClose_Click(object s, RoutedEventArgs e)
        => Application.Current.Shutdown();

    private void BtnMinimize_Click(object s, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object s, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void TxtSetupExpiry_SetDefault() { }

    // ======================= First Run Setup =======================
    private void BtnSetupSave_Click(object s, RoutedEventArgs e)
    {
        SetupErrorPanel.Visibility = Visibility.Collapsed;

        var ownerName = TxtSetupOwnerName.Text.Trim();
        var software  = TxtSetupSoftware.Text.Trim();
        var email     = TxtSetupEmail.Text.Trim();
        var phone     = TxtSetupPhone.Text.Trim();
        var password  = TxtSetupPassword.Password;
        var confirm   = TxtSetupConfirm.Password;

        if (string.IsNullOrEmpty(ownerName))
        { ShowSetupError("يرجى إدخال اسم مالك النظام"); return; }
        if (string.IsNullOrEmpty(password))
        { ShowSetupError("يرجى إدخال كلمة المرور"); return; }
        if (password.Length < 6)
        { ShowSetupError("كلمة المرور يجب أن تكون 6 أحرف على الأقل"); return; }
        if (password != confirm)
        { ShowSetupError("كلمة المرور وتأكيدها غير متطابقين"); return; }

        try
        {
            OwnerCredentialsManager.SetupOwner(ownerName, password, email, phone, software);
            LoadStoredLicenses();
            ShowPortal();
        }
        catch (Exception ex)
        {
            ShowSetupError($"حدث خطأ: {ex.Message}");
        }
    }

    private void ShowSetupError(string msg)
    {
        SetupErrorTxt.Text = "⚠ " + msg;
        SetupErrorPanel.Visibility = Visibility.Visible;
    }

    // ======================= Owner Login =======================
    private void BtnOwnerLogin_Click(object s, RoutedEventArgs e)
    {
        LoginErrorPanel.Visibility = Visibility.Collapsed;
        var password = TxtLoginPassword.Password;
        if (string.IsNullOrEmpty(password))
        { ShowLoginError("يرجى إدخال كلمة المرور"); return; }

        if (!OwnerCredentialsManager.ValidateOwnerPassword(password))
        { ShowLoginError("كلمة المرور غير صحيحة"); TxtLoginPassword.Clear(); return; }

        LoadStoredLicenses();
        ShowPortal();
    }

    private void BtnBackToLogin_Click(object s, RoutedEventArgs e)
    {
        var loginWin = new LoginWindow();
        loginWin.Show();
        Close();
    }

    private void ShowLoginError(string msg)
    {
        LoginErrorTxt.Text = "⚠ " + msg;
        LoginErrorPanel.Visibility = Visibility.Visible;
    }

    // ======================= Portal =======================
    private void ShowPortal()
    {
        ShowPanel("Portal");
        var creds = OwnerCredentialsManager.LoadOwner();
        TxtSidebarOwnerName.Text = creds?.SoftwareName ?? "itQAN Soft";
        LoadOverview();
        LoadOwnerSettings();
        TxtPortalPageTitle.Text = "📊  نظرة عامة";
    }

    private void OwnerNav_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not string tag) return;
        foreach (var btn in OwnerNavPanel.Children.OfType<Button>())
            btn.Style = btn.Tag?.ToString() == tag
                ? (Style)Resources["OwnerNavBtnActive"]
                : (Style)Resources["OwnerNavBtn"];

        HideAllPortalPanels();
        switch (tag)
        {
            case "Overview":
                PanelOverview.Visibility = Visibility.Visible;
                TxtPortalPageTitle.Text = "📊  نظرة عامة";
                LoadOverview();
                break;
            case "NewLicense":
                PanelNewLicense.Visibility = Visibility.Visible;
                TxtPortalPageTitle.Text = "🔑  إنشاء ترخيص جديد";
                LicResultPanel.Visibility = Visibility.Collapsed;
                LicErrorPanel.Visibility  = Visibility.Collapsed;
                break;
            case "Licenses":
                PanelLicenses.Visibility = Visibility.Visible;
                TxtPortalPageTitle.Text = "📋  التراخيص المُنشأة";
                LoadLicensesGrid();
                break;
            case "RestaurantSetup":
                PanelRestaurantSetup.Visibility = Visibility.Visible;
                TxtPortalPageTitle.Text = "🏪  إعداد المطعم";
                RSErrorPanel.Visibility   = Visibility.Collapsed;
                RSSuccessPanel.Visibility = Visibility.Collapsed;
                break;
            case "OwnerSettings":
                PanelOwnerSettings.Visibility = Visibility.Visible;
                TxtPortalPageTitle.Text = "⚙️  إعدادات المالك";
                break;
        }
    }

    private void HideAllPortalPanels()
    {
        PanelOverview.Visibility         = Visibility.Collapsed;
        PanelNewLicense.Visibility       = Visibility.Collapsed;
        PanelLicenses.Visibility         = Visibility.Collapsed;
        PanelRestaurantSetup.Visibility  = Visibility.Collapsed;
        PanelOwnerSettings.Visibility    = Visibility.Collapsed;
    }

    private void LoadOverview()
    {
        TxtOverviewDate.Text = $"اليوم: {DateTime.Now:dddd, dd/MM/yyyy}";
        LoadStoredLicenses();
        TxtTotalLicenses.Text    = _storedLicenses.Count.ToString();
        TxtActiveLicenses.Text   = _storedLicenses.Count(l => !l.IsExpired).ToString();
        TxtExpiringSoon.Text     = _storedLicenses.Count(l => !l.IsExpired && l.RemainingDays <= 30).ToString();

        var creds = OwnerCredentialsManager.LoadOwner();
        if (creds != null)
        {
            TxtOwnerInfoName.Text     = $"المالك: {creds.OwnerName}";
            TxtOwnerInfoEmail.Text    = $"البريد: {(string.IsNullOrEmpty(creds.ContactEmail) ? "—" : creds.ContactEmail)}";
            TxtOwnerInfoPhone.Text    = $"الجوال: {(string.IsNullOrEmpty(creds.ContactPhone) ? "—" : creds.ContactPhone)}";
            TxtOwnerInfoSoftware.Text = $"الشركة: {creds.SoftwareName}";
        }
    }

    // ======================= License Generation =======================
    private void BtnGenerateLicense_Click(object s, RoutedEventArgs e)
    {
        LicErrorPanel.Visibility  = Visibility.Collapsed;
        LicResultPanel.Visibility = Visibility.Collapsed;

        var restName   = TxtLicRestName.Text.Trim();
        var custName   = TxtLicCustName.Text.Trim();
        var phone      = TxtLicPhone.Text.Trim();
        var email      = TxtLicEmail.Text.Trim();
        var expiry     = TxtLicExpiry.Text.Trim();
        var edition    = ((ComboBoxItem?)CmbLicEdition.SelectedItem)?.Content?.ToString() ?? "Full";
        var deviceFp   = TxtLicDevice.Text.Trim();

        if (string.IsNullOrEmpty(restName))
        { ShowLicError("يرجى إدخال اسم المطعم"); return; }
        if (string.IsNullOrEmpty(custName))
        { ShowLicError("يرجى إدخال اسم صاحب المطعم"); return; }
        if (string.IsNullOrEmpty(expiry))
        { ShowLicError("يرجى إدخال تاريخ الانتهاء"); return; }
        if (!DateTime.TryParse(expiry, out var expiryDate))
        { ShowLicError("تاريخ الانتهاء غير صحيح — استخدم الصيغة: yyyy-MM-dd"); return; }
        if (expiryDate <= DateTime.Now)
        { ShowLicError("تاريخ الانتهاء يجب أن يكون في المستقبل"); return; }

        if (!int.TryParse(TxtMaxPos.Text, out int maxPos))     maxPos = 1;
        if (!int.TryParse(TxtMaxKitchen.Text, out int maxKit)) maxKit = 1;
        if (!int.TryParse(TxtMaxCashier.Text, out int maxCash))maxCash= 2;
        if (!int.TryParse(TxtMaxTotal.Text, out int maxTotal)) maxTotal = 5;

        if (string.IsNullOrEmpty(deviceFp))
            deviceFp = "ANY";

        try
        {
            var licenseKey = LicenseGenerator.GenerateLicense(
                customerName: custName, customerPhone: phone, customerEmail: email,
                restaurantName: restName, expiryDate: expiryDate.ToString("yyyy-MM-dd"),
                edition: edition,
                maxPos: maxPos, maxKitchen: maxKit, maxCashier: maxCash, maxTotal: maxTotal,
                canUsePOS: ChkPOS.IsChecked == true,
                canUseKitchen: ChkKitchen.IsChecked == true,
                canUseInventory: ChkInventory.IsChecked == true,
                canUseCustomers: ChkCustomers.IsChecked == true,
                canUseSuppliers: ChkSuppliers.IsChecked == true,
                canUseSales: ChkSales.IsChecked == true,
                canUseReservations: ChkReservations.IsChecked == true,
                canUseReports: ChkReports.IsChecked == true,
                canUseAdmin: ChkAdmin.IsChecked == true,
                canUseMenu: ChkMenu.IsChecked == true,
                deviceFingerprint: deviceFp);

            TxtGeneratedKey.Text = licenseKey;
            LicResultPanel.Visibility = Visibility.Visible;

            var licData = new LicenseData
            {
                LicenseKey        = licenseKey,
                CustomerName      = custName,
                CustomerPhone     = phone,
                CustomerEmail     = email,
                RestaurantName    = restName,
                ExpiryDate        = expiryDate.ToString("yyyy-MM-dd"),
                CreatedAt         = DateTime.Now.ToString("yyyy-MM-dd"),
                MaxPosDevices     = maxPos,
                MaxKitchenDevices = maxKit,
                MaxCashierDevices = maxCash,
                MaxTotalDevices   = maxTotal,
                Edition           = edition,
                DeviceFingerprint = deviceFp,
                CanUsePOS         = ChkPOS.IsChecked == true,
                CanUseKitchen     = ChkKitchen.IsChecked == true,
                CanUseInventory   = ChkInventory.IsChecked == true,
                CanUseCustomers   = ChkCustomers.IsChecked == true,
                CanUseSuppliers   = ChkSuppliers.IsChecked == true,
                CanUseSales       = ChkSales.IsChecked == true,
                CanUseReservations= ChkReservations.IsChecked == true,
                CanUseReports     = ChkReports.IsChecked == true,
                CanUseAdmin       = ChkAdmin.IsChecked == true,
                CanUseMenu        = ChkMenu.IsChecked == true,
            };
            _storedLicenses.Add(licData);
            SaveStoredLicenses();
        }
        catch (Exception ex)
        {
            ShowLicError($"خطأ في إنشاء الترخيص: {ex.Message}");
        }
    }

    private void BtnCopyKey_Click(object s, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtGeneratedKey.Text))
        {
            Clipboard.SetText(TxtGeneratedKey.Text);
            MessageBox.Show("✅ تم نسخ مفتاح الترخيص إلى الحافظة", "نسخ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCopyStoredKey_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is string key && !string.IsNullOrEmpty(key))
        {
            Clipboard.SetText(key);
            MessageBox.Show("✅ تم نسخ مفتاح الترخيص إلى الحافظة", "نسخ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ShowLicError(string msg)
    {
        LicErrorTxt.Text = "⚠ " + msg;
        LicErrorPanel.Visibility = Visibility.Visible;
    }

    private void LoadLicensesGrid()
    {
        LoadStoredLicenses();
        GridLicenses.ItemsSource = _storedLicenses.Select(l => new
        {
            l.LicenseKey,
            l.RestaurantName,
            l.CustomerName,
            l.Edition,
            l.MaxTotalDevices,
            l.ExpiryDate,
            StatusTxt = l.IsExpired ? "⛔ منتهي" : $"✅ {l.RemainingDays} يوم"
        }).ToList();
    }

    // ======================= Restaurant Setup =======================
    private async void BtnSetupRestaurant_Click(object s, RoutedEventArgs e)
    {
        RSErrorPanel.Visibility   = Visibility.Collapsed;
        RSSuccessPanel.Visibility = Visibility.Collapsed;

        var restName = TxtRSRestName.Text.Trim();
        var mgrName  = TxtRSManagerName.Text.Trim();
        var username = TxtRSUsername.Text.Trim();
        var password = TxtRSPassword.Password;
        var confirm  = TxtRSConfirmPassword.Password;

        if (string.IsNullOrEmpty(restName) || string.IsNullOrEmpty(mgrName) ||
            string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        { ShowRSError("يرجى ملء جميع الحقول"); return; }
        if (password != confirm)
        { ShowRSError("كلمة المرور وتأكيدها غير متطابقين"); return; }
        if (password.Length < 6)
        { ShowRSError("كلمة المرور يجب أن تكون 6 أحرف على الأقل"); return; }

        BtnSetupRestaurant.IsEnabled = false;

        try
        {
            var db = new DbHelper(App.ConnectionString);
            if (!await db.TestConnectionAsync())
            { ShowRSError("تعذّر الاتصال بقاعدة البيانات.\nتأكد من تشغيل SQL Server Express."); return; }

            using var conn = db.OpenConnection();
            using var tx = conn.BeginTransaction();

            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM roles)
                INSERT INTO roles (role_name) VALUES
                    (N'Owner'), (N'Admin'), (N'Manager'), (N'Cashier'), (N'Kitchen'), (N'Waiter');",
                transaction: tx);

            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM branches WHERE arabic_name = @rn)
                INSERT INTO branches (arabic_name, address, phone, is_active)
                VALUES (@rn, N'الفرع الرئيسي', N'0500000000', 1);",
                new { rn = restName }, transaction: tx);

            var branchId = await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 branch_id FROM branches WHERE arabic_name = @rn ORDER BY branch_id DESC",
                new { rn = restName }, transaction: tx);

            var roleId = await conn.ExecuteScalarAsync<int>(
                "SELECT role_id FROM roles WHERE role_name = N'Admin'", transaction: tx);

            var existingUser = await conn.ExecuteScalarAsync<int?>(
                "SELECT user_id FROM users WHERE username = @un", new { un = username }, transaction: tx);
            if (existingUser != null)
            { tx.Rollback(); ShowRSError("اسم المستخدم موجود بالفعل. اختر اسماً آخر."); return; }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            await conn.ExecuteAsync(@"
                INSERT INTO users (full_name, username, password_hash, role_id, branch_id, is_active)
                VALUES (@fn, @un, @ph, @rid, @bid, 1);",
                new { fn = mgrName, un = username, ph = hash, rid = roleId, bid = branchId },
                transaction: tx);

            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='restaurant_name')
                INSERT INTO settings (setting_key, value, description) VALUES ('restaurant_name', @rn, N'اسم المطعم');
                ELSE
                UPDATE settings SET value=@rn WHERE setting_key='restaurant_name';",
                new { rn = restName }, transaction: tx);

            tx.Commit();
            RSSuccessTxt.Text = $"✅ تم تهيئة المطعم '{restName}' بنجاح!\nيمكن الدخول الآن بـ: {username}";
            RSSuccessPanel.Visibility = Visibility.Visible;
            TxtRSRestName.Clear(); TxtRSManagerName.Clear();
            TxtRSUsername.Clear(); TxtRSPassword.Clear(); TxtRSConfirmPassword.Clear();
        }
        catch (Exception ex)
        {
            ShowRSError($"حدث خطأ:\n{ex.Message}");
        }
        finally { BtnSetupRestaurant.IsEnabled = true; }
    }

    private void ShowRSError(string msg)
    {
        RSErrorTxt.Text = "⚠ " + msg;
        RSErrorPanel.Visibility = Visibility.Visible;
    }

    // ======================= Owner Settings =======================
    private void LoadOwnerSettings()
    {
        var creds = OwnerCredentialsManager.LoadOwner();
        if (creds == null) return;
        TxtSettOwnerName.Text = creds.OwnerName;
        TxtSettSoftware.Text  = creds.SoftwareName;
        TxtSettEmail.Text     = creds.ContactEmail;
        TxtSettPhone.Text     = creds.ContactPhone;
    }

    private void BtnSaveOwnerInfo_Click(object s, RoutedEventArgs e)
    {
        OwnerCredentialsManager.UpdateOwnerInfo(
            TxtSettOwnerName.Text.Trim(),
            TxtSettEmail.Text.Trim(),
            TxtSettPhone.Text.Trim(),
            TxtSettSoftware.Text.Trim());
        TxtSidebarOwnerName.Text = TxtSettSoftware.Text.Trim();
        MessageBox.Show("✅ تم حفظ المعلومات بنجاح", "حفظ", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnChangePassword_Click(object s, RoutedEventArgs e)
    {
        var np = TxtNewPassword.Password;
        var nc = TxtConfirmNewPassword.Password;
        if (string.IsNullOrEmpty(np)) { MessageBox.Show("أدخل كلمة المرور الجديدة"); return; }
        if (np.Length < 6) { MessageBox.Show("كلمة المرور يجب أن تكون 6 أحرف على الأقل"); return; }
        if (np != nc) { MessageBox.Show("كلمة المرور وتأكيدها غير متطابقين"); return; }
        OwnerCredentialsManager.ChangePassword(np);
        TxtNewPassword.Clear(); TxtConfirmNewPassword.Clear();
        MessageBox.Show("✅ تم تغيير كلمة المرور بنجاح", "تغيير كلمة المرور", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnExitPortal_Click(object s, RoutedEventArgs e)
    {
        var loginWin = new LoginWindow();
        loginWin.Show();
        Close();
    }

    // ======================= Helpers =======================
    private void ShowPanel(string panel)
    {
        PanelFirstRun.Visibility = panel == "FirstRun" ? Visibility.Visible : Visibility.Collapsed;
        PanelLogin.Visibility    = panel == "Login"    ? Visibility.Visible : Visibility.Collapsed;
        PanelPortal.Visibility   = panel == "Portal"   ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadStoredLicenses()
    {
        if (!File.Exists(LicStoreFile)) { _storedLicenses = new(); return; }
        try
        {
            var json = File.ReadAllText(LicStoreFile);
            _storedLicenses = JsonSerializer.Deserialize<List<LicenseData>>(json) ?? new();
        }
        catch { _storedLicenses = new(); }
    }

    private void SaveStoredLicenses()
    {
        Directory.CreateDirectory(LicStoreDir);
        File.WriteAllText(LicStoreFile, JsonSerializer.Serialize(_storedLicenses));
    }
}
