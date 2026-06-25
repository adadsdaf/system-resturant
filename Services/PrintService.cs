using RestaurantMS.Desktop.Models;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RestaurantMS.Desktop.Services;

public static class PrintService
{
    private static readonly FontFamily ArabicFont = new("Segoe UI");

    // ========== فاتورة كبيرة A4/Letter ==========
    public static void PrintLargeReceipt(OrderDetailModel order, ReceiptSettings settings, string copyLabel)
    {
        var dlg = new PrintDialog();
        if (settings.ThermalPrinterName == "" || settings.ThermalPrinterName == "افتراضي")
        {
            // Use default printer without dialog
        }
        else
        {
            dlg.PrintQueue = FindPrinter(settings.ThermalPrinterName) ?? dlg.PrintQueue;
        }

        var doc = BuildLargeDocument(order, settings, copyLabel);
        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        try { dlg.PrintDocument(paginator, $"فاتورة #{order.OrderId} - {copyLabel}"); }
        catch (Exception ex) { MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ"); }
    }

    // ========== قصاصة 80mm ==========
    public static void PrintSmallSlip(OrderDetailModel order, ReceiptSettings settings,
                                       string groupName, List<OrderItemDetailModel> groupItems)
    {
        var dlg = new PrintDialog();
        try
        {
            // 80mm width = 302 device-independent pixels
            double width80mm  = 302;
            double heightFull = 800;

            var visual = BuildSmallSlipVisual(order, settings, groupName, groupItems, width80mm);
            var size   = new Size(width80mm, heightFull);
            visual.Measure(size);
            visual.Arrange(new Rect(size));
            visual.UpdateLayout();

            dlg.PrintVisual(visual, $"قصاصة - {groupName}");
        }
        catch (Exception ex) { MessageBox.Show($"خطأ في طباعة القصاصة: {ex.Message}", "خطأ"); }
    }

    // ========== معاينة الفاتورة الكبيرة (FlowDocument) ==========
    public static FlowDocument BuildLargeDocument(OrderDetailModel order, ReceiptSettings settings, string copyLabel)
    {
        var doc = new FlowDocument
        {
            FontFamily = ArabicFont,
            FontSize   = 13,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(30, 20, 30, 20),
            PageWidth   = 680,
            ColumnWidth = double.MaxValue
        };

        // ---- رأس الفاتورة ----
        var header = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin        = new Thickness(0, 0, 0, 10)
        };
        header.Inlines.Add(new Run(settings.RestaurantName)
            { FontSize = 22, FontWeight = FontWeights.Bold });
        header.Inlines.Add(new LineBreak());

        if (!string.IsNullOrWhiteSpace(settings.Address))
        {
            header.Inlines.Add(new Run(settings.Address) { FontSize = 11, Foreground = Brushes.Gray });
            header.Inlines.Add(new LineBreak());
        }
        if (!string.IsNullOrWhiteSpace(settings.Phone))
        {
            header.Inlines.Add(new Run($"📞 {settings.Phone}") { FontSize = 11 });
            header.Inlines.Add(new LineBreak());
        }
        if (!string.IsNullOrWhiteSpace(settings.TaxNumber))
        {
            header.Inlines.Add(new Run($"الرقم الضريبي: {settings.TaxNumber}") { FontSize = 11 });
            header.Inlines.Add(new LineBreak());
        }
        doc.Blocks.Add(header);

        // سطر فاصل
        doc.Blocks.Add(MakeSeparator());

        // ---- معلومات الفاتورة ----
        var infoTable = new Table { FontSize = 12 };
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var infoGroup = new TableRowGroup();

        if (settings.ShowOrderNumber)
            infoGroup.Rows.Add(MakeInfoRow("رقم الطلب", $"#{order.OrderId}"));
        if (settings.ShowDateTime)
            infoGroup.Rows.Add(MakeInfoRow("التاريخ والوقت", order.CreatedAt.ToString("yyyy/MM/dd hh:mm tt")));
        if (settings.ShowCashierName && !string.IsNullOrWhiteSpace(order.ServedBy))
            infoGroup.Rows.Add(MakeInfoRow("الكاشير", order.ServedBy));
        if (settings.ShowTableNumber && !string.IsNullOrWhiteSpace(order.TableNumber) && order.TableNumber != "بدون طاولة")
            infoGroup.Rows.Add(MakeInfoRow("الطاولة", order.TableNumber));
        infoGroup.Rows.Add(MakeInfoRow("العميل", order.CustomerName));
        infoGroup.Rows.Add(MakeInfoRow("طريقة الدفع", TranslatePayment(order.PaymentMethod)));

        infoTable.RowGroups.Add(infoGroup);
        doc.Blocks.Add(infoTable);
        doc.Blocks.Add(MakeSeparator());

        // ---- نوع النسخة ----
        var copyPara = new Paragraph(new Run($"◀ {copyLabel} ▶"))
        {
            TextAlignment = TextAlignment.Center,
            FontSize      = 15,
            FontWeight    = FontWeights.Bold,
            Foreground    = Brushes.DarkBlue,
            Margin        = new Thickness(0, 4, 0, 8)
        };
        doc.Blocks.Add(copyPara);

        // ---- جدول الأصناف ----
        var itemsTable = new Table { FontSize = 12, CellSpacing = 0 };
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(2.5, GridUnitType.Star) });
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(0.8, GridUnitType.Star) });
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });

        var headerGroup = new TableRowGroup();
        var hRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)) };
        hRow.Cells.Add(MakeCell("الصنف",    FontWeights.Bold, Brushes.White, TextAlignment.Right));
        hRow.Cells.Add(MakeCell("الكمية",   FontWeights.Bold, Brushes.White, TextAlignment.Center));
        hRow.Cells.Add(MakeCell("السعر",    FontWeights.Bold, Brushes.White, TextAlignment.Center));
        hRow.Cells.Add(MakeCell("الإجمالي", FontWeights.Bold, Brushes.White, TextAlignment.Center));
        headerGroup.Rows.Add(hRow);
        itemsTable.RowGroups.Add(headerGroup);

        var bodyGroup = new TableRowGroup();
        bool alt = false;
        foreach (var item in order.Items)
        {
            var row = new TableRow
            {
                Background = alt ? new SolidColorBrush(Color.FromRgb(241, 245, 249)) : Brushes.White
            };
            row.Cells.Add(MakeCell(item.ItemName,              FontWeights.Normal, Brushes.Black, TextAlignment.Right));
            row.Cells.Add(MakeCell(item.Quantity.ToString(),   FontWeights.Normal, Brushes.Black, TextAlignment.Center));
            row.Cells.Add(MakeCell($"{item.UnitPrice:N2}",     FontWeights.Normal, Brushes.Black, TextAlignment.Center));
            row.Cells.Add(MakeCell($"{item.Subtotal:N2}",      FontWeights.SemiBold, Brushes.Black, TextAlignment.Center));
            bodyGroup.Rows.Add(row);
            alt = !alt;
        }
        itemsTable.RowGroups.Add(bodyGroup);
        doc.Blocks.Add(itemsTable);
        doc.Blocks.Add(MakeSeparator());

        // ---- المجاميع ----
        var totals = new Table { FontSize = 13 };
        totals.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        totals.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var tg = new TableRowGroup();

        tg.Rows.Add(MakeTotalRow("المجموع الفرعي", $"{order.Subtotal:N2} {settings.Currency}", false));
        if (order.DiscountAmount > 0)
            tg.Rows.Add(MakeTotalRow("الخصم", $"-{order.DiscountAmount:N2} {settings.Currency}", false));
        if (settings.TaxEnabled && order.TaxAmount > 0)
            tg.Rows.Add(MakeTotalRow($"الضريبة ({settings.TaxRate:0}%)", $"{order.TaxAmount:N2} {settings.Currency}", false));

        var totalRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)) };
        totalRow.Cells.Add(new TableCell(new Paragraph(new Run("الإجمالي"))
            { TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 16 }));
        totalRow.Cells.Add(new TableCell(new Paragraph(new Run($"{order.TotalAmount:N2} {settings.Currency}"))
            { TextAlignment = TextAlignment.Left, FontWeight = FontWeights.Bold, Foreground = Brushes.Orange, FontSize = 16 }));
        tg.Rows.Add(totalRow);

        totals.RowGroups.Add(tg);
        doc.Blocks.Add(totals);
        doc.Blocks.Add(MakeSeparator());

        // ---- تذييل ----
        if (!string.IsNullOrWhiteSpace(settings.Footer))
        {
            var footer = new Paragraph(new Run(settings.Footer))
            {
                TextAlignment = TextAlignment.Center,
                FontSize      = 13,
                FontStyle     = FontStyles.Italic,
                Foreground    = Brushes.Gray,
                Margin        = new Thickness(0, 8, 0, 0)
            };
            doc.Blocks.Add(footer);
        }

        var powered = new Paragraph(new Run("RestaurantMS by itQAN Soft"))
        {
            TextAlignment = TextAlignment.Center, FontSize = 10, Foreground = Brushes.LightGray
        };
        doc.Blocks.Add(powered);

        return doc;
    }

    // ========== مرئيات قصاصة 80mm ==========
    public static FrameworkElement BuildSmallSlipVisual(OrderDetailModel order, ReceiptSettings settings,
                                                         string groupName, List<OrderItemDetailModel> items,
                                                         double width)
    {
        var sp = new StackPanel
        {
            Width           = width,
            Background      = Brushes.White,
            FlowDirection   = FlowDirection.RightToLeft
        };

        // رأس المطعم
        sp.Children.Add(new TextBlock
        {
            Text            = settings.RestaurantName,
            FontSize        = 16, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily      = ArabicFont, Margin = new Thickness(0, 6, 0, 2)
        });

        if (!string.IsNullOrWhiteSpace(settings.Phone))
            sp.Children.Add(new TextBlock
            {
                Text = settings.Phone, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = ArabicFont
            });

        sp.Children.Add(MakeLine(width));

        // معلومات الطلب
        sp.Children.Add(MakeSlipRow($"طلب #{order.OrderId}", order.CreatedAt.ToString("hh:mm tt"), width));
        if (!string.IsNullOrWhiteSpace(order.TableNumber) && order.TableNumber != "بدون طاولة")
            sp.Children.Add(MakeSlipRow("الطاولة:", order.TableNumber, width));

        sp.Children.Add(MakeLine(width));

        // القسم
        sp.Children.Add(new TextBlock
        {
            Text = $"◀ {groupName} ▶",
            FontSize = 14, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = ArabicFont, Margin = new Thickness(0, 4, 0, 4)
        });

        sp.Children.Add(MakeLine(width));

        // الأصناف
        foreach (var item in items)
        {
            var row = new Grid { Margin = new Thickness(4, 2, 4, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameBlock = new TextBlock
            {
                Text = item.ItemName, FontSize = 13, FontFamily = ArabicFont,
                TextWrapping = TextWrapping.Wrap
            };
            var qtyBlock = new TextBlock
            {
                Text = $"×{item.Quantity}", FontSize = 13, FontWeight = FontWeights.Bold,
                FontFamily = ArabicFont, Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
            };
            var priceBlock = new TextBlock
            {
                Text = $"{item.Subtotal:N2}", FontSize = 12, FontFamily = ArabicFont,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(nameBlock, 0);
            Grid.SetColumn(qtyBlock, 1);
            Grid.SetColumn(priceBlock, 2);
            row.Children.Add(nameBlock);
            row.Children.Add(qtyBlock);
            row.Children.Add(priceBlock);
            sp.Children.Add(row);
        }

        sp.Children.Add(MakeLine(width));

        // إجمالي هذا القسم
        var groupTotal = items.Sum(i => i.Subtotal);
        sp.Children.Add(MakeSlipRow("الإجمالي:", $"{groupTotal:N2} {settings.Currency}", width, true));

        sp.Children.Add(MakeLine(width));

        if (!string.IsNullOrWhiteSpace(settings.Footer))
            sp.Children.Add(new TextBlock
            {
                Text = settings.Footer, FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = ArabicFont, Margin = new Thickness(0, 4, 0, 6),
                FontStyle = FontStyles.Italic
            });

        return sp;
    }

    // ===== Helpers =====
    private static Block MakeSeparator() => new Paragraph(new Run(new string('─', 60)))
    {
        TextAlignment = TextAlignment.Center, Foreground = Brushes.Gray,
        FontSize = 10, Margin = new Thickness(0, 4, 0, 4)
    };

    private static TableRow MakeInfoRow(string label, string value)
    {
        var row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(label + ":"))
            { TextAlignment = TextAlignment.Right, Foreground = Brushes.Gray }));
        row.Cells.Add(new TableCell(new Paragraph(new Run(value))
            { TextAlignment = TextAlignment.Left, FontWeight = FontWeights.SemiBold }));
        return row;
    }

    private static TableCell MakeCell(string text, FontWeight weight, Brush fg, TextAlignment align) =>
        new(new Paragraph(new Run(text))
        {
            FontWeight    = weight,
            Foreground    = fg,
            TextAlignment = align,
            Margin        = new Thickness(4, 3, 4, 3)
        });

    private static TableRow MakeTotalRow(string label, string value, bool bold)
    {
        var row = new TableRow { Background = Brushes.White };
        row.Cells.Add(new TableCell(new Paragraph(new Run(label))
            { TextAlignment = TextAlignment.Right, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal }));
        row.Cells.Add(new TableCell(new Paragraph(new Run(value))
            { TextAlignment = TextAlignment.Left, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal }));
        return row;
    }

    private static UIElement MakeLine(double width) => new Border
    {
        Height = 1, Width = width, Background = new SolidColorBrush(Colors.LightGray),
        Margin = new Thickness(0, 3, 0, 3)
    };

    private static UIElement MakeSlipRow(string label, string value, double width, bool bold = false)
    {
        var g = new Grid { Margin = new Thickness(4, 1, 4, 1) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var l = new TextBlock { Text = label, FontSize = 12, FontFamily = ArabicFont, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal };
        var r = new TextBlock { Text = value, FontSize = 12, FontFamily = ArabicFont, TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal };

        Grid.SetColumn(l, 0);
        Grid.SetColumn(r, 1);
        g.Children.Add(l);
        g.Children.Add(r);
        return g;
    }

    private static string TranslatePayment(string method) => method switch
    {
        "Cash"     => "كاش",
        "Card"     => "بطاقة",
        "Transfer" => "تحويل",
        _          => method
    };

    private static PrintQueue? FindPrinter(string name)
    {
        try
        {
            using var ps = new PrintServer();
            return ps.GetPrintQueues().FirstOrDefault(q =>
                q.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }
}
