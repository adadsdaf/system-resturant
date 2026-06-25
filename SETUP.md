# دليل تثبيت وإعداد نظام إدارة المطاعم — itQAN Soft

## متطلبات النظام

| المتطلب | الحد الأدنى | الموصى به |
|---------|------------|-----------|
| نظام التشغيل | Windows 10 (64-bit) | Windows 11 (64-bit) |
| المعالج | Core i3 / 2.0 GHz | Core i5 / 3.0 GHz |
| الذاكرة RAM | 4 GB | 8 GB |
| مساحة القرص | 2 GB فارغة | 10 GB فارغة |
| قاعدة البيانات | SQL Server Express 2019 | SQL Server 2022 |
| إطار العمل | .NET 8.0 Runtime | .NET 8.0 Runtime |
| الشبكة | اختياري (للعمل متعدد الأجهزة) | LAN 100 Mbps |

---

## الخطوة 1 — تثبيت SQL Server Express

### تنزيل SQL Server Express 2022
1. اذهب إلى: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. اختر **Express** وقم بتنزيله
3. شغّل الملف المنزَّل واختر **Basic** للتثبيت السريع
4. انتظر حتى اكتمال التثبيت (قد يستغرق 5-10 دقائق)

### تثبيت SQL Server Management Studio (SSMS)
1. من نفس الصفحة، انقر على **Install SSMS**
2. نزّل وثبّت SSMS (أداة إدارة قواعد البيانات)

---

## الخطوة 2 — تهيئة SQL Server

### تفعيل الاتصال عن بُعد (للشبكة المحلية فقط)
1. افتح **SQL Server Configuration Manager** من قائمة ابدأ
2. انتقل إلى: `SQL Server Network Configuration → Protocols for SQLEXPRESS`
3. انقر بزر الماوس الأيمن على **TCP/IP** واختر **Enable**
4. أعد تشغيل خدمة SQL Server:
   - انتقل إلى `SQL Server Services`
   - انقر بزر الأيمن على `SQL Server (SQLEXPRESS)` → **Restart**

### تفعيل مصادقة SQL (اختياري)
1. افتح SSMS واتصل بـ `.\SQLEXPRESS` باستخدام Windows Authentication
2. انقر بزر الأيمن على اسم الخادم → **Properties**
3. اختر **Security** → فعّل **SQL Server and Windows Authentication mode**
4. أعد تشغيل الخدمة

---

## الخطوة 3 — إنشاء قاعدة البيانات

### من SSMS:
1. افتح SSMS واتصل بـ `.\SQLEXPRESS`
2. انقر بزر الأيمن على **Databases** → **New Database**
3. أدخل اسم القاعدة: `RestaurantDB`
4. انقر **OK**

### تشغيل سكريبت الجداول:
1. في SSMS، افتح ملف: `Database/schema.sql` (موجود مع ملفات التطبيق)
2. تأكد أن القاعدة المختارة هي `RestaurantDB`
3. اضغط **F5** أو زر **Execute** لتشغيل السكريبت

### سكريبت الإنشاء الأساسي (اختياري — تشغيل يدوي):
```sql
-- إنشاء قاعدة البيانات
CREATE DATABASE RestaurantDB;
GO
USE RestaurantDB;
GO

-- جدول الأدوار
CREATE TABLE roles (
    role_id    INT IDENTITY PRIMARY KEY,
    role_name  NVARCHAR(50) NOT NULL UNIQUE
);

-- إدراج الأدوار الأساسية
INSERT INTO roles (role_name) VALUES
('Owner'), ('Admin'), ('Manager'), ('Cashier'), ('Kitchen'), ('Waiter');

-- جدول الفروع
CREATE TABLE branches (
    branch_id    INT IDENTITY PRIMARY KEY,
    arabic_name  NVARCHAR(100),
    address      NVARCHAR(255),
    phone        NVARCHAR(30),
    is_active    BIT DEFAULT 1
);

-- إدراج فرع افتراضي
INSERT INTO branches (arabic_name, address, phone)
VALUES (N'الفرع الرئيسي', N'—', N'—');

-- جدول المستخدمين
CREATE TABLE users (
    user_id       INT IDENTITY PRIMARY KEY,
    username      NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    full_name     NVARCHAR(100),
    role_id       INT REFERENCES roles(role_id),
    branch_id     INT REFERENCES branches(branch_id),
    is_active     BIT DEFAULT 1,
    last_login    DATETIME,
    created_at    DATETIME DEFAULT GETDATE()
);

-- جدول الإعدادات
CREATE TABLE settings (
    setting_id    INT IDENTITY PRIMARY KEY,
    setting_key   NVARCHAR(100) NOT NULL UNIQUE,
    setting_value NVARCHAR(MAX)
);

-- إعدادات افتراضية
INSERT INTO settings (setting_key, setting_value) VALUES
('restaurant_name', N'مطعمي'),
('currency_symbol', N'ريال'),
('vat_rate', '15'),
('receipt_footer', N'شكراً لزيارتكم');
```

---

## الخطوة 4 — تثبيت .NET 8.0

1. اذهب إلى: https://dotnet.microsoft.com/download/dotnet/8.0
2. في قسم **.NET Desktop Runtime 8.0**، نزّل نسخة x64
3. ثبّت الملف المنزَّل

---

## الخطوة 5 — إعداد ملف الاتصال

### تعديل appsettings.json:
ابحث عن ملف `appsettings.json` في مجلد التطبيق وعدّل connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RestaurantDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### إذا كنت تستخدم SQL Authentication (اسم مستخدم + كلمة مرور):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RestaurantDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### إذا كان الخادم على جهاز آخر في الشبكة:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.100\\SQLEXPRESS;Database=RestaurantDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

---

## الخطوة 6 — تشغيل التطبيق للمرة الأولى

### إعداد مالك النظام:
1. شغّل ملف `RestaurantMS.Desktop.exe`
2. ستظهر **شاشة إعداد مالك النظام** (تظهر مرة واحدة فقط)
3. أدخل:
   - اسم مالك النظام
   - كلمة مرور قوية (لا تُنسى هذه الكلمة)
   - البريد الإلكتروني ورقم الهاتف (اختياري)
4. انقر **حفظ وإكمال الإعداد**

### إعداد بيانات المطعم:
1. سجّل الدخول بحساب مالك النظام
2. اذهب إلى **الإدارة والإعدادات** ← **بيانات الفرع**
3. أدخل بيانات المطعم: الاسم، العنوان، رقم الهاتف، الضريبة

---

## الخطوة 7 — إنشاء المستخدمين

### إضافة أول مستخدم (مدير):
1. اذهب إلى **الإدارة والإعدادات** ← تبويب **المستخدمون**
2. انقر **＋ مستخدم جديد**
3. اختر الدور: **Admin** أو **Manager**
4. أدخل البيانات وانقر حفظ

### الأدوار المتاحة:

| الدور | الصلاحيات |
|-------|-----------|
| Owner | جميع الصلاحيات (حساب حصري للمالك) |
| Admin | إدارة كاملة للنظام |
| Manager | إدارة بدون تعديل الإعدادات |
| Cashier | نقطة البيع، العملاء، المبيعات |
| Kitchen | شاشة المطبخ فقط |
| Waiter | نقطة البيع، الحجوزات، العملاء |

---

## الخطوة 8 — إعداد القائمة والمخزون

1. **القائمة**: الإدارة والإعدادات ← القائمة ← إضافة فئات وأصناف
2. **المخزون**: الإضافة والتحديث من صفحة إدارة المخزون
3. **الموردون**: إضافة موردين وأوامر الشراء من صفحة الموردين

---

## استكشاف الأخطاء وإصلاحها

### مشكلة: "لا يمكن الاتصال بـ SQL Server"
- تحقق من تشغيل خدمة SQL Server Express
- تحقق من صحة Connection String في appsettings.json
- تأكد من تفعيل TCP/IP في SQL Server Configuration Manager

### مشكلة: "قاعدة البيانات غير موجودة"
- شغّل سكريبت الإنشاء من الخطوة 3

### مشكلة: "ملف appsettings.json غير موجود"
- يجب أن يكون في نفس مجلد ملف .exe
- أنشئه يدوياً بالمحتوى الوارد في الخطوة 5

### مشكلة: "خطأ .NET Runtime"
- تأكد من تثبيت .NET Desktop Runtime 8.0 (x64)

---

## التحديث إلى إصدار جديد

1. أغلق التطبيق تماماً
2. احتفظ بنسخة من `appsettings.json` و `owner.dat`
3. انسخ ملفات الإصدار الجديد فوق الملفات القديمة
4. أعد ملف `appsettings.json` من نسختك الاحتياطية
5. شغّل التطبيق — سيتم ترقية قاعدة البيانات تلقائياً إذا لزم

---

## النسخ الاحتياطي

### نسخ قاعدة البيانات:
```sql
-- من SSMS، شغّل هذا الأمر:
BACKUP DATABASE RestaurantDB
TO DISK = 'C:\Backup\RestaurantDB_' + FORMAT(GETDATE(),'yyyyMMdd') + '.bak'
WITH FORMAT, COMPRESSION;
```

### الملفات المهمة للنسخ الاحتياطي:
- `appsettings.json` — إعدادات الاتصال
- `%AppData%\itQAN Soft\RestaurantMS\owner.dat` — بيانات مالك النظام
- `%AppData%\itQAN Soft\RestaurantMS\licenses.json` — بيانات الترخيص

---

## معلومات التواصل والدعم

**itQAN Soft** — نظم المعلومات والحلول البرمجية

- البريد الإلكتروني: info@itqansoft.com
- الموقع: www.itqansoft.com

---
*نظام إدارة المطاعم — الإصدار 1.0 — © 2025 itQAN Soft*
