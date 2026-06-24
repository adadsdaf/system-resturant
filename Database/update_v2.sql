-- ================================================
--  RestaurantMS V2 — جداول الأجهزة والصلاحيات والطابعات
--  قم بتشغيله بعد setup_sqlserver.sql و update_receipt_settings.sql
-- ================================================

USE RestaurantMS;
GO

-- ===== الأجهزة المسجلة =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='registered_devices')
CREATE TABLE registered_devices (
    device_id   INT IDENTITY(1,1) PRIMARY KEY,
    device_fp   NVARCHAR(50)  NOT NULL UNIQUE,
    device_name NVARCHAR(100),
    device_role NVARCHAR(20)  DEFAULT N'General',
    ip_address  NVARCHAR(50),
    first_seen  DATETIME      DEFAULT GETDATE(),
    last_seen   DATETIME      DEFAULT GETDATE(),
    is_active   BIT           DEFAULT 1,
    notes       NVARCHAR(300)
);
GO

-- ===== صلاحيات الأدوار =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='role_permissions')
CREATE TABLE role_permissions (
    perm_id    INT IDENTITY(1,1) PRIMARY KEY,
    role_id    INT          NOT NULL REFERENCES roles(role_id) ON DELETE CASCADE,
    page_key   NVARCHAR(50) NOT NULL,
    is_allowed BIT          DEFAULT 1
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
               WHERE CONSTRAINT_NAME='UQ_role_page')
ALTER TABLE role_permissions ADD CONSTRAINT UQ_role_page UNIQUE (role_id, page_key);
GO

-- ===== صلاحيات Owner (كل الصفحات) =====
IF NOT EXISTS (SELECT 1 FROM role_permissions rp JOIN roles r ON rp.role_id=r.role_id WHERE r.role_name=N'Owner')
INSERT INTO role_permissions (role_id, page_key, is_allowed)
SELECT r.role_id, p.page_key, 1
FROM roles r
CROSS JOIN (VALUES ('Dashboard'),('Pos'),('Kitchen'),('Menu'),('Inventory'),('Customers'),
                   ('Suppliers'),('Sales'),('Reservations'),('Reports'),('Admin')) AS p(page_key)
WHERE r.role_name = N'Owner';
GO

-- ===== صلاحيات Admin (كل الصفحات) =====
IF NOT EXISTS (SELECT 1 FROM role_permissions rp JOIN roles r ON rp.role_id=r.role_id WHERE r.role_name=N'Admin')
INSERT INTO role_permissions (role_id, page_key, is_allowed)
SELECT r.role_id, p.page_key, 1
FROM roles r
CROSS JOIN (VALUES ('Dashboard'),('Pos'),('Kitchen'),('Menu'),('Inventory'),('Customers'),
                   ('Suppliers'),('Sales'),('Reservations'),('Reports'),('Admin')) AS p(page_key)
WHERE r.role_name = N'Admin';
GO

-- ===== صلاحيات Manager (بدون Admin) =====
IF NOT EXISTS (SELECT 1 FROM role_permissions rp JOIN roles r ON rp.role_id=r.role_id WHERE r.role_name=N'Manager')
INSERT INTO role_permissions (role_id, page_key, is_allowed)
SELECT r.role_id, p.page_key, p.allowed
FROM roles r
CROSS JOIN (VALUES ('Dashboard',1),('Pos',1),('Kitchen',1),('Menu',1),('Inventory',1),
                   ('Customers',1),('Suppliers',1),('Sales',1),('Reservations',1),
                   ('Reports',1),('Admin',0)) AS p(page_key, allowed)
WHERE r.role_name = N'Manager';
GO

-- ===== صلاحيات Cashier =====
IF NOT EXISTS (SELECT 1 FROM role_permissions rp JOIN roles r ON rp.role_id=r.role_id WHERE r.role_name=N'Cashier')
INSERT INTO role_permissions (role_id, page_key, is_allowed)
SELECT r.role_id, p.page_key, p.allowed
FROM roles r
CROSS JOIN (VALUES ('Dashboard',1),('Pos',1),('Kitchen',0),('Menu',0),('Inventory',0),
                   ('Customers',1),('Suppliers',0),('Sales',1),('Reservations',0),
                   ('Reports',0),('Admin',0)) AS p(page_key, allowed)
WHERE r.role_name = N'Cashier';
GO

-- ===== صلاحيات Kitchen =====
IF NOT EXISTS (SELECT 1 FROM role_permissions rp JOIN roles r ON rp.role_id=r.role_id WHERE r.role_name=N'Kitchen')
INSERT INTO role_permissions (role_id, page_key, is_allowed)
SELECT r.role_id, p.page_key, p.allowed
FROM roles r
CROSS JOIN (VALUES ('Dashboard',1),('Pos',0),('Kitchen',1),('Menu',0),('Inventory',0),
                   ('Customers',0),('Suppliers',0),('Sales',0),('Reservations',0),
                   ('Reports',0),('Admin',0)) AS p(page_key, allowed)
WHERE r.role_name = N'Kitchen';
GO

-- ===== صلاحيات Waiter =====
IF NOT EXISTS (SELECT 1 FROM role_permissions rp JOIN roles r ON rp.role_id=r.role_id WHERE r.role_name=N'Waiter')
INSERT INTO role_permissions (role_id, page_key, is_allowed)
SELECT r.role_id, p.page_key, p.allowed
FROM roles r
CROSS JOIN (VALUES ('Dashboard',1),('Pos',1),('Kitchen',0),('Menu',0),('Inventory',0),
                   ('Customers',1),('Suppliers',0),('Sales',0),('Reservations',1),
                   ('Reports',0),('Admin',0)) AS p(page_key, allowed)
WHERE r.role_name = N'Waiter';
GO

-- ===== إعدادات الطابعات والأجهزة =====
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='printer_thermal_name')
    INSERT INTO settings (setting_key, value, description) VALUES ('printer_thermal_name', N'', N'اسم الطابعة الحرارية 80mm');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='printer_thermal_type')
    INSERT INTO settings (setting_key, value, description) VALUES ('printer_thermal_type', N'Windows', N'نوع اتصال الطابعة: Windows / Network / Serial');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='printer_thermal_address')
    INSERT INTO settings (setting_key, value, description) VALUES ('printer_thermal_address', N'', N'IP أو COM للطابعة الحرارية');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='printer_thermal_port')
    INSERT INTO settings (setting_key, value, description) VALUES ('printer_thermal_port', N'9100', N'منفذ IP للطابعة الحرارية');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='printer_large_name')
    INSERT INTO settings (setting_key, value, description) VALUES ('printer_large_name', N'', N'اسم طابعة A4 الكبيرة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='printer_large_type')
    INSERT INTO settings (setting_key, value, description) VALUES ('printer_large_type', N'Windows', N'نوع اتصال طابعة A4');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='printer_large_address')
    INSERT INTO settings (setting_key, value, description) VALUES ('printer_large_address', N'', N'IP أو COM لطابعة A4');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='max_devices')
    INSERT INTO settings (setting_key, value, description) VALUES ('max_devices', N'5', N'الحد الأقصى لعدد الأجهزة المسجلة');
GO
IF NOT EXISTS (SELECT 1 FROM settings WHERE setting_key='device_auto_register')
    INSERT INTO settings (setting_key, value, description) VALUES ('device_auto_register', N'1', N'تسجيل الأجهزة تلقائياً عند أول تشغيل');
GO

PRINT N'✅ تم تطبيق التحديث V2 بنجاح!';
GO
