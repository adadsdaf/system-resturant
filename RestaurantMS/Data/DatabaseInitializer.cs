using Dapper;
using Npgsql;

namespace RestaurantMS.Data;

public class DatabaseInitializer
{
    private readonly DbHelper _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(DbHelper db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await CreateTablesAsync();
            await SeedDataAsync();
            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
        }
    }

    private async Task CreateTablesAsync()
    {
        using var conn = _db.OpenConnection();

        var sql = @"
CREATE TABLE IF NOT EXISTS roles (
    role_id SERIAL PRIMARY KEY,
    role_name VARCHAR(50) UNIQUE NOT NULL
);

CREATE TABLE IF NOT EXISTS branches (
    branch_id SERIAL PRIMARY KEY,
    branch_code VARCHAR(20) UNIQUE NOT NULL,
    arabic_name VARCHAR(150) NOT NULL,
    foreign_name VARCHAR(150) DEFAULT '',
    arabic_address TEXT DEFAULT '',
    phone VARCHAR(50) DEFAULT '',
    email VARCHAR(150) DEFAULT '',
    is_main BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS users (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) DEFAULT '',
    password_hash VARCHAR(255) NOT NULL,
    role_id INTEGER REFERENCES roles(role_id),
    branch_id INTEGER REFERENCES branches(branch_id),
    is_active BOOLEAN DEFAULT TRUE,
    last_login TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_sessions (
    session_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(user_id),
    username VARCHAR(50) NOT NULL,
    login_time TIMESTAMP DEFAULT NOW(),
    logout_time TIMESTAMP,
    machine_name VARCHAR(100) DEFAULT '',
    ip_address VARCHAR(50) DEFAULT '',
    branch_id INTEGER REFERENCES branches(branch_id),
    session_status VARCHAR(20) DEFAULT 'active'
);

CREATE TABLE IF NOT EXISTS settings (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT DEFAULT ''
);

CREATE TABLE IF NOT EXISTS menu_categories (
    category_id SERIAL PRIMARY KEY,
    category_name VARCHAR(100) NOT NULL,
    description TEXT DEFAULT '',
    sort_order INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS menu_items (
    item_id SERIAL PRIMARY KEY,
    category_id INTEGER REFERENCES menu_categories(category_id) ON DELETE CASCADE,
    item_name VARCHAR(150) NOT NULL,
    description TEXT DEFAULT '',
    price NUMERIC(10,2) NOT NULL DEFAULT 0,
    cost_price NUMERIC(10,2) DEFAULT 0,
    is_available BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS customers (
    customer_id SERIAL PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(30) DEFAULT '',
    email VARCHAR(100) DEFAULT '',
    address TEXT DEFAULT '',
    notes TEXT DEFAULT '',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS loyalty_accounts (
    loyalty_id SERIAL PRIMARY KEY,
    customer_id INTEGER REFERENCES customers(customer_id) ON DELETE CASCADE,
    points_balance INTEGER DEFAULT 0,
    total_earned INTEGER DEFAULT 0,
    total_redeemed INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS orders (
    order_id SERIAL PRIMARY KEY,
    customer_name VARCHAR(100) DEFAULT 'Guest',
    customer_id INTEGER REFERENCES customers(customer_id),
    served_by INTEGER REFERENCES users(user_id),
    branch_id INTEGER REFERENCES branches(branch_id),
    subtotal NUMERIC(10,2) DEFAULT 0,
    discount_amount NUMERIC(10,2) DEFAULT 0,
    tax_amount NUMERIC(10,2) DEFAULT 0,
    total_amount NUMERIC(10,2) DEFAULT 0,
    payment_method VARCHAR(30) DEFAULT 'Cash',
    payment_status VARCHAR(20) DEFAULT 'Pending',
    order_status VARCHAR(20) DEFAULT 'Pending',
    notes TEXT DEFAULT '',
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS order_items (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES orders(order_id) ON DELETE CASCADE,
    menu_item_id INTEGER REFERENCES menu_items(item_id),
    item_name VARCHAR(150) NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    unit_price NUMERIC(10,2) NOT NULL,
    subtotal NUMERIC(10,2) NOT NULL
);

CREATE TABLE IF NOT EXISTS payments (
    payment_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES orders(order_id),
    amount_paid NUMERIC(10,2) NOT NULL,
    payment_method VARCHAR(30) DEFAULT 'Cash',
    reference_no VARCHAR(100) DEFAULT '',
    payment_date TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS kitchen_orders (
    kitchen_order_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES orders(order_id),
    table_number VARCHAR(20) DEFAULT '',
    customer_name VARCHAR(100) DEFAULT '',
    status VARCHAR(20) DEFAULT 'Pending',
    notes TEXT DEFAULT '',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS kitchen_order_items (
    kitchen_item_id SERIAL PRIMARY KEY,
    kitchen_order_id INTEGER REFERENCES kitchen_orders(kitchen_order_id) ON DELETE CASCADE,
    item_name VARCHAR(150) NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    status VARCHAR(20) DEFAULT 'Pending'
);

CREATE TABLE IF NOT EXISTS ingredients (
    ingredient_id SERIAL PRIMARY KEY,
    ingredient_name VARCHAR(100) NOT NULL,
    unit VARCHAR(30) DEFAULT '',
    current_stock NUMERIC(10,2) DEFAULT 0,
    min_stock NUMERIC(10,2) DEFAULT 0,
    reorder_point NUMERIC(10,2) DEFAULT 0,
    cost_per_unit NUMERIC(10,2) DEFAULT 0,
    supplier_id INTEGER,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS inventory_transactions (
    transaction_id SERIAL PRIMARY KEY,
    ingredient_id INTEGER REFERENCES ingredients(ingredient_id),
    transaction_type VARCHAR(20) NOT NULL,
    quantity NUMERIC(10,2) NOT NULL,
    unit_cost NUMERIC(10,2) DEFAULT 0,
    reference_no VARCHAR(100) DEFAULT '',
    notes TEXT DEFAULT '',
    created_by INTEGER REFERENCES users(user_id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS suppliers (
    supplier_id SERIAL PRIMARY KEY,
    supplier_name VARCHAR(150) NOT NULL,
    contact_person VARCHAR(100) DEFAULT '',
    phone VARCHAR(50) DEFAULT '',
    email VARCHAR(100) DEFAULT '',
    address TEXT DEFAULT '',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS purchase_orders (
    po_id SERIAL PRIMARY KEY,
    supplier_id INTEGER REFERENCES suppliers(supplier_id),
    po_number VARCHAR(50) UNIQUE NOT NULL,
    status VARCHAR(20) DEFAULT 'Draft',
    total_amount NUMERIC(10,2) DEFAULT 0,
    notes TEXT DEFAULT '',
    created_by INTEGER REFERENCES users(user_id),
    created_at TIMESTAMP DEFAULT NOW(),
    received_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS purchase_order_items (
    po_item_id SERIAL PRIMARY KEY,
    po_id INTEGER REFERENCES purchase_orders(po_id) ON DELETE CASCADE,
    ingredient_id INTEGER REFERENCES ingredients(ingredient_id),
    ingredient_name VARCHAR(100) NOT NULL,
    quantity NUMERIC(10,2) NOT NULL,
    unit_cost NUMERIC(10,2) DEFAULT 0,
    total_cost NUMERIC(10,2) DEFAULT 0
);

CREATE TABLE IF NOT EXISTS reservations (
    reservation_id SERIAL PRIMARY KEY,
    customer_name VARCHAR(100) NOT NULL,
    phone VARCHAR(30) DEFAULT '',
    party_size INTEGER DEFAULT 1,
    reservation_date DATE NOT NULL,
    reservation_time TIME NOT NULL,
    table_number VARCHAR(20) DEFAULT '',
    status VARCHAR(20) DEFAULT 'Confirmed',
    notes TEXT DEFAULT '',
    created_by INTEGER REFERENCES users(user_id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sales_invoices (
    invoice_id SERIAL PRIMARY KEY,
    invoice_number VARCHAR(50) UNIQUE NOT NULL,
    customer_name VARCHAR(100) DEFAULT '',
    customer_id INTEGER REFERENCES customers(customer_id),
    order_id INTEGER REFERENCES orders(order_id),
    subtotal NUMERIC(10,2) DEFAULT 0,
    discount_amount NUMERIC(10,2) DEFAULT 0,
    tax_amount NUMERIC(10,2) DEFAULT 0,
    total_amount NUMERIC(10,2) DEFAULT 0,
    payment_method VARCHAR(30) DEFAULT 'Cash',
    status VARCHAR(20) DEFAULT 'Active',
    notes TEXT DEFAULT '',
    created_by INTEGER REFERENCES users(user_id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sales_returns (
    return_id SERIAL PRIMARY KEY,
    return_number VARCHAR(50) UNIQUE NOT NULL,
    invoice_id INTEGER REFERENCES sales_invoices(invoice_id),
    customer_name VARCHAR(100) DEFAULT '',
    return_amount NUMERIC(10,2) DEFAULT 0,
    reason TEXT DEFAULT '',
    status VARCHAR(20) DEFAULT 'Pending',
    created_by INTEGER REFERENCES users(user_id),
    approved_by INTEGER REFERENCES users(user_id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS audit_logs (
    log_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(user_id),
    username VARCHAR(50) DEFAULT '',
    action VARCHAR(100) NOT NULL,
    table_name VARCHAR(50) DEFAULT '',
    record_id INTEGER,
    details TEXT DEFAULT '',
    ip_address VARCHAR(50) DEFAULT '',
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS license_settings (
    setting_id SERIAL PRIMARY KEY,
    setting_key VARCHAR(100) UNIQUE NOT NULL,
    setting_value TEXT DEFAULT '',
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS licensed_devices (
    device_id SERIAL PRIMARY KEY,
    device_name VARCHAR(150) NOT NULL,
    device_key VARCHAR(255) UNIQUE NOT NULL,
    mac_address VARCHAR(50) DEFAULT '',
    is_active BOOLEAN DEFAULT TRUE,
    last_seen TIMESTAMP,
    notes TEXT DEFAULT '',
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS tables (
    table_id SERIAL PRIMARY KEY,
    table_number VARCHAR(20) NOT NULL,
    capacity INTEGER DEFAULT 4,
    branch_id INTEGER REFERENCES branches(branch_id),
    is_active BOOLEAN DEFAULT TRUE
);
";
        await conn.ExecuteAsync(sql);
    }

    private async Task SeedDataAsync()
    {
        using var conn = _db.OpenConnection();

        // Seed roles
        await conn.ExecuteAsync(@"
            INSERT INTO roles (role_name) VALUES
                ('Owner'), ('Admin'), ('Manager'), ('Cashier'), ('Waiter'), ('Kitchen')
            ON CONFLICT (role_name) DO NOTHING");

        // Seed main branch
        await conn.ExecuteAsync(@"
            INSERT INTO branches (branch_code, arabic_name, foreign_name, is_main, is_active)
            VALUES ('BR001', 'الفرع الرئيسي', 'Main Branch', TRUE, TRUE)
            ON CONFLICT (branch_code) DO NOTHING");

        // Seed admin user (password: admin123)
        var adminExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users WHERE username='admin'");
        if (adminExists == 0)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("admin123");
            await conn.ExecuteAsync(@"
                INSERT INTO users (username, full_name, email, password_hash, role_id, branch_id, is_active)
                SELECT 'admin', 'مدير النظام', 'admin@restaurant.com', @hash,
                       (SELECT role_id FROM roles WHERE role_name='Admin'),
                       (SELECT branch_id FROM branches WHERE branch_code='BR001'),
                       TRUE",
                new { hash });
        }

        // Seed owner user (password: owner2025)
        var ownerExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users WHERE username='owner'");
        if (ownerExists == 0)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("owner2025");
            await conn.ExecuteAsync(@"
                INSERT INTO users (username, full_name, email, password_hash, role_id, branch_id, is_active)
                SELECT 'owner', 'صاحب المطعم', 'owner@restaurant.com', @hash,
                       (SELECT role_id FROM roles WHERE role_name='Owner'),
                       (SELECT branch_id FROM branches WHERE branch_code='BR001'),
                       TRUE",
                new { hash });
        }

        // Seed default settings
        var settingsSql = @"
            INSERT INTO settings (key, value) VALUES
                ('restaurant_name', 'مطعم إتقان'),
                ('tax_enabled', '0'),
                ('tax_rate', '15'),
                ('currency', 'ريال'),
                ('receipt_footer', 'شكراً لزيارتكم'),
                ('loyalty_enabled', '1'),
                ('loyalty_points_per_unit', '1'),
                ('points_value', '0.1')
            ON CONFLICT (key) DO NOTHING";
        await conn.ExecuteAsync(settingsSql);

        // Seed sample categories if empty
        var catCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM menu_categories");
        if (catCount == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO menu_categories (category_name, sort_order, is_active) VALUES
                    ('المشروبات', 1, TRUE),
                    ('المقبلات', 2, TRUE),
                    ('الأطباق الرئيسية', 3, TRUE),
                    ('الحلويات', 4, TRUE)");
        }

        // Seed sample menu items if empty
        var itemCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM menu_items");
        if (itemCount == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO menu_items (category_id, item_name, price, is_available) VALUES
                    ((SELECT category_id FROM menu_categories WHERE category_name='المشروبات'), 'قهوة عربية', 15, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='المشروبات'), 'شاي بالنعناع', 10, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='المشروبات'), 'عصير برتقال', 20, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='المقبلات'), 'حمص بالطحينة', 25, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='المقبلات'), 'متبل', 25, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='الأطباق الرئيسية'), 'مندي دجاج', 85, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='الأطباق الرئيسية'), 'كباب مشكل', 65, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='الأطباق الرئيسية'), 'برياني لحم', 90, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='الحلويات'), 'أم علي', 30, TRUE),
                    ((SELECT category_id FROM menu_categories WHERE category_name='الحلويات'), 'كنافة', 35, TRUE)");
        }

        // Seed license settings
        await conn.ExecuteAsync(@"
            INSERT INTO license_settings (setting_key, setting_value) VALUES
                ('license_key', ''),
                ('max_devices', '5'),
                ('expiry_date', '2026-12-31'),
                ('customer_name', 'مطعم إتقان'),
                ('license_type', 'Full')
            ON CONFLICT (setting_key) DO NOTHING");
    }
}
