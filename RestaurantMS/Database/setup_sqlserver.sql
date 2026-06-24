-- ================================================
--  RestaurantMS — SQL Server Express Setup Script
--  قم بتشغيل هذا الملف في SQL Server Management Studio
--  على قاعدة بيانات RestaurantMS
-- ================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RestaurantMS')
    CREATE DATABASE RestaurantMS;
GO

USE RestaurantMS;
GO

-- ===== الأدوار =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='roles')
CREATE TABLE roles (
    role_id    INT IDENTITY(1,1) PRIMARY KEY,
    role_name  NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ===== الفروع =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='branches')
CREATE TABLE branches (
    branch_id    INT IDENTITY(1,1) PRIMARY KEY,
    arabic_name  NVARCHAR(100) NOT NULL,
    address      NVARCHAR(255),
    phone        NVARCHAR(20),
    is_active    BIT DEFAULT 1,
    created_at   DATETIME DEFAULT GETDATE()
);
GO

-- ===== المستخدمون =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='users')
CREATE TABLE users (
    user_id       INT IDENTITY(1,1) PRIMARY KEY,
    full_name     NVARCHAR(100) NOT NULL,
    username      NVARCHAR(50)  NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    role_id       INT NOT NULL REFERENCES roles(role_id),
    branch_id     INT NOT NULL REFERENCES branches(branch_id),
    is_active     BIT DEFAULT 1,
    last_login    DATETIME,
    created_at    DATETIME DEFAULT GETDATE(),
    updated_at    DATETIME DEFAULT GETDATE()
);
GO

-- ===== الإعدادات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='settings')
CREATE TABLE settings (
    setting_id   INT IDENTITY(1,1) PRIMARY KEY,
    setting_key  NVARCHAR(50)  NOT NULL UNIQUE,
    value        NVARCHAR(500),
    description  NVARCHAR(255)
);
GO

-- ===== تصنيفات القائمة =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='menu_categories')
CREATE TABLE menu_categories (
    category_id   INT IDENTITY(1,1) PRIMARY KEY,
    category_name NVARCHAR(100) NOT NULL,
    sort_order    INT DEFAULT 0,
    is_active     BIT DEFAULT 1
);
GO

-- ===== أصناف القائمة =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='menu_items')
CREATE TABLE menu_items (
    item_id       INT IDENTITY(1,1) PRIMARY KEY,
    category_id   INT REFERENCES menu_categories(category_id),
    item_name     NVARCHAR(100) NOT NULL,
    description   NVARCHAR(500),
    price         DECIMAL(10,2) NOT NULL,
    cost_price    DECIMAL(10,2) DEFAULT 0,
    is_available  BIT DEFAULT 1,
    created_at    DATETIME DEFAULT GETDATE(),
    updated_at    DATETIME DEFAULT GETDATE()
);
GO

-- ===== الطاولات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='tables')
CREATE TABLE tables (
    table_id      INT IDENTITY(1,1) PRIMARY KEY,
    branch_id     INT REFERENCES branches(branch_id),
    table_number  INT NOT NULL,
    capacity      INT DEFAULT 4,
    location      NVARCHAR(100),
    is_active     BIT DEFAULT 1
);
GO

-- ===== العملاء =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='customers')
CREATE TABLE customers (
    customer_id  INT IDENTITY(1,1) PRIMARY KEY,
    full_name    NVARCHAR(100) NOT NULL,
    phone        NVARCHAR(20),
    email        NVARCHAR(150),
    is_active    BIT DEFAULT 1,
    created_at   DATETIME DEFAULT GETDATE(),
    updated_at   DATETIME DEFAULT GETDATE()
);
GO

-- ===== حسابات الولاء =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='loyalty_accounts')
CREATE TABLE loyalty_accounts (
    account_id     INT IDENTITY(1,1) PRIMARY KEY,
    customer_id    INT REFERENCES customers(customer_id),
    points_balance INT DEFAULT 0
);
GO

-- ===== الطلبات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='orders')
CREATE TABLE orders (
    order_id        INT IDENTITY(1,1) PRIMARY KEY,
    customer_name   NVARCHAR(100) DEFAULT N'زبون عادي',
    customer_id     INT REFERENCES customers(customer_id),
    served_by       INT REFERENCES users(user_id),
    branch_id       INT REFERENCES branches(branch_id),
    subtotal        DECIMAL(10,2) DEFAULT 0,
    discount_amount DECIMAL(10,2) DEFAULT 0,
    tax_amount      DECIMAL(10,2) DEFAULT 0,
    total_amount    DECIMAL(10,2) NOT NULL,
    payment_method  NVARCHAR(20) DEFAULT 'Cash',
    payment_status  NVARCHAR(20) DEFAULT 'Paid',
    order_status    NVARCHAR(20) DEFAULT 'Completed',
    created_at      DATETIME DEFAULT GETDATE()
);
GO

-- ===== عناصر الطلبات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='order_items')
CREATE TABLE order_items (
    item_id       INT IDENTITY(1,1) PRIMARY KEY,
    order_id      INT REFERENCES orders(order_id) ON DELETE CASCADE,
    menu_item_id  INT REFERENCES menu_items(item_id),
    item_name     NVARCHAR(100) NOT NULL,
    quantity      INT NOT NULL,
    unit_price    DECIMAL(10,2) NOT NULL,
    subtotal      DECIMAL(10,2) NOT NULL
);
GO

-- ===== المدفوعات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='payments')
CREATE TABLE payments (
    payment_id     INT IDENTITY(1,1) PRIMARY KEY,
    order_id       INT REFERENCES orders(order_id),
    amount_paid    DECIMAL(10,2) NOT NULL,
    payment_method NVARCHAR(20),
    payment_time   DATETIME DEFAULT GETDATE()
);
GO

-- ===== طلبات المطبخ =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='kitchen_orders')
CREATE TABLE kitchen_orders (
    kitchen_order_id INT IDENTITY(1,1) PRIMARY KEY,
    order_id         INT REFERENCES orders(order_id),
    table_number     NVARCHAR(10),
    customer_name    NVARCHAR(100),
    status           NVARCHAR(20) DEFAULT 'Pending',
    notes            NVARCHAR(500),
    created_at       DATETIME DEFAULT GETDATE(),
    updated_at       DATETIME DEFAULT GETDATE()
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='kitchen_order_items')
CREATE TABLE kitchen_order_items (
    id               INT IDENTITY(1,1) PRIMARY KEY,
    kitchen_order_id INT REFERENCES kitchen_orders(kitchen_order_id) ON DELETE CASCADE,
    item_name        NVARCHAR(100) NOT NULL,
    quantity         INT NOT NULL
);
GO

-- ===== الحجوزات =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='reservations')
CREATE TABLE reservations (
    reservation_id   INT IDENTITY(1,1) PRIMARY KEY,
    customer_name    NVARCHAR(100) NOT NULL,
    phone            NVARCHAR(20),
    reservation_date DATE NOT NULL,
    reservation_time TIME,
    party_size       INT DEFAULT 2,
    table_id         INT REFERENCES tables(table_id),
    special_requests NVARCHAR(500),
    status           NVARCHAR(20) DEFAULT 'Pending',
    branch_id        INT REFERENCES branches(branch_id),
    created_at       DATETIME DEFAULT GETDATE()
);
GO

-- ===== المواد المخزنية =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='ingredients')
CREATE TABLE ingredients (
    ingredient_id  INT IDENTITY(1,1) PRIMARY KEY,
    ingredient_name NVARCHAR(100) NOT NULL,
    unit           NVARCHAR(20) DEFAULT N'كغ',
    current_stock  DECIMAL(10,2) DEFAULT 0,
    min_stock      DECIMAL(10,2) DEFAULT 0,
    cost_per_unit  DECIMAL(10,2) DEFAULT 0,
    is_active      BIT DEFAULT 1,
    created_at     DATETIME DEFAULT GETDATE(),
    updated_at     DATETIME DEFAULT GETDATE()
);
GO

-- ===== حركات المخزون =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='inventory_movements')
CREATE TABLE inventory_movements (
    movement_id    INT IDENTITY(1,1) PRIMARY KEY,
    ingredient_id  INT REFERENCES ingredients(ingredient_id),
    movement_type  NVARCHAR(5) NOT NULL, -- IN / OUT
    quantity       DECIMAL(10,2) NOT NULL,
    notes          NVARCHAR(300),
    performed_by   INT REFERENCES users(user_id),
    branch_id      INT REFERENCES branches(branch_id),
    created_at     DATETIME DEFAULT GETDATE()
);
GO

-- ===== الموردون =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='suppliers')
CREATE TABLE suppliers (
    supplier_id   INT IDENTITY(1,1) PRIMARY KEY,
    company_name  NVARCHAR(150) NOT NULL,
    contact_name  NVARCHAR(100),
    phone         NVARCHAR(20),
    email         NVARCHAR(150),
    is_active     BIT DEFAULT 1,
    created_at    DATETIME DEFAULT GETDATE()
);
GO

-- ===== طلبات الشراء =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='purchase_orders')
CREATE TABLE purchase_orders (
    po_id                  INT IDENTITY(1,1) PRIMARY KEY,
    supplier_id            INT REFERENCES suppliers(supplier_id),
    ordered_by             INT REFERENCES users(user_id),
    total_amount           DECIMAL(10,2) DEFAULT 0,
    status                 NVARCHAR(20) DEFAULT 'Pending',
    expected_delivery_date DATE,
    received_at            DATETIME,
    notes                  NVARCHAR(500),
    created_at             DATETIME DEFAULT GETDATE()
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='purchase_order_items')
CREATE TABLE purchase_order_items (
    poi_id          INT IDENTITY(1,1) PRIMARY KEY,
    po_id           INT REFERENCES purchase_orders(po_id) ON DELETE CASCADE,
    ingredient_id   INT REFERENCES ingredients(ingredient_id),
    quantity_ordered DECIMAL(10,2) NOT NULL,
    unit_price      DECIMAL(10,2) NOT NULL,
    subtotal        DECIMAL(10,2) NOT NULL
);
GO

-- ===== سجل الأحداث =====
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='audit_logs')
CREATE TABLE audit_logs (
    log_id     INT IDENTITY(1,1) PRIMARY KEY,
    user_id    INT REFERENCES users(user_id),
    action     NVARCHAR(200) NOT NULL,
    details    NVARCHAR(1000),
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- ================================================
--  البيانات الأولية (Seed Data)
-- ================================================

-- الأدوار
IF NOT EXISTS (SELECT 1 FROM roles)
INSERT INTO roles (role_name) VALUES
    (N'Owner'), (N'Admin'), (N'Manager'), (N'Cashier'), (N'Kitchen'), (N'Waiter');
GO

-- فرع افتراضي
IF NOT EXISTS (SELECT 1 FROM branches)
INSERT INTO branches (arabic_name, address, phone) VALUES
    (N'الفرع الرئيسي', N'الرياض — شارع التحلية', N'0501234567');
GO

-- مستخدمون افتراضيون
IF NOT EXISTS (SELECT 1 FROM users WHERE username='admin')
BEGIN
    -- admin / admin123
    INSERT INTO users (full_name, username, password_hash, role_id, branch_id) VALUES
        (N'مدير النظام', 'admin',
         '$2a$11$hmDSmhXUdvbzcvsl1yA/zeTvwtnep9cFHYWfkpJUnwBzVMV/5OZte',
         (SELECT role_id FROM roles WHERE role_name='Admin'),
         (SELECT TOP 1 branch_id FROM branches WHERE is_active = 1));
END
GO

IF NOT EXISTS (SELECT 1 FROM users WHERE username='owner')
BEGIN
    -- owner / owner2025
    INSERT INTO users (full_name, username, password_hash, role_id, branch_id) VALUES
        (N'صاحب المطعم', 'owner',
         '$2a$11$UpMV0gS1B..nb9P4HwhFLOMK1CQgxP81iwfkIFODSrN1MnPUeLvAe',
         (SELECT role_id FROM roles WHERE role_name='Owner'),
         (SELECT TOP 1 branch_id FROM branches WHERE is_active = 1));
END
GO

IF NOT EXISTS (SELECT 1 FROM users WHERE username='cashier1')
BEGIN
    -- cashier1 / cashier123
    INSERT INTO users (full_name, username, password_hash, role_id, branch_id) VALUES
        (N'أحمد الكاشير', 'cashier1',
         '$2a$11$8JJLi4qolMtBKpfAMdsOVe/M2n9tlwn3siRbOoPUMR/zSnq/hEXZm',
         (SELECT role_id FROM roles WHERE role_name='Cashier'),
         (SELECT TOP 1 branch_id FROM branches WHERE is_active = 1));
END
GO

IF NOT EXISTS (SELECT 1 FROM users WHERE username='Hassan')
BEGIN
    -- Hassan / 123456
    INSERT INTO users (full_name, username, password_hash, role_id, branch_id) VALUES
        (N'حسن المدير', 'Hassan',
         '$2a$11$9OL1cupJHFrzZZhQH.FWbeGBZIfj5DvGyMEUKyt3CMxOaOwdiy9LO',
         (SELECT role_id FROM roles WHERE role_name='Admin'),
         (SELECT TOP 1 branch_id FROM branches WHERE is_active = 1));
END
GO

-- الإعدادات
IF NOT EXISTS (SELECT 1 FROM settings)
INSERT INTO settings (setting_key, value, description) VALUES
    ('restaurant_name', N'مطعم الإتقان',     N'اسم المطعم'),
    ('currency',        N'ريال',              N'العملة'),
    ('tax_enabled',     '0',                  N'تفعيل الضريبة (1=نعم, 0=لا)'),
    ('tax_rate',        '15',                 N'نسبة الضريبة %'),
    ('loyalty_enabled', '1',                  N'تفعيل نظام النقاط'),
    ('points_per_riyal','1',                  N'نقاط لكل ريال'),
    ('receipt_footer',  N'شكراً لزيارتكم',  N'نص أسفل الإيصال');
GO

-- تصنيفات القائمة
IF NOT EXISTS (SELECT 1 FROM menu_categories)
INSERT INTO menu_categories (category_name, sort_order) VALUES
    (N'مشروبات ساخنة', 1), (N'مشروبات باردة', 2),
    (N'وجبات رئيسية', 3), (N'مقبلات', 4),
    (N'حلويات', 5), (N'وجبات خفيفة', 6);
GO

-- أصناف القائمة
IF NOT EXISTS (SELECT 1 FROM menu_items)
INSERT INTO menu_items (category_id, item_name, price, cost_price) VALUES
    (1, N'قهوة عربية',       8,  2),
    (1, N'شاي كرك',          7,  1.5),
    (1, N'كابتشينو',        15,  4),
    (1, N'لاتيه',           16,  4.5),
    (2, N'عصير برتقال',     12,  3),
    (2, N'ليمون بنعناع',    10,  2.5),
    (2, N'مياه معدنية',      3,  0.5),
    (3, N'برغر دجاج',       28, 10),
    (3, N'برغر لحم',        32, 12),
    (3, N'دجاج مشوي',       35, 12),
    (3, N'سمك مقلي',        30, 11),
    (4, N'سلطة خضراء',      12,  4),
    (4, N'حمص بطحينة',      10,  3),
    (4, N'سمبوسك',          15,  5),
    (5, N'أم علي',          18,  5),
    (5, N'كنافة',           20,  6),
    (5, N'آيس كريم',        12,  3),
    (6, N'نجر بطاطس',        8,  2),
    (6, N'بيتزا صغيرة',     22,  7);
GO

-- طاولات
IF NOT EXISTS (SELECT 1 FROM tables)
INSERT INTO tables (branch_id, table_number, capacity, location)
SELECT b.branch_id, n, 4, N'صالة داخلية'
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) AS t(n)
CROSS JOIN branches b
WHERE b.arabic_name = N'الفرع الرئيسي';
GO

-- مواد مخزنية
IF NOT EXISTS (SELECT 1 FROM ingredients)
INSERT INTO ingredients (ingredient_name, unit, current_stock, min_stock, cost_per_unit) VALUES
    (N'طحين',       N'كغ',    50, 10, 2),
    (N'سكر',        N'كغ',    30,  5, 3),
    (N'زيت نباتي',  N'لتر',   20,  5, 8),
    (N'دجاج',       N'كغ',    15,  5, 20),
    (N'لحم بقري',   N'كغ',    10,  3, 35),
    (N'بيض',        N'بيضة',  60, 12, 1),
    (N'قهوة عربية', N'كغ',     5,  1, 45),
    (N'شاي',        N'كغ',     3,  0.5,30),
    (N'ليمون',      N'كغ',     4,  1, 5),
    (N'طماطم',      N'كغ',    10,  3, 4),
    (N'بصل',        N'كغ',     8,  2, 2),
    (N'ثوم',        N'كغ',     2,  0.5, 8),
    (N'ملح',        N'كغ',     5,  1, 1),
    (N'فلفل أسود',  N'كغ',     1,  0.2,25),
    (N'زبدة',       N'كغ',     3,  0.5,20);
GO

-- عميل تجريبي
IF NOT EXISTS (SELECT 1 FROM customers)
BEGIN
    INSERT INTO customers (full_name, phone, email) VALUES (N'محمد العميل', '0501111111', 'test@test.com');
    INSERT INTO loyalty_accounts (customer_id, points_balance)
        SELECT customer_id, 150 FROM customers WHERE phone='0501111111';
END
GO

PRINT N'✅ تم إنشاء قاعدة البيانات وتعبئة البيانات الأولية بنجاح!';
PRINT N'بيانات الدخول: admin / admin123 أو owner / owner2025';
GO
