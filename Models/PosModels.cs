namespace RestaurantMS.Desktop.Models;

public class CartItemModel
{
    public int     ItemId     { get; set; }
    public string  Name       { get; set; } = "";
    public decimal Price      { get; set; }
    public int     Quantity   { get; set; }
    public int     CategoryId { get; set; }
    public string  CategoryName { get; set; } = "";
    public string  Notes      { get; set; } = "";
    public decimal LineTotal  => Price * Quantity;
}

public class OrderDetailModel
{
    public int     OrderId       { get; set; }
    public string  CustomerName  { get; set; } = "";
    public string  PaymentMethod { get; set; } = "";
    public decimal Subtotal      { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount     { get; set; }
    public decimal TotalAmount   { get; set; }
    public string  OrderStatus   { get; set; } = "";
    public DateTime CreatedAt    { get; set; }
    public string  ServedBy      { get; set; } = "";
    public string  TableNumber   { get; set; } = "";
    public List<OrderItemDetailModel> Items { get; set; } = new();
}

public class OrderItemDetailModel
{
    public string  ItemName   { get; set; } = "";
    public int     Quantity   { get; set; }
    public decimal UnitPrice  { get; set; }
    public decimal Subtotal   { get; set; }
    public string  CategoryName { get; set; } = "";
}

public class ReceiptSettings
{
    public string RestaurantName     { get; set; } = "مطعم الإتقان";
    public string Phone              { get; set; } = "";
    public string Address            { get; set; } = "";
    public string TaxNumber          { get; set; } = "";
    public string Footer             { get; set; } = "شكراً لزيارتكم";
    public string Currency           { get; set; } = "ريال";
    public bool   ShowLogo           { get; set; } = true;
    public bool   PrintLargeCustomer { get; set; } = true;
    public bool   PrintLargeStaff    { get; set; } = true;
    public bool   PrintSmallSlips    { get; set; } = true;
    public string SlipSplitBy        { get; set; } = "category";
    public string ThermalPrinterName { get; set; } = "";
    public bool   TaxEnabled         { get; set; } = false;
    public double TaxRate            { get; set; } = 15;
    public bool   ShowOrderNumber    { get; set; } = true;
    public bool   ShowDateTime       { get; set; } = true;
    public bool   ShowCashierName    { get; set; } = true;
    public bool   ShowTableNumber    { get; set; } = true;
}