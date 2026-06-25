using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Input;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class CurrencyDialog : Window
{
    private readonly DbHelper _db;
    private readonly dynamic? _existing;
    public bool Saved { get; private set; }

    public CurrencyDialog(DbHelper db, dynamic? existing = null)
    {
        InitializeComponent();
        _db       = db;
        _existing = existing;

        if (existing != null)
        {
            TxtTitle.Text   = "💱  تعديل عملة";
            TxtName.Text    = (string?)existing.currency_name ?? "";
            TxtCode.Text    = (string?)existing.currency_code ?? "";
            TxtSymbol.Text  = (string?)existing.currency_symbol ?? "";
            TxtRate.Text    = ((decimal)existing.exchange_rate).ToString("F6");
            ChkIsLocal.IsChecked = (bool)existing.is_local;
        }
    }

    private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            try { DragMove(); } catch { }
    }

    private void BtnClose_Click(object s, RoutedEventArgs e) => Close();

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        var name   = TxtName.Text.Trim();
        var code   = TxtCode.Text.Trim().ToUpperInvariant();
        var symbol = TxtSymbol.Text.Trim();
        var isLocal = ChkIsLocal.IsChecked == true;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
        {
            MessageBox.Show("يرجى إدخال اسم ورمز العملة.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtRate.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rate))
        {
            MessageBox.Show("سعر الصرف غير صحيح.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (isLocal)
                await _db.ExecuteAsync(
                    "UPDATE currencies SET is_local=0", null);

            if (_existing == null)
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO currencies (currency_name, currency_code, currency_symbol, is_local, exchange_rate)
                      VALUES (@name, @code, @symbol, @isLocal, @rate)",
                    new { name, code, symbol, isLocal, rate });
            }
            else
            {
                int id = (int)_existing.currency_id;
                await _db.ExecuteAsync(
                    @"UPDATE currencies SET currency_name=@name, currency_code=@code,
                      currency_symbol=@symbol, is_local=@isLocal, exchange_rate=@rate
                      WHERE currency_id=@id",
                    new { name, code, symbol, isLocal, rate, id });
            }

            if (isLocal)
                await _db.ExecuteAsync(
                    "UPDATE settings SET value=@code WHERE setting_key='default_currency'",
                    new { code });

            Saved = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
