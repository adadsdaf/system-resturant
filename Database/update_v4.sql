-- ================================================
--  RestaurantMS V4 — إدارة الصلاحيات الهرمية
--  تتبع منشئ الحساب + صلاحية إدارة الموظفين
--  قم بتشغيله بعد update_v3.sql
-- ================================================

USE RestaurantMS;
GO

-- ===== إضافة عمود created_by لجدول المستخدمين =====
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('users') AND name='created_by')
    ALTER TABLE users ADD created_by INT NULL REFERENCES users(user_id);
GO

-- ===== إضافة مفتاح صلاحية إدارة الموظفين =====
-- يُمكِّن هذا المفتاح المدير من إدارة حسابات موظفيه فقط
IF NOT EXISTS (SELECT 1 FROM role_permissions WHERE page_key = 'UserMgmt')
BEGIN
    -- المالك والمسؤول: وصول كامل
    INSERT INTO role_permissions (role_id, page_key, is_allowed)
    SELECT role_id, 'UserMgmt', 1
    FROM roles
    WHERE role_name IN ('Owner', 'Admin', 'Manager')
    AND role_id NOT IN (SELECT role_id FROM role_permissions WHERE page_key='UserMgmt');

    -- الأدوار الأخرى: بدون وصول
    INSERT INTO role_permissions (role_id, page_key, is_allowed)
    SELECT role_id, 'UserMgmt', 0
    FROM roles
    WHERE role_name IN ('Cashier', 'Kitchen', 'Waiter')
    AND role_id NOT IN (SELECT role_id FROM role_permissions WHERE page_key='UserMgmt');
END
GO

-- ===== تحديث صلاحيات المدير لتشمل وصول الإدارة =====
UPDATE role_permissions
SET is_allowed = 1
WHERE role_id = (SELECT role_id FROM roles WHERE role_name = 'Manager')
  AND page_key IN ('Admin', 'UserMgmt');
GO

-- ===== تأكيد القيم الافتراضية للصلاحيات =====
-- إدراج صلاحيات المدير إذا لم تكن موجودة
DECLARE @ManagerRoleId INT = (SELECT TOP 1 role_id FROM roles WHERE role_name='Manager');

IF @ManagerRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM role_permissions WHERE role_id=@ManagerRoleId AND page_key='Admin')
        INSERT INTO role_permissions (role_id, page_key, is_allowed)
        VALUES (@ManagerRoleId, 'Admin', 1);

    IF NOT EXISTS (SELECT 1 FROM role_permissions WHERE role_id=@ManagerRoleId AND page_key='UserMgmt')
        INSERT INTO role_permissions (role_id, page_key, is_allowed)
        VALUES (@ManagerRoleId, 'UserMgmt', 1);
END
GO

PRINT 'V4: تم تحديث صلاحيات المدير وإضافة عمود created_by';
