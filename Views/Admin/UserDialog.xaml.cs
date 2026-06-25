using RestaurantMS.Desktop.Data;
using RestaurantMS.Desktop.Models;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class UserDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _user;
    private readonly CurrentUser? _currentUser;

    public UserDialog(DbHelper db, dynamic? user)
    {
        InitializeComponent();
        _db          = db;
        _user        = user;
        _currentUser = App.CurrentUser;

        Loaded += async (_, _) =>
        {
            // تصفية الأدوار بحسب صلاحية المُنشِئ
            // المالك/المسؤول: كل الأدوار | المدير: Cashier و Kitchen و Waiter فقط
            string roleFilter = _currentUser?.IsStrictManager == true
                ? "WHERE role_name IN (N'Cashier', N'Kitchen', N'Waiter')"
                : _currentUser?.IsAdmin == true
                    ? "WHERE role_name != N'Owner'"   // المسؤول لا ينشئ مالكاً آخر
                    : "WHERE role_name IN (N'Cashier', N'Kitchen', N'Waiter')";

            if (_currentUser?.IsOwner == true)
                roleFilter = "";  // المالك يرى كل الأدوار

            var roles = await _db.QueryAsync<dynamic>(
                $"SELECT role_id, role_name FROM roles {roleFilter} ORDER BY role_id");
            CmbRole.ItemsSource = roles.ToList();

            var branches = await _db.QueryAsync<dynamic>(
                "SELECT branch_id, arabic_name FROM branches WHERE is_active=1 ORDER BY arabic_name");
            CmbBranch.ItemsSource = branches.ToList();

            if (_user != null)
            {
                TxtTitle.Text           = "تعديل بيانات المستخدم";
                TxtFullName.Text        = (string)_user.full_name;
                TxtUsername.Text        = (string)_user.username;
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

        if (CmbRole.SelectedValue == null)
        { MessageBox.Show("يرجى اختيار دور للمستخدم"); return; }

        if (CmbBranch.SelectedValue == null)
        { MessageBox.Show("يرجى اختيار الفرع"); return; }

        var roleId   = (int)CmbRole.SelectedValue;
        var branchId = (int)CmbBranch.SelectedValue;
        var fullName = TxtFullName.Text.Trim();
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        try
        {
            if (_user == null)
            {
                if (string.IsNullOrEmpty(password)) { MessageBox.Show("أدخل كلمة المرور"); return; }
                var hash = BCrypt.Net.BCrypt.HashPassword(password);

                // تحقق من عدم تكرار اسم المستخدم
                var exists = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM users WHERE username=@un", new { un = username });
                if (exists > 0)
                {
                    MessageBox.Show("اسم المستخدم مستخدم بالفعل، اختر اسماً آخر.",
                        "تكرار", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تسجيل created_by لتتبع منشئ الحساب
                int? createdBy = _currentUser?.UserId;

                // التحقق من وجود عمود created_by قبل استخدامه
                var colExists = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('users') AND name='created_by'");

                if (colExists > 0)
                    await _db.ExecuteAsync(
                        "INSERT INTO users (full_name,username,password_hash,role_id,branch_id,created_by) VALUES (@fn,@un,@ph,@rid,@bid,@cb)",
                        new { fn = fullName, un = username, ph = hash, rid = roleId, bid = branchId, cb = createdBy });
                else
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
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ بيانات المستخدم:\n{ex.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
