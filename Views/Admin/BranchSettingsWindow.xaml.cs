using RestaurantMS.Desktop.Data;
using System.Windows;
using System.Windows.Input;

namespace RestaurantMS.Desktop.Views.Admin;

public partial class BranchSettingsWindow : Window
{
    private readonly DbHelper _db = new(App.ConnectionString);
    private int _branchId = 0;

    public BranchSettingsWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadBranchAsync();
    }

    private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            try { DragMove(); } catch { }
    }

    private void BtnClose_Click(object s, RoutedEventArgs e) => Close();

    private async Task LoadBranchAsync()
    {
        try
        {
            var branch = await _db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT branch_id, arabic_name, english_name, address, address_en,
                         phone, fax, po_box, email, website,
                         financial_year, tax_name, tax_number, commercial_reg,
                         branch_code, manager_name
                  FROM branches WHERE is_active=1 ORDER BY branch_id");

            if (branch == null) return;

            _branchId = (int)branch.branch_id;
            TxtBranchSubtitle.Text = $"  —  {(string)(branch.arabic_name ?? "")}";

            TxtBranchCode.Text    = (string?)(branch.branch_code)    ?? "001";
            TxtFinancialYear.Text = (string?)(branch.financial_year) ?? DateTime.Now.Year.ToString();
            TxtArabicName.Text    = (string?)(branch.arabic_name)    ?? "";
            TxtEnglishName.Text   = (string?)(branch.english_name)   ?? "";
            TxtManagerName.Text   = (string?)(branch.manager_name)   ?? "";
            TxtAddress.Text       = (string?)(branch.address)        ?? "";
            TxtAddressEn.Text     = (string?)(branch.address_en)     ?? "";
            TxtPhone.Text         = (string?)(branch.phone)          ?? "";
            TxtFax.Text           = (string?)(branch.fax)            ?? "";
            TxtPoBox.Text         = (string?)(branch.po_box)         ?? "";
            TxtEmail.Text         = (string?)(branch.email)          ?? "";
            TxtWebsite.Text       = (string?)(branch.website)        ?? "";
            TxtTaxName.Text       = (string?)(branch.tax_name)       ?? "";
            TxtTaxNumber.Text     = (string?)(branch.tax_number)     ?? "";
            TxtCommercialReg.Text = (string?)(branch.commercial_reg) ?? "";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل بيانات الفرع:\n{ex.Message}");
        }
    }

    private async void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtArabicName.Text))
        {
            MessageBox.Show("يرجى إدخال اسم الفرع العربي.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_branchId == 0)
            {
                // إنشاء فرع جديد
                await _db.ExecuteAsync(@"
                    INSERT INTO branches
                        (arabic_name, english_name, address, address_en, phone, fax,
                         po_box, email, website, financial_year, tax_name, tax_number,
                         commercial_reg, branch_code, manager_name, is_active)
                    VALUES
                        (@an, @en, @ad, @aden, @ph, @fx, @pb, @em, @wb, @fy,
                         @tn, @tnum, @cr, @bc, @mn, 1)",
                    BuildParams());
            }
            else
            {
                var p = BuildParams();
                await _db.ExecuteAsync(@"
                    UPDATE branches SET
                        arabic_name=@an, english_name=@en, address=@ad, address_en=@aden,
                        phone=@ph, fax=@fx, po_box=@pb, email=@em, website=@wb,
                        financial_year=@fy, tax_name=@tn, tax_number=@tnum,
                        commercial_reg=@cr, branch_code=@bc, manager_name=@mn
                    WHERE branch_id=@bid",
                    new
                    {
                        bid  = _branchId,
                        p.an, p.en, p.ad, p.aden, p.ph, p.fx,
                        p.pb, p.em, p.wb, p.fy, p.tn, p.tnum,
                        p.cr, p.bc, p.mn
                    });
            }

            // تحديث اسم المطعم في الإعدادات
            await _db.ExecuteAsync(
                "UPDATE settings SET value=@v WHERE setting_key='restaurant_name'",
                new { v = TxtArabicName.Text.Trim() });

            MessageBox.Show("✅ تم حفظ بيانات الفرع بنجاح!", "حفظ",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private dynamic BuildParams() => new
    {
        an   = TxtArabicName.Text.Trim(),
        en   = TxtEnglishName.Text.Trim(),
        ad   = TxtAddress.Text.Trim(),
        aden = TxtAddressEn.Text.Trim(),
        ph   = TxtPhone.Text.Trim(),
        fx   = TxtFax.Text.Trim(),
        pb   = TxtPoBox.Text.Trim(),
        em   = TxtEmail.Text.Trim(),
        wb   = TxtWebsite.Text.Trim(),
        fy   = TxtFinancialYear.Text.Trim(),
        tn   = TxtTaxName.Text.Trim(),
        tnum = TxtTaxNumber.Text.Trim(),
        cr   = TxtCommercialReg.Text.Trim(),
        bc   = TxtBranchCode.Text.Trim(),
        mn   = TxtManagerName.Text.Trim()
    };
}
