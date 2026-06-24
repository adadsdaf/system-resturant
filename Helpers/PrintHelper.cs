using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace RestaurantMS.Desktop.Helpers;

public static class PrintHelper
{
    public static void PrintReceipt(ReceiptData data)
    {
        var doc = new FlowDocument
        {
            PageWidth = 300,
            PagePadding = new Thickness(10),
            ColumnWidth = 300,
            FontFamily = new FontFamily("Tahoma"),
            FlowDirection = FlowDirection.RightToLeft
        };

        // ترويسة
        var header = new Paragraph(new Run(data.RestaurantName))
        {
            TextAlignment = TextAlignment.Center,
            FontSize = 18, FontWeight = FontWeights.Bold
        };
        doc.Blocks.Add(header);

        if (!string.IsNullOrEmpty(data.BranchName))
            doc.Blocks.Add(new Paragraph(new Run(data.BranchName)) { TextAlignment = TextAlignment.Center, FontSize = 12 });

        doc.Blocks.Add(new Paragraph(new Run("─────────────────────────")) { TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run($"رقم الطلب: #{data.OrderId}")) { FontSize = 13 });
        doc.Blocks.Add(new Paragraph(new Run($"التاريخ: {data.DateTime:dd/MM/yyyy HH:mm}")) { FontSize = 12 });
        doc.Blocks.Add(new Paragraph(new Run($"العميل: {data.CustomerName}")) { FontSize = 12 });
        doc.Blocks.Add(new Paragraph(new Run($"الكاشير: {data.CashierName}")) { FontSize = 12 });
        doc.Blocks.Add(new Paragraph(new Run("─────────────────────────")) { TextAlignment = TextAlignment.Center });

        // عناصر الطلب
        foreach (var item in data.Items)
        {
            var p = new Paragraph { FontSize = 12 };
            p.Inlines.Add(new Run($"{item.Name}\n"));
            p.Inlines.Add(new Run($"  {item.Quantity} × {item.Price:N2} = {item.Subtotal:N2}") { Foreground = Brushes.Gray });
            doc.Blocks.Add(p);
        }

        doc.Blocks.Add(new Paragraph(new Run("─────────────────────────")) { TextAlignment = TextAlignment.Center });

        // الإجماليات
        doc.Blocks.Add(new Paragraph(new Run($"المجموع الفرعي: {data.Subtotal:N2}")) { FontSize = 13 });
        if (data.Discount > 0)
            doc.Blocks.Add(new Paragraph(new Run($"الخصم: -{data.Discount:N2}")) { FontSize = 13 });
        if (data.Tax > 0)
            doc.Blocks.Add(new Paragraph(new Run($"الضريبة ({data.TaxRate}%): {data.Tax:N2}")) { FontSize = 13 });
        doc.Blocks.Add(new Paragraph(new Run($"الإجمالي: {data.Total:N2} {data.Currency}"))
        {
            FontSize = 16, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center
        });
        doc.Blocks.Add(new Paragraph(new Run($"طريقة الدفع: {data.PaymentMethod}")) { FontSize = 12 });
        doc.Blocks.Add(new Paragraph(new Run("─────────────────────────")) { TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run("شكراً لزيارتكم 🍽️")) { TextAlignment = TextAlignment.Center, FontSize = 13 });

        // طباعة
        var pd = new PrintDialog();
        if (pd.ShowDialog() == true)
        {
            var dv = new DocumentViewer();
            dv.Document = ((IDocumentPaginatorSource)doc).DocumentPaginator.Source as FlowDocument;
            pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"إيصال #{data.OrderId}");
        }
    }

    public static void PrintKitchenTicket(KitchenTicketData data)
    {
        var doc = new FlowDocument
        {
            PageWidth = 250, PagePadding = new Thickness(10),
            ColumnWidth = 250, FontFamily = new FontFamily("Tahoma"),
            FlowDirection = FlowDirection.RightToLeft
        };

        doc.Blocks.Add(new Paragraph(new Run("🍳  تذكرة المطبخ"))
        { TextAlignment = TextAlignment.Center, FontSize = 16, FontWeight = FontWeights.Bold });
        doc.Blocks.Add(new Paragraph(new Run($"طلب #{data.OrderId}  |  طاولة {data.TableNumber}"))
        { TextAlignment = TextAlignment.Center, FontSize = 14 });
        doc.Blocks.Add(new Paragraph(new Run(data.DateTime.ToString("HH:mm")))
        { TextAlignment = TextAlignment.Center, FontSize = 18, FontWeight = FontWeights.Bold });
        doc.Blocks.Add(new Paragraph(new Run("─────────────────")));

        foreach (var item in data.Items)
            doc.Blocks.Add(new Paragraph(new Run($"× {item.Quantity}  {item.Name}"))
            { FontSize = 15, FontWeight = FontWeights.Bold });

        if (!string.IsNullOrEmpty(data.Notes))
            doc.Blocks.Add(new Paragraph(new Run($"⚠️ {data.Notes}")) { FontSize = 13 });

        var pd = new PrintDialog();
        if (pd.ShowDialog() == true)
            pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"تذكرة مطبخ #{data.OrderId}");
    }
}

public class ReceiptData
{
    public int    OrderId        { get; set; }
    public string RestaurantName { get; set; } = "";
    public string BranchName     { get; set; } = "";
    public string CustomerName   { get; set; } = "";
    public string CashierName    { get; set; } = "";
    public DateTime DateTime     { get; set; }
    public List<ReceiptItem> Items { get; set; } = new();
    public decimal Subtotal     { get; set; }
    public decimal Discount     { get; set; }
    public decimal Tax          { get; set; }
    public decimal Total        { get; set; }
    public double  TaxRate      { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string Currency      { get; set; } = "ريال";
}

public class ReceiptItem
{
    public string  Name     { get; set; } = "";
    public int     Quantity { get; set; }
    public decimal Price    { get; set; }
    public decimal Subtotal { get; set; }
}

public class KitchenTicketData
{
    public int    OrderId     { get; set; }
    public string TableNumber { get; set; } = "";
    public DateTime DateTime  { get; set; }
    public string Notes       { get; set; } = "";
    public List<ReceiptItem> Items { get; set; } = new();
}
