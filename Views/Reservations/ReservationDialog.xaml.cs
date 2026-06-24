using RestaurantMS.Desktop.Data;
using System.Windows;

namespace RestaurantMS.Desktop.Views.Reservations;

public partial class ReservationDialog : Window
{
    private readonly DbHelper _db;

    public ReservationDialog(DbHelper db)
    {
        InitializeComponent();
        _db = db;
        DpDate.SelectedDate = DateTime.Today;
        Loaded += async (_, _) =>
        {
            var tables = await _db.QueryAsync<dynamic>(
                "SELECT table_id, table_number FROM tables WHERE is_active=1 ORDER BY table_number");
            CmbTable.ItemsSource = tables.Select(t => new
            {
                t.table_id, Display = $"طاولة {t.table_number}"
            }).ToList();
            CmbTable.SelectedIndex = 0;
        };
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text) || DpDate.SelectedDate == null)
        { MessageBox.Show("أدخل الاسم والتاريخ"); return; }

        var tableId  = CmbTable.SelectedValue as int?;
        var branchId = App.CurrentUser!.BranchId;
        await _db.ExecuteAsync(
            @"INSERT INTO reservations (customer_name,phone,reservation_date,reservation_time,
               party_size,table_id,special_requests,status,branch_id)
              VALUES (@cn,@ph,@dt,@tm,@ps,@tid,@notes,'Pending',@bid)",
            new
            {
                cn    = TxtName.Text.Trim(),
                ph    = TxtPhone.Text.Trim(),
                dt    = DpDate.SelectedDate!.Value,
                tm    = TxtTime.Text.Trim(),
                ps    = int.TryParse(TxtParty.Text, out var p) ? p : 2,
                tid   = (object?)tableId ?? DBNull.Value,
                notes = TxtNotes.Text,
                bid   = branchId
            });
        DialogResult = true;
    }
    private void BtnCancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
