# دليل إعداد وتشغيل نظام إدارة المطاعم
## RestaurantMS Desktop — itQAN Soft
### إصدار 2.0 | .NET 7 WPF + SQL Server Express | ثيم أبيض/فاتح

---

## 📋 المتطلبات الأساسية قبل البدء

| المطلب | الإصدار | رابط التنزيل |
|--------|---------|-------------|
| Visual Studio 2022 | 17.8 أو أحدث | https://visualstudio.microsoft.com |
| .NET 7 SDK | 7.0 أو أحدث | https://dotnet.microsoft.com/download/dotnet/7.0 |
| SQL Server Express | 2019 أو 2022 | https://www.microsoft.com/sql-server |
| SSMS (اختياري) | أي إصدار | https://aka.ms/ssmsfullsetup |

> **ملاحظة:** يتطلب المشروع **.NET 7 SDK** وليس .NET 8.

---

## 🗄️ الخطوة الأولى: إعداد قاعدة البيانات

### الطريقة 1: باستخدام SSMS (الأسهل)

1. افتح **SQL Server Management Studio (SSMS)**
2. اتصل بـ: `.\SQLEXPRESS` أو `localhost\SQLEXPRESS`
3. من القائمة: **File → Open → File**
4. اختر الملف: `Database\setup_sqlserver.sql`
5. اضغط **Execute** أو `F5`
6. انتظر حتى ترى الرسالة:
   ```
   ✅ تم إنشاء قاعدة البيانات وتعبئة البيانات الأولية بنجاح!
   ```

### الطريقة 2: باستخدام سطر الأوامر (CMD)

```cmd
sqlcmd -S .\SQLEXPRESS -i "Database\setup_sqlserver.sql"
```

### الطريقة 3: من داخل Visual Studio

1. افتح **View → SQL Server Object Explorer**
2. اتصل بـ `.\SQLEXPRESS`
3. انقر بالزر الأيمن على قاعدة البيانات → **New Query**
4. الصق محتوى ملف `setup_sqlserver.sql`
5. اضغط **Execute**

---

## ⚙️ الخطوة الثانية: ضبط إعدادات الاتصال

افتح ملف `appsettings.json` في جذر المشروع وتأكد من صحة الاتصال:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RestaurantMS;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### إذا كان اسم السيرفر مختلفاً:

| الحالة | القيمة المطلوبة |
|--------|----------------|
| SQL Server Express الافتراضي | `Server=.\SQLEXPRESS` |
| SQL Server المسمى | `Server=.\YOUR_INSTANCE_NAME` |
| SQL Server على منفذ محدد | `Server=localhost,1433` |
| SQL Server على جهاز آخر | `Server=192.168.1.x\SQLEXPRESS` |

### إذا كنت تستخدم SQL Authentication:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=RestaurantMS;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

---

## 💻 الخطوة الثالثة: فتح المشروع وبناؤه

### فتح المشروع في Visual Studio 2022:

1. افتح **Visual Studio 2022**
2. اختر **Open a project or solution**
3. اختر الملف: `RestaurantMS.Desktop.csproj`
4. انتظر حتى يكتمل تحميل الحزم

### تثبيت حزم NuGet (تلقائي):

Visual Studio يُنزّل الحزم تلقائياً. إذا لم يحدث:

```powershell
dotnet restore
```

### الحزم المستخدمة:

| الحزمة | الإصدار | الغرض |
|--------|---------|-------|
| Dapper | 2.1.35 | ORM خفيف لـ SQL |
| Microsoft.Data.SqlClient | 5.2.1 | الاتصال بـ SQL Server |
| BCrypt.Net-Next | 4.0.3 | تشفير كلمات المرور |
| MaterialDesignThemes | 5.1.0 | واجهة Material Design |
| Microsoft.Extensions.Configuration | 7.0.0 | قراءة الإعدادات |

### البناء عبر سطر الأوامر:

```cmd
dotnet build
```

### إنتاج ملف EXE قابل للتوزيع (Windows):

```cmd
dotnet publish -r win-x64 --self-contained -c Release -o .\publish
```

الملف التنفيذي سيكون في مجلد `publish\`.

---

## ▶️ الخطوة الرابعة: تشغيل التطبيق

1. اضغط **F5** لتشغيل مع Debugging
2. أو اضغط **Ctrl+F5** لتشغيل بدون Debugging (أسرع)

---

## 🚀 أول تشغيل — بوابة المالك

عند تشغيل التطبيق **لأول مرة**، تفتح تلقائياً **بوابة مالك النظام** لإعداد:

1. **بيانات دخول المالك** — اسم المستخدم وكلمة المرور (تُحفظ مشفّرة في AppData)
2. **إعدادات المطعم** — الاسم والبيانات الأساسية
3. **إنشاء التراخيص** — توليد مفاتيح ترخيص للأجهزة

بعد الإعداد، يُطلب تفعيل **ترخيص الجهاز** قبل الدخول للنظام.

### الدخول للبوابة لاحقاً:

من شاشة تسجيل الدخول اضغط زر **"⚙ بوابة مالك النظام"** وأدخل بيانات المالك.

### بيانات الدخول الافتراضية (بعد تشغيل سكريبت قاعدة البيانات):

| المستخدم | كلمة المرور | الصلاحيات |
|----------|------------|----------|
| `admin`  | `admin123` | مدير — جميع الصفحات |
| `cashier1` | `admin123` | كاشير — نقطة البيع والمطبخ |

---

## 🎨 الثيم البصري (الإصدار 2.0)

تم التحويل إلى **ثيم أبيض/فاتح** احترافي:

| العنصر | القيمة |
|--------|-------|
| الخلفية العامة | `#F4F7FC` |
| البطاقات | `#FFFFFF` (أبيض) |
| شريط التنقل | `#1A2332` (أزرق داكن) |
| النص الرئيسي | `#1E293B` |
| النص الثانوي (Muted) | `#64748B` |
| الحدود | `#E2E8F0` |
| اللون المميز (Accent) | `#F7941D` (برتقالي itQAN) |
| اللون الأزرق | `#1B4E9E` |
| النجاح | `#22C55E` |
| الخطر | `#EF4444` |

---

## 🗂️ هيكل المشروع

```
RestaurantMS.Desktop/
├── 📄 RestaurantMS.Desktop.csproj    ← ملف المشروع (.NET 7)
├── 📄 appsettings.json               ← إعدادات الاتصال بقاعدة البيانات
├── 📄 App.xaml / App.xaml.cs         ← نقطة البدء + تعريف الأنماط والألوان
│
├── 📁 Assets/
│   └── itqansoft_logo.png            ← شعار itQAN Soft
│
├── 📁 Database/
│   └── setup_sqlserver.sql           ← سكريبت إنشاء قاعدة البيانات (20 جدول)
│
├── 📁 Data/
│   └── DbHelper.cs                   ← مساعد قاعدة البيانات (Dapper)
│
├── 📁 Models/
│   └── CurrentUser.cs                ← نموذج المستخدم الحالي في الجلسة
│
├── 📁 Services/
│   ├── LicenseManager.cs             ← إدارة التراخيص (تشفير AES)
│   ├── LicenseGenerator.cs           ← توليد مفاتيح الترخيص
│   ├── LicenseData.cs                ← نموذج بيانات الترخيص
│   ├── OwnerCredentialsManager.cs    ← إدارة بيانات المالك (مشفّرة في AppData)
│   └── DesktopShortcut.cs            ← إنشاء اختصار سطح المكتب
│
└── 📁 Views/
    ├── SetupWindow.xaml(.cs)          ← إعداد أول تشغيل (حساب المدير)
    ├── LicenseActivationWindow.xaml(.cs) ← تفعيل الترخيص
    ├── LoginWindow.xaml(.cs)          ← شاشة تسجيل الدخول
    ├── MainWindow.xaml(.cs)           ← النافذة الرئيسية + شريط التنقل
    │
    ├── 📁 Owner/
    │   └── OwnerPortalWindow.xaml(.cs) ← بوابة المالك الكاملة
    │
    ├── 📁 Dashboard/                  ← لوحة التحكم والإحصائيات
    ├── 📁 Pos/                        ← نقطة البيع + بحث العملاء
    ├── 📁 Kitchen/                    ← شاشة المطبخ وتتبع الطلبات
    ├── 📁 Menu/                       ← القائمة والتصنيفات
    ├── 📁 Inventory/                  ← المخزون وحركاته
    ├── 📁 Customers/                  ← العملاء وبرنامج الولاء
    ├── 📁 Suppliers/                  ← الموردون وطلبات الشراء
    ├── 📁 Sales/                      ← المبيعات والفواتير
    ├── 📁 Reservations/               ← الحجوزات
    ├── 📁 Reports/                    ← التقارير والإحصائيات
    └── 📁 Admin/                      ← المستخدمون والطاولات والإعدادات
```

---

## 📦 جداول قاعدة البيانات (20 جدول)

| الجدول | الوصف |
|--------|-------|
| `roles` | أدوار المستخدمين |
| `branches` | الفروع |
| `users` | المستخدمون وبيانات الدخول (بدون عمود email) |
| `settings` | إعدادات النظام |
| `menu_categories` | تصنيفات القائمة |
| `menu_items` | أصناف القائمة |
| `tables` | طاولات المطعم |
| `customers` | قاعدة بيانات العملاء |
| `loyalty_accounts` | حسابات نقاط الولاء |
| `orders` | الطلبات الرئيسية |
| `order_items` | عناصر كل طلب |
| `payments` | المدفوعات |
| `kitchen_orders` | طلبات المطبخ |
| `kitchen_order_items` | عناصر طلبات المطبخ |
| `reservations` | الحجوزات |
| `ingredients` | المواد المخزنية |
| `inventory_movements` | حركات المخزون |
| `suppliers` | الموردون |
| `purchase_orders` | طلبات الشراء |
| `audit_logs` | سجل الأحداث |

---

## 🚨 حل المشاكل الشائعة

### ❌ خطأ: "The current .NET SDK does not support targeting .NET 8.0"

**السبب:** تثبيت SDK قديم.

**الحل:** ثبِّت **.NET 7 SDK** من https://dotnet.microsoft.com/download/dotnet/7.0

---

### ❌ خطأ: "A network-related or instance-specific error"

**السبب:** SQL Server Express غير مشغّل أو اسم الـ instance خاطئ.

**الحل:**
1. افتح **Services** (اضغط `Win+R` واكتب `services.msc`)
2. ابحث عن **SQL Server (SQLEXPRESS)** وتأكد أنه **Running**

أو عبر CMD كـ Administrator:
```cmd
net start MSSQL$SQLEXPRESS
```

---

### ❌ خطأ: "Login failed for user"

**الحل:**
1. افتح SSMS → انقر بالزر الأيمن على السيرفر → **Properties → Security**
2. فعّل **SQL Server and Windows Authentication mode**
3. أعد تشغيل SQL Server Service

---

### ❌ خطأ: "Cannot find 'RestaurantMS' database"

**الحل:** نفِّذ سكريبت `Database\setup_sqlserver.sql` (الخطوة الأولى)

---

### ❌ مشكلة: ترخيص الجهاز مرفوض

**السبب:** مفتاح الترخيص مرتبط بجهاز مختلف.

**الحل:**
1. افتح بوابة المالك → قسم **التراخيص**
2. انسخ **معرف الجهاز** من شاشة تفعيل الترخيص
3. أنشئ ترخيصاً جديداً بهذا المعرف
4. أدخل مفتاح الترخيص الجديد في شاشة التفعيل

---

## 📞 الدعم التقني

**شركة itQAN Soft — لنظم المعلومات والحلول البرمجية**

> *"نبتكر الحلول — نتقن التنفيذ — نصنع الفرق"*

---

*© 2025 itQAN Soft. جميع الحقوق محفوظة.*
