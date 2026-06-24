using Dapper;
using RestaurantMS.Desktop.Data;
using Microsoft.Data.SqlClient;
using RestaurantMS.Desktop.Models;
using System.Data.Common;
using System.Windows;
using System.Windows.Input;

namespace RestaurantMS.Desktop.Views;

public partial class SetupWindow : Window
{
    private readonly DbHelper _db;

    public SetupWindow()
    {
        InitializeComponent();
        _db = new DbHelper(App.ConnectionString);
        Loaded += (_, _) => TxtFullName.Focus();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        try { DragMove(); } catch { }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            try { DragMove(); } catch { }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnCreateAdmin_Click(sender, e);
    }

    private async void BtnCreateAdmin_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        var fullName   = TxtFullName.Text.Trim();
        var username   = TxtUsername.Text.Trim();
        var password   = TxtPassword.Password;
        var confirm    = TxtConfirmPassword.Password;
        var restName   = TxtRestName.Text.Trim();

        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(restName))
        {
            ShowError("يرجى ملء جميع الحقول.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");
            return;
        }

        if (password != confirm)
        {
            ShowError("كلمة المرور وتأكيدها غير متطابقين.");
            return;
        }

        if (username.Length < 3)
        {
            ShowError("اسم المستخدم يجب أن يكون 3 أحرف على الأقل.");
            return;
        }

        SetLoading(true);

        try
        {
            if (!await _db.TestConnectionAsync())
            {
                ShowError("تعذّر الاتصال بـ SQL Server Express.\nتأكد من تشغيله.");
                SetLoading(false);
                return;
            }

            var existing = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT username FROM users WHERE username = @username",
                new { username });

            if (existing != null)
            {
                ShowError("اسم المستخدم موجود بالفعل. اختر اسم آخر.");
                SetLoading(false);
                return;
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            using var conn = _db.OpenConnection();
            using var tx = conn.BeginTransaction();

            try
            {

                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM roles)
                    INSERT INTO roles (role_name) VALUES
                        (N'Owner'), (N'Admin'), (N'Manager'), (N'Cashier'), (N'Kitchen'), (N'Waiter');",
                    transaction: tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO branches (arabic_name, address, phone, is_active)
                    VALUES (@rn, N'الفرع الرئيسي', N'0500000000', 1);",
                    new { rn = restName }, transaction: tx);

                var branchId = await conn.ExecuteScalarAsync<int>(
                    "SELECT TOP 1 branch_id FROM branches ORDER BY branch_id DESC", transaction: tx);

                var roleId = await conn.ExecuteScalarAsync<int>(
                    "SELECT role_id FROM roles WHERE role_name = N'Owner'", transaction: tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO users (full_name, username, password_hash, email, role_id, branch_id, is_active)
                    VALUES (@fn, @un, @ph, @em, @rid, @bid, 1);",
                    new { fn = fullName, un = username, ph = hashedPassword, em = $"{username}@restaurant.com", rid = roleId, bid = branchId },
                    transaction: tx);

                var userId = await conn.ExecuteScalarAsync<int>(
                    "SELECT TOP 1 user_id FROM users ORDER BY user_id DESC", transaction: tx);

                await conn.ExecuteAsync(
                    "INSERT INTO settings (setting_key, value, description) VALUES (@k, @v, @d);",
                    new { k = "restaurant_name", v = restName, d = "اسم المطعم" }, transaction: tx);

                tx.Commit();

                App.CurrentUser = new CurrentUser
                {
                    UserId     = userId,
                    Username   = username,
                    FullName   = fullName,
                    RoleName   = "Owner",
                    BranchName = restName,
                    BranchId   = branchId
                };

                var main = new MainWindow();
                main.Show();
                Close();
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                throw new Exception($"فشل إنشاء البيانات:\n{ex.Message}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"حدث خطأ:\n{ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ShowError(string msg)
    {
        LblError.Text = msg;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void HideError() => ErrorPanel.Visibility = Visibility.Collapsed;

    private void SetLoading(bool loading)
    {
        BtnCreateAdmin.IsEnabled = !loading;
        TxtBtnLabel.Text  = loading ? "جارٍ الإنشاء..." : "إنشاء حساب المدير";
    }
}
