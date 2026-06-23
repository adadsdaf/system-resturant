using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantMS.Data;
using System.Security.Claims;

namespace RestaurantMS.Controllers;

[Authorize]
public class SalesController : Controller
{
    private readonly DbHelper _db;

    public SalesController(DbHelper db) => _db = db;

    [HttpGet("/sales/invoices")]
    public async Task<IActionResult> Invoices()
    {
        var invoices = await _db.QueryAsync<dynamic>(
            @"SELECT si.invoice_id, si.invoice_number, si.customer_name, si.total_amount,
                     si.payment_method, si.status, si.created_at, u.full_name as created_by_name
              FROM sales_invoices si
              LEFT JOIN users u ON si.created_by=u.user_id
              ORDER BY si.created_at DESC LIMIT 100");
        ViewBag.Invoices = invoices;
        return View();
    }

    [HttpPost("/sales/invoices/create")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest data)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var num = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM sales_invoices");
        var invoiceNumber = $"INV-{(num + 1):D5}";

        var invoiceId = await _db.ExecuteScalarAsync<int>(
            @"INSERT INTO sales_invoices (invoice_number, customer_name, customer_id, order_id, subtotal,
              discount_amount, tax_amount, total_amount, payment_method, status, notes, created_by)
              VALUES (@num, @cn, @cid, @oid, @sub, @disc, @tax, @total, @pm, 'Active', @notes, @uid) RETURNING invoice_id",
            new { num = invoiceNumber, cn = data.CustomerName ?? "", cid = data.CustomerId, oid = data.OrderId,
                  sub = data.Subtotal, disc = data.DiscountAmount, tax = data.TaxAmount, total = data.TotalAmount,
                  pm = data.PaymentMethod ?? "Cash", notes = data.Notes ?? "", uid = userId });

        return Json(new { success = true, invoice_id = invoiceId, invoice_number = invoiceNumber });
    }

    [HttpGet("/sales/invoices/{id}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var invoice = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM sales_invoices WHERE invoice_id=@id", new { id });
        if (invoice == null) return NotFound();
        return Json(invoice);
    }

    [HttpPost("/sales/invoices/delete")]
    public async Task<IActionResult> DeleteInvoice(int invoice_id)
    {
        await _db.ExecuteAsync("UPDATE sales_invoices SET status='Cancelled' WHERE invoice_id=@id", new { id = invoice_id });
        return Json(new { success = true });
    }

    [HttpGet("/sales/returns")]
    public async Task<IActionResult> Returns()
    {
        var returns = await _db.QueryAsync<dynamic>(
            @"SELECT sr.return_id, sr.return_number, sr.customer_name, sr.return_amount,
                     sr.reason, sr.status, sr.created_at, u.full_name as created_by_name,
                     si.invoice_number
              FROM sales_returns sr
              LEFT JOIN users u ON sr.created_by=u.user_id
              LEFT JOIN sales_invoices si ON sr.invoice_id=si.invoice_id
              ORDER BY sr.created_at DESC");
        var invoices = await _db.QueryAsync<dynamic>(
            "SELECT invoice_id, invoice_number, customer_name, total_amount FROM sales_invoices WHERE status='Active' ORDER BY created_at DESC LIMIT 50");
        ViewBag.Returns = returns;
        ViewBag.Invoices = invoices;
        return View();
    }

    [HttpPost("/sales/returns/create")]
    public async Task<IActionResult> CreateReturn([FromBody] CreateReturnRequest data)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var num = await _db.QueryScalarAsync<int>("SELECT COUNT(*) FROM sales_returns");
        var returnNumber = $"RET-{(num + 1):D5}";

        var returnId = await _db.ExecuteScalarAsync<int>(
            @"INSERT INTO sales_returns (return_number, invoice_id, customer_name, return_amount, reason, status, created_by)
              VALUES (@num, @iid, @cn, @amt, @reason, 'Pending', @uid) RETURNING return_id",
            new { num = returnNumber, iid = data.InvoiceId, cn = data.CustomerName ?? "", amt = data.ReturnAmount, reason = data.Reason ?? "", uid = userId });

        return Json(new { success = true, return_id = returnId, return_number = returnNumber });
    }

    [HttpPost("/sales/returns/approve")]
    public async Task<IActionResult> ApproveReturn(int return_id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        await _db.ExecuteAsync(
            "UPDATE sales_returns SET status='Approved', approved_by=@uid WHERE return_id=@id",
            new { uid = userId, id = return_id });
        return Json(new { success = true });
    }

    [HttpPost("/sales/returns/delete")]
    public async Task<IActionResult> DeleteReturn(int return_id)
    {
        await _db.ExecuteAsync("DELETE FROM sales_returns WHERE return_id=@id AND status='Pending'", new { id = return_id });
        return Json(new { success = true });
    }

    [HttpGet("/sales/sessions")]
    public async Task<IActionResult> UserSessions()
    {
        var sessions = await _db.QueryAsync<dynamic>(
            @"SELECT us.session_id, us.username, u.full_name, us.login_time, us.logout_time,
                     us.ip_address, us.session_status
              FROM user_sessions us
              LEFT JOIN users u ON us.user_id=u.user_id
              ORDER BY us.login_time DESC LIMIT 100");
        ViewBag.Sessions = sessions;
        return View();
    }
}

public class CreateInvoiceRequest
{
    public string? CustomerName { get; set; }
    public int? CustomerId { get; set; }
    public int? OrderId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class CreateReturnRequest
{
    public int? InvoiceId { get; set; }
    public string? CustomerName { get; set; }
    public decimal ReturnAmount { get; set; }
    public string? Reason { get; set; }
}
