using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Services;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Pos;

public partial class LastInvoiceDialog : Window
{
    private readonly OrderDetailModel   _order;
    private readonly ReceiptSettings    _settings;

    public LastInvoiceDialog(OrderDetailModel order, ReceiptSettings settings)
    {
        InitializeComponent();
        _order    = order;
        _settings = settings;
        LoadData();
    }

    private void LoadData()
    {
        TxtOrderId.Text    = $"#{_order.OrderId}";
        TxtCustomer.Text   = _order.CustomerName;
        TxtPayMethod.Text  = TranslatePayment(_order.PaymentMethod);
        TxtDate.Text       = _order.CreatedAt.ToString("yyyy/MM/dd\nhh:mm tt");
        TxtOrderMeta.Text  = $"تاريخ الفاتورة: {_order.CreatedAt:yyyy/MM/dd hh:mm tt}  |  الكاشير: {_order.ServedBy}";

        TxtSubtotal.Text = $"{_order.Subtotal:N2} {_settings.Currency}";
        TxtDiscount.Text = $"-{_order.DiscountAmount:N2} {_settings.Currency}";
        TxtTax.Text      = $"{_order.TaxAmount:N2} {_settings.Currency}";
        TxtTotal.Text    = $"{_order.TotalAmount:N2} {_settings.Currency}";

        GridItems.ItemsSource = _order.Items;
    }

    private void BtnPrintCustomer_Click(object s, RoutedEventArgs e)
    {
        var win = new PrintReceiptWindow(_order, _settings, "نسخة العميل");
        win.Owner = this;
        win.ShowDialog();
    }

    private void BtnPrintStaff_Click(object s, RoutedEventArgs e)
    {
        var win = new PrintReceiptWindow(_order, _settings, "نسخة المطعم");
        win.Owner = this;
        win.ShowDialog();
    }

    private void BtnPrintSlips_Click(object s, RoutedEventArgs e)
    {
        var groups = _order.Items
            .GroupBy(i => i.CategoryName)
            .ToList();

        if (!groups.Any())
        {
            MessageBox.Show("لا توجد أصناف في هذا الطلب.", "تنبيه");
            return;
        }

        var result = MessageBox.Show(
            $"سيتم طباعة {groups.Count} قصاصة (قصاصة لكل قسم).\nهل تريد المتابعة؟",
            "تأكيد الطباعة", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        foreach (var group in groups)
        {
            PrintService.PrintSmallSlip(_order, _settings, group.Key, group.ToList());
        }
    }

    private void BtnClose_Click(object s, RoutedEventArgs e) => Close();

    private static string TranslatePayment(string method) => method switch
    {
        "Cash"     => "💵 كاش",
        "Card"     => "💳 بطاقة",
        "Transfer" => "📱 تحويل",
        _          => method
    };
}
