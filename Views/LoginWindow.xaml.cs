using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Views.Owner;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RestaurantMS.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly DbHelper _db;

    public LoginWindow()
    {
        InitializeComponent();
        _db = new DbHelper(App.ConnectionString);
        Loaded += (_, _) => TxtUsername.Focus();
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

    private void TxtUsername_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => UserPlaceholder.Visibility = string.IsNullOrEmpty(TxtUsername.Text)
            ? Visibility.Visible : Visibility.Collapsed;

    private void TxtUsername_GotFocus(object sender, RoutedEventArgs e)
        => UserFieldBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0x94, 0x1D));

    private void TxtUsername_LostFocus(object sender, RoutedEventArgs e)
        => UserFieldBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));

    private void TxtPassword_Changed(object sender, RoutedEventArgs e)
        => PassPlaceholder.Visibility = string.IsNullOrEmpty(TxtPassword.Password)
            ? Visibility.Visible : Visibility.Collapsed;

    private void TxtPassword_GotFocus(object sender, RoutedEventArgs e)
        => PassFieldBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0x94, 0x1D));

    private void TxtPassword_LostFocus(object sender, RoutedEventArgs e)
        => PassFieldBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnLogin_Click(sender, e);
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("يرجى إدخال اسم المستخدم وكلمة المرور");
            return;
        }

        SetLoading(true);
        HideError();

        try
        {
            if (!await _db.TestConnectionAsync())
            {
                ShowError("تعذّر الاتصال بـ SQL Server Express.\nتأكد من تشغيله ومن صحة الـ Connection String.");
                return;
            }

            var user = await _db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT u.user_id, u.username, u.full_name, u.password_hash,
                         u.role_id, u.branch_id, r.role_name,
                         b.arabic_name AS branch_name
                  FROM users u
                  LEFT JOIN roles    r ON u.role_id   = r.role_id
                  LEFT JOIN branches b ON u.branch_id = b.branch_id
                  WHERE u.username = @username AND u.is_active = 1",
                new { username });

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, (string)user.password_hash))
            {
                ShowError("اسم المستخدم أو كلمة المرور غير صحيحة");
                TxtPassword.Clear();
                TxtPassword.Focus();
                return;
            }

            await _db.ExecuteAsync(
                "UPDATE users SET last_login = GETDATE() WHERE user_id = @id",
                new { id = (int)user.user_id });

            App.CurrentUser = new CurrentUser
            {
                UserId     = (int)user.user_id,
                Username   = (string)user.username,
                FullName   = (string)user.full_name,
                RoleName   = (string)(user.role_name ?? "Cashier"),
                BranchName = (string)(user.branch_name ?? ""),
                BranchId   = (int)(user.branch_id ?? 1)
            };

            var main = new MainWindow();
            main.Show();
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في الاتصال:\n{ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void BtnOwnerPortal_Click(object sender, RoutedEventArgs e)
    {
        var portal = new OwnerPortalWindow(isFirstRun: false);
        portal.Show();
        Close();
    }

    private void ShowError(string msg)
    {
        LblError.Text = msg;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void HideError() => ErrorPanel.Visibility = Visibility.Collapsed;

    private void SetLoading(bool loading)
    {
        BtnLogin.IsEnabled = !loading;
        TxtLoginLabel.Text = loading ? "جارٍ التحقق..." : "تسجيل الدخول";
    }
}
