using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Customers;

public partial class CustomerDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _customer;

    public CustomerDialog(DbHelper db, dynamic? customer)
    {
        InitializeComponent();
        _db       = db;
        _customer = customer;
        if (customer != null)
        {
            TxtTitle.Text  = "تعديل بيانات العميل";
            TxtName.Text   = (string)customer.full_name;
            TxtPhone.Text  = (string)customer.phone;
            TxtEmail.Text  = (string)(customer.email ?? "");
        }
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text) || string.IsNullOrWhiteSpace(TxtPhone.Text))
        { MessageBox.Show("يرجى ملء الاسم والهاتف"); return; }

        var name  = TxtName.Text.Trim();
        var phone = TxtPhone.Text.Trim();
        var email = TxtEmail.Text.Trim();

        if (_customer == null)
        {
            var cid = await _db.ExecuteScalarAsync<int>(
                "INSERT INTO customers (full_name,phone,email) OUTPUT INSERTED.customer_id VALUES (@n,@p,@e)",
                new { n = name, p = phone, e = string.IsNullOrEmpty(email) ? (object)DBNull.Value : email });
            await _db.ExecuteAsync(
                "INSERT INTO loyalty_accounts (customer_id,points_balance) VALUES (@cid,0)", new { cid });
        }
        else
        {
            await _db.ExecuteAsync(
                "UPDATE customers SET full_name=@n,phone=@p,email=@e,updated_at=GETDATE() WHERE customer_id=@id",
                new { n = name, p = phone, e = string.IsNullOrEmpty(email) ? (object)DBNull.Value : email, id = (int)_customer.customer_id });
        }
        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
