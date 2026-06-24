using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Suppliers;

public partial class SupplierDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _supplier;

    public SupplierDialog(DbHelper db, dynamic? supplier)
    {
        InitializeComponent();
        _db       = db;
        _supplier = supplier;
        if (supplier != null)
        {
            TxtTitle.Text   = "تعديل بيانات المورد";
            TxtCompany.Text = (string)supplier.company_name;
            TxtContact.Text = (string)(supplier.contact_name ?? "");
            TxtPhone.Text   = (string)(supplier.phone ?? "");
            TxtEmail.Text   = (string)(supplier.email ?? "");
        }
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCompany.Text)) { MessageBox.Show("أدخل اسم الشركة"); return; }
        if (_supplier == null)
            await _db.ExecuteAsync(
                "INSERT INTO suppliers (company_name,contact_name,phone,email) VALUES (@c,@cn,@p,@e)",
                new { c = TxtCompany.Text.Trim(), cn = TxtContact.Text.Trim(), p = TxtPhone.Text.Trim(), e = TxtEmail.Text.Trim() });
        else
            await _db.ExecuteAsync(
                "UPDATE suppliers SET company_name=@c,contact_name=@cn,phone=@p,email=@e WHERE supplier_id=@id",
                new { c = TxtCompany.Text.Trim(), cn = TxtContact.Text.Trim(), p = TxtPhone.Text.Trim(), e = TxtEmail.Text.Trim(), id = (int)_supplier.supplier_id });
        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
