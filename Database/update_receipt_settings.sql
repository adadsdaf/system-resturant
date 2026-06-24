-- ================================================
--  RestaurantMS — تحديث إعدادات الفاتورة
--  قم بتشغيل هذا الملف بعد setup_sqlserver.sql
-- ================================================

USE RestaurantMS;
GO

-- إضافة إعدادات الفاتورة الجديدة
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_restaurant_name')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_restaurant_name', N'مطعم الإتقان',    N'اسم المطعم على الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_phone')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_phone',           N'0501234567',       N'رقم الهاتف على الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_address')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_address',         N'',                 N'العنوان على الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_tax_number')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_tax_number',      N'',                 N'الرقم الضريبي');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_footer')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_footer',          N'شكراً لزيارتكم — نتطلع لخدمتكم مجدداً', N'نص أسفل الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_show_logo')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_show_logo',       '1',                 N'إظهار الشعار على الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_large_customer')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_large_customer',  '1',                 N'طباعة فاتورة كبيرة للعميل');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_large_staff')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_large_staff',     '1',                 N'طباعة فاتورة كبيرة للعامل');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_small_slips')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_small_slips',     '1',                 N'طباعة قصاصات 80mm للمطبخ');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_slip_by')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_slip_by',         'category',          N'تقسيم القصاصات: category أو group');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_printer_name')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_printer_name',    N'',                 N'اسم الطابعة الحرارية 80mm');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_show_order_number')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_show_order_number','1',                N'إظهار رقم الطلب على الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_show_datetime')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_show_datetime',   '1',                 N'إظهار التاريخ والوقت على الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_show_cashier')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_show_cashier',    '1',                 N'إظهار اسم الكاشير على الفاتورة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='receipt_show_table')
    INSERT INTO settings (setting_key, value, description) VALUES
        ('receipt_show_table',      '1',                 N'إظهار رقم الطاولة على الفاتورة');
GO

PRINT N'✅ تم تحديث إعدادات الفاتورة بنجاح!';
GO
