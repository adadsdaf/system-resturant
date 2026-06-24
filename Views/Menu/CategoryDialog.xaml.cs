using System.Windows;
using System.Windows.Input;

namespace RestaurantMS.Desktop.Views.Menu;

public partial class CategoryDialog : Window
{
    public string CategoryName { get; private set; } = "";

    public CategoryDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => TxtName.Focus();
    }

    private void BtnAdd_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("أدخل اسم التصنيف", "تنبيه");
            return;
        }
        CategoryName = TxtName.Text.Trim();
        DialogResult = true;
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

    private void TxtName_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnAdd_Click(s, e);
    }
}
