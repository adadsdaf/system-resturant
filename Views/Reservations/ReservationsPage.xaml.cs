using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantMS.Desktop.Views.Reservations;

public partial class ReservationsPage : Page
{
    private readonly DbHelper _db = new(App.ConnectionString);

    public ReservationsPage()
    {
        InitializeComponent();
        DpFilter.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var date = DpFilter.SelectedDate ?? DateTime.Today;
            var rows = await _db.QueryAsync<dynamic>(
                @"SELECT r.reservation_id, r.customer_name, r.phone, r.reservation_date,
                         r.reservation_time, r.party_size, r.table_id, r.status, r.special_requests,
                         t.table_number
                  FROM reservations r
                  LEFT JOIN tables t ON r.table_id = t.table_id
                  WHERE r.reservation_date = @date
                  ORDER BY r.reservation_time",
                new { date });

            GridRes.ItemsSource = rows.Select(r => new
            {
                r.reservation_id, r.customer_name, r.phone, r.party_size, r.status,
                date_fmt   = ((DateTime)r.reservation_date).ToString("dd/MM/yyyy"),
                time_fmt   = r.reservation_time?.ToString() ?? "",
                table_num  = r.table_number != null ? $"طاولة {r.table_number}" : "—",
                status_txt = ((string)r.status) switch
                {
                    "Pending"    => "⏳ انتظار",
                    "Confirmed"  => "✅ مؤكد",
                    "Cancelled"  => "❌ ملغي",
                    "No Show"    => "🚫 لم يحضر",
                    "Completed"  => "✔️ مكتمل",
                    _            => r.status
                }
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الحجوزات:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnAdd_Click(object s, RoutedEventArgs e)
    {
        var dlg = new ReservationDialog(_db);
        if (dlg.ShowDialog() == true) _ = LoadAsync();
    }

    private async void BtnConfirm_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        await _db.ExecuteAsync("UPDATE reservations SET status='Confirmed' WHERE reservation_id=@id", new { id });
        await LoadAsync();
    }

    private async void BtnCancel_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        if (MessageBox.Show("إلغاء هذا الحجز؟", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _db.ExecuteAsync("UPDATE reservations SET status='Cancelled' WHERE reservation_id=@id", new { id });
        await LoadAsync();
    }

    private async void BtnNoShow_Click(object s, RoutedEventArgs e)
    {
        if (((Button)s).Tag is not int id) return;
        await _db.ExecuteAsync("UPDATE reservations SET status='No Show' WHERE reservation_id=@id", new { id });
        await LoadAsync();
    }

    private async void DpFilter_Changed(object s, SelectionChangedEventArgs e) => await LoadAsync();
    private async void BtnRefresh_Click(object s, RoutedEventArgs e) => await LoadAsync();
}
