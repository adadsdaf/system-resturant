-- ================================================
--  RestaurantMS V3 — بيانات موسعة حسب واجهة itQAN ERP
--  قم بتشغيله بعد update_v2.sql
-- ================================================

USE RestaurantMS;
GO

-- ===== تعزيز جدول الفروع بحقول إضافية =====
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='english_name')
    ALTER TABLE branches ADD english_name NVARCHAR(100);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='address_en')
    ALTER TABLE branches ADD address_en NVARCHAR(300);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='fax')
    ALTER TABLE branches ADD fax NVARCHAR(20);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='po_box')
    ALTER TABLE branches ADD po_box NVARCHAR(20);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='email')
    ALTER TABLE branches ADD email NVARCHAR(150);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='website')
    ALTER TABLE branches ADD website NVARCHAR(200);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='financial_year')
    ALTER TABLE branches ADD financial_year NVARCHAR(4) DEFAULT CAST(YEAR(GETDATE()) AS NVARCHAR(4));
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='tax_name')
    ALTER TABLE branches ADD tax_name NVARCHAR(100);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='tax_number')
    ALTER TABLE branches ADD tax_number NVARCHAR(50);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='commercial_reg')
    ALTER TABLE branches ADD commercial_reg NVARCHAR(50);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='branch_code')
    ALTER TABLE branches ADD branch_code NVARCHAR(10);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='manager_name')
    ALTER TABLE branches ADD manager_name NVARCHAR(100);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('branches') AND name='logo_path')
    ALTER TABLE branches ADD logo_path NVARCHAR(500);
GO

-- ===== جدول العملات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='currencies')
CREATE TABLE currencies (
    currency_id     INT IDENTITY(1,1) PRIMARY KEY,
    currency_name   NVARCHAR(100) NOT NULL,
    currency_code   NVARCHAR(10)  NOT NULL UNIQUE,
    currency_symbol NVARCHAR(10),
    is_local        BIT DEFAULT 0,
    exchange_rate   DECIMAL(18,6) DEFAULT 1.000000,
    is_active       BIT DEFAULT 1,
    created_at      DATETIME DEFAULT GETDATE()
);
GO

-- إدراج عملات افتراضية
IF NOT EXISTS (SELECT 1 FROM currencies WHERE currency_code='SAR')
    INSERT INTO currencies (currency_name, currency_code, currency_symbol, is_local, exchange_rate)
    VALUES (N'ريال سعودي', 'SAR', N'ر.س', 1, 1.000000);
GO
IF NOT EXISTS (SELECT 1 FROM currencies WHERE currency_code='USD')
    INSERT INTO currencies (currency_name, currency_code, currency_symbol, is_local, exchange_rate)
    VALUES (N'دولار أمريكي', 'USD', '$', 0, 3.750000);
GO
IF NOT EXISTS (SELECT 1 FROM currencies WHERE currency_code='EUR')
    INSERT INTO currencies (currency_name, currency_code, currency_symbol, is_local, exchange_rate)
    VALUES (N'يورو', 'EUR', '€', 0, 4.100000);
GO
IF NOT EXISTS (SELECT 1 FROM currencies WHERE currency_code='AED')
    INSERT INTO currencies (currency_name, currency_code, currency_symbol, is_local, exchange_rate)
    VALUES (N'درهم إماراتي', 'AED', N'د.إ', 0, 1.020000);
GO
IF NOT EXISTS (SELECT 1 FROM currencies WHERE currency_code='KWD')
    INSERT INTO currencies (currency_name, currency_code, currency_symbol, is_local, exchange_rate)
    VALUES (N'دينار كويتي', 'KWD', N'د.ك', 0, 12.200000);
GO

-- ===== سجل الجلسات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='sessions_log')
CREATE TABLE sessions_log (
    session_id     INT IDENTITY(1,1) PRIMARY KEY,
    user_id        INT REFERENCES users(user_id),
    username       NVARCHAR(50),
    device_name    NVARCHAR(100),
    device_fp      NVARCHAR(50),
    ip_address     NVARCHAR(50),
    branch_id      INT,
    login_time     DATETIME DEFAULT GETDATE(),
    logout_time    DATETIME,
    session_status NVARCHAR(20) DEFAULT N'نشط'  -- نشط / مكتمل
);
GO

-- ===== تعزيز جدول المخزون بحقول إضافية =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='inventory_items')
CREATE TABLE inventory_items (
    item_id       INT IDENTITY(1,1) PRIMARY KEY,
    item_code     NVARCHAR(20),
    item_name     NVARCHAR(100) NOT NULL,
    english_name  NVARCHAR(100),
    barcode       NVARCHAR(50),
    category      NVARCHAR(50),
    unit          NVARCHAR(20) DEFAULT N'كجم',
    cost_price    DECIMAL(10,2) DEFAULT 0,
    sale_price    DECIMAL(10,2) DEFAULT 0,
    min_price     DECIMAL(10,2) DEFAULT 0,
    max_price     DECIMAL(10,2) DEFAULT 0,
    current_stock DECIMAL(10,3) DEFAULT 0,
    min_stock     DECIMAL(10,3) DEFAULT 0,
    max_stock     DECIMAL(10,3) DEFAULT 0,
    reorder_point DECIMAL(10,3) DEFAULT 0,
    tax_rate      DECIMAL(5,2) DEFAULT 0,
    is_active     BIT DEFAULT 1,
    created_at    DATETIME DEFAULT GETDATE(),
    updated_at    DATETIME DEFAULT GETDATE()
);
GO

-- إضافة أعمدة إلى ingredients إذا كانت موجودة بدلاً من inventory_items
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='ingredients')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='item_code')
        ALTER TABLE ingredients ADD item_code NVARCHAR(20);
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='english_name')
        ALTER TABLE ingredients ADD english_name NVARCHAR(100);
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='barcode')
        ALTER TABLE ingredients ADD barcode NVARCHAR(50);
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='cost_price')
        ALTER TABLE ingredients ADD cost_price DECIMAL(10,2) DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='sale_price')
        ALTER TABLE ingredients ADD sale_price DECIMAL(10,2) DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='min_price')
        ALTER TABLE ingredients ADD min_price DECIMAL(10,2) DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='max_price')
        ALTER TABLE ingredients ADD max_price DECIMAL(10,2) DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='reorder_point')
        ALTER TABLE ingredients ADD reorder_point DECIMAL(10,3) DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='barcode')
        ALTER TABLE ingredients ADD barcode NVARCHAR(50);
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ingredients') AND name='tax_rate')
        ALTER TABLE ingredients ADD tax_rate DECIMAL(5,2) DEFAULT 0;
END
GO

-- ===== إعدادات معلومات الترخيص =====
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='license_holder')
    INSERT INTO settings (setting_key, value, description) VALUES ('license_holder', N'', N'اسم صاحب الترخيص');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='license_phone')
    INSERT INTO settings (setting_key, value, description) VALUES ('license_phone', N'', N'هاتف التواصل');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='system_version')
    INSERT INTO settings (setting_key, value, description) VALUES ('system_version', N'1.0.0', N'إصدار النظام');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='default_currency')
    INSERT INTO settings (setting_key, value, description) VALUES ('default_currency', N'SAR', N'رمز العملة الافتراضية');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='financial_year')
    INSERT INTO settings (setting_key, value, description) VALUES ('financial_year', N'2025', N'السنة المالية');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='itqan_contact')
    INSERT INTO settings (setting_key, value, description) VALUES ('itqan_contact', N'7774b3b7', N'رقم التواصل مع itQAN Soft');
GO

PRINT N'✅ تم تطبيق التحديث V3 بنجاح!';
GO
