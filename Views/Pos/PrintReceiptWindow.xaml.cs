using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Services;
using System.Windows;
using System.Windows.Documents;

namespace RestaurantMS.Desktop.Views.Pos;

public partial class PrintReceiptWindow : Window
{
    private readonly OrderDetailModel _order;
    private readonly ReceiptSettings  _settings;
    private readonly string           _copyLabel;
    private FlowDocument?             _document;

    public PrintReceiptWindow(OrderDetailModel order, ReceiptSettings settings, string copyLabel)
    {
        InitializeComponent();
        _order     = order;
        _settings  = settings;
        _copyLabel = copyLabel;
        TxtTitle.Text = $"معاينة الفاتورة — {copyLabel}";
        LoadDocument();
    }

    private void LoadDocument()
    {
        try
        {
            _document = PrintService.BuildLargeDocument(_order, _settings, _copyLabel);
            DocViewer.Document = _document;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنشاء معاينة الفاتورة: {ex.Message}", "خطأ");
        }
    }

    private void BtnPrint_Click(object s, RoutedEventArgs e)
    {
        try
        {
            PrintService.PrintLargeReceipt(_order, _settings, _copyLabel);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ");
        }
    }

    private void BtnClose_Click(object s, RoutedEventArgs e) => Close();
}
