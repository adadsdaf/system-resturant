using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class UserDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _user;

    public UserDialog(DbHelper db, dynamic? user)
    {
        InitializeComponent();
        _db   = db;
        _user = user;
        Loaded += async (_, _) =>
        {
            var roles = await _db.QueryAsync<dynamic>("SELECT role_id, role_name FROM roles ORDER BY role_name");
            CmbRole.ItemsSource = roles.ToList();

            var branches = await _db.QueryAsync<dynamic>("SELECT branch_id, arabic_name FROM branches WHERE is_active=1 ORDER BY arabic_name");
            CmbBranch.ItemsSource = branches.ToList();

            if (_user != null)
            {
                TxtTitle.Text       = "تعديل بيانات المستخدم";
                TxtFullName.Text    = (string)_user.full_name;
                TxtUsername.Text    = (string)_user.username;
                CmbRole.SelectedValue   = (int)_user.role_id;
                CmbBranch.SelectedValue = (int)_user.branch_id;
            }
            else
            {
                CmbRole.SelectedIndex   = 0;
                CmbBranch.SelectedIndex = 0;
            }
        };
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtFullName.Text) || string.IsNullOrWhiteSpace(TxtUsername.Text))
        { MessageBox.Show("يرجى ملء الحقول المطلوبة"); return; }

        var roleId   = (int)CmbRole.SelectedValue;
        var branchId = (int)CmbBranch.SelectedValue;
        var fullName = TxtFullName.Text.Trim();
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (_user == null)
        {
            if (string.IsNullOrEmpty(password)) { MessageBox.Show("أدخل كلمة المرور"); return; }
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            await _db.ExecuteAsync(
                "INSERT INTO users (full_name,username,password_hash,role_id,branch_id) VALUES (@fn,@un,@ph,@rid,@bid)",
                new { fn = fullName, un = username, ph = hash, rid = roleId, bid = branchId });
        }
        else
        {
            if (!string.IsNullOrEmpty(password))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(password);
                await _db.ExecuteAsync(
                    "UPDATE users SET full_name=@fn,username=@un,password_hash=@ph,role_id=@rid,branch_id=@bid,updated_at=GETDATE() WHERE user_id=@id",
                    new { fn = fullName, un = username, ph = hash, rid = roleId, bid = branchId, id = (int)_user.user_id });
            }
            else
            {
                await _db.ExecuteAsync(
                    "UPDATE users SET full_name=@fn,username=@un,role_id=@rid,branch_id=@bid,updated_at=GETDATE() WHERE user_id=@id",
                    new { fn = fullName, un = username, rid = roleId, bid = branchId, id = (int)_user.user_id });
            }
        }
        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
