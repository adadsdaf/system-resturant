using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RestaurantMS.Desktop.Views.Profile;

public partial class ChangePasswordDialog : Window
{
    private readonly DbHelper _db = new(App.ConnectionString);

    public ChangePasswordDialog()
    {
        InitializeComponent();
        var u = App.CurrentUser;
        if (u != null)
            TxtUserLabel.Text = $"{u.FullName}  —  {u.RoleName}";
    }

    private void Header_MouseDown(object s, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            try { DragMove(); } catch { }
    }

    private void Pass_Changed(object s, RoutedEventArgs e)
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        HighlightBorder(CurrentPassBorder, false);
        HighlightBorder(NewPassBorder, false);
        HighlightBorder(ConfirmPassBorder, false);
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        var current = TxtCurrentPass.Password;
        var newPass  = TxtNewPass.Password;
        var confirm  = TxtConfirmPass.Password;

        if (string.IsNullOrWhiteSpace(current))
        { ShowError("يرجى إدخال كلمة المرور الحالية"); HighlightBorder(CurrentPassBorder, true); return; }
        if (string.IsNullOrWhiteSpace(newPass))
        { ShowError("يرجى إدخال كلمة المرور الجديدة"); HighlightBorder(NewPassBorder, true); return; }
        if (newPass.Length < 6)
        { ShowError("كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل"); HighlightBorder(NewPassBorder, true); return; }
        if (newPass != confirm)
        { ShowError("كلمة المرور الجديدة وتأكيدها غير متطابقين"); HighlightBorder(ConfirmPassBorder, true); return; }

        try
        {
            var hash = await _db.ExecuteScalarAsync<string>(
                "SELECT password_hash FROM users WHERE user_id = @id",
                new { id = App.CurrentUser!.UserId });

            if (hash == null || !BCrypt.Net.BCrypt.Verify(current, hash))
            {
                ShowError("كلمة المرور الحالية غير صحيحة");
                HighlightBorder(CurrentPassBorder, true);
                return;
            }

            var newHash = BCrypt.Net.BCrypt.HashPassword(newPass);
            await _db.ExecuteAsync(
                "UPDATE users SET password_hash = @h WHERE user_id = @id",
                new { h = newHash, id = App.CurrentUser!.UserId });

            MessageBox.Show("تم تغيير كلمة المرور بنجاح ✅",
                "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ أثناء الحفظ: {ex.Message}");
        }
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string msg)
    {
        TxtError.Text = msg;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private static void HighlightBorder(System.Windows.Controls.Border b, bool error)
    {
        b.BorderBrush = error
            ? new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26))
            : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
    }
}
