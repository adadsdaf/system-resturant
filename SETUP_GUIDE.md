# دليل إعداد وتشغيل نظام إدارة المطاعم
## RestaurantMS Desktop — itQAN Soft
### إصدار 1.0 | .NET 8 WPF + SQL Server Express

---

## 📋 المتطلبات الأساسية قبل البدء

قبل فتح المشروع يجب أن يكون مثبتاً على جهازك:

| المطلب | الإصدار | رابط التنزيل |
|--------|---------|-------------|
| Visual Studio 2022 | 17.8 أو أحدث | https://visualstudio.microsoft.com |
| .NET 8 SDK | 8.0 أو أحدث | https://dotnet.microsoft.com/download |
| SQL Server Express | 2019 أو 2022 | https://www.microsoft.com/sql-server |
| SSMS (اختياري) | أي إصدار | https://aka.ms/ssmsfullsetup |

---

## 🗄️ الخطوة الأولى: إعداد قاعدة البيانات

### الطريقة 1: باستخدام SSMS (الأسهل)

1. افتح **SQL Server Management Studio (SSMS)**
2. اتصل بـ: `.\SQLEXPRESS` أو `localhost\SQLEXPRESS`
3. من القائمة: **File → Open → File**
4. اختر الملف: `RestaurantMS.Desktop\Database\setup_sqlserver.sql`
5. اضغط **Execute** أو `F5`
6. انتظر حتى ترى الرسالة:
   ```
   ✅ تم إنشاء قاعدة البيانات وتعبئة البيانات الأولية بنجاح!
   ```

### الطريقة 2: باستخدام سطر الأوامر (CMD)

```cmd
sqlcmd -S .\SQLEXPRESS -i "RestaurantMS.Desktop\Database\setup_sqlserver.sql"
```

### الطريقة 3: من داخل Visual Studio

1. افتح **View → SQL Server Object Explorer**
2. اتصل بـ `.\SQLEXPRESS`
3. انقر بالزر الأيمن على قاعدة البيانات → **New Query**
4. الصق محتوى ملف `setup_sqlserver.sql`
5. اضغط **Execute**

---

## 💻 الخطوة الثانية: فتح المشروع في Visual Studio 2022

### خطوات الفتح:

1. افتح **Visual Studio 2022**
2. اختر **Open a project or solution**
3. تصفح إلى مجلد:
   ```
   RestaurantMS.Desktop\RestaurantMS.Desktop.csproj
   ```
4. اختر الملف `.csproj` واضغط **Open**

### تثبيت حزم NuGet:

بعد فتح المشروع، سيقوم Visual Studio بتنزيل الحزم تلقائياً. إذا لم يحدث:
- افتح: **Tools → NuGet Package Manager → Manage NuGet Packages for Solution**
- اضغط **Restore**

أو عبر Package Manager Console:
```powershell
dotnet restore
```

### الحزم المستخدمة (تُثبّت تلقائياً):

| الحزمة | الإصدار | الغرض |
|--------|---------|-------|
| Dapper | 2.1.35 | ORM خفيف لـ SQL |
| Microsoft.Data.SqlClient | 5.2.1 | الاتصال بـ SQL Server |
| BCrypt.Net-Next | 4.0.3 | تشفير كلمات المرور |
| MaterialDesignThemes | 5.1.0 | واجهة Material Design |
| Microsoft.Extensions.Configuration | 8.0.0 | قراءة الإعدادات |

---

## ⚙️ الخطوة الثالثة: ضبط إعدادات الاتصال

افتح ملف `appsettings.json` في جذر المشروع:

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

### إذا كنت تستخدم SQL Authentication (مستخدم وكلمة مرور):

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=RestaurantMS;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

---

## ▶️ الخطوة الرابعة: تشغيل التطبيق

1. اضغط **F5** لتشغيل مع Debugging
2. أو اضغط **Ctrl+F5** لتشغيل بدون Debugging (أسرع)
3. أو اضغط على زر ▶️ في شريط الأدوات

### بيانات الدخول الافتراضية:

| المستخدم | كلمة المرور | الصلاحيات |
|----------|------------|----------|
| `admin`  | `admin123` | مدير - جميع الصفحات |
| `owner`  | `owner2025`| مالك - كامل الصلاحيات |
| `cashier1` | `admin123` | كاشير - نقطة البيع والمطبخ |

---

## 🚨 حل المشاكل الشائعة

### ❌ خطأ: "A network-related or instance-specific error"

**السبب:** SQL Server Express غير مشغّل أو اسم الـ instance خاطئ.

**الحل:**
1. افتح **Services** (اضغط `Win+R` واكتب `services.msc`)
2. ابحث عن **SQL Server (SQLEXPRESS)**
3. تأكد أنه **Running**
4. إذا كان موقوفاً، انقر عليه بالزر الأيمن → **Start**

أو عبر CMD كـ Administrator:
```cmd
net start MSSQL$SQLEXPRESS
```

### ❌ خطأ: "Login failed for user"

**السبب:** الـ Integrated Security غير مفعّل.

**الحل:**
1. افتح SSMS
2. انقر بالزر الأيمن على السيرفر → **Properties**
3. اختر **Security**
4. فعّل **SQL Server and Windows Authentication mode**
5. أعد تشغيل SQL Server Service

### ❌ خطأ: "Cannot find 'RestaurantMS' database"

**السبب:** لم يُنفَّذ سكريبت قاعدة البيانات.

**الحل:** عُد إلى الخطوة الأولى ونفِّذ `setup_sqlserver.sql`

### ❌ خطأ: "NuGet packages not restored"

**الحل:**
```powershell
# في Package Manager Console
Update-Package -reinstall

# أو عبر CMD في مجلد المشروع
dotnet restore
```

### ❌ خطأ: "The type or namespace 'BCrypt' could not be found"

**الحل:**
- انقر بالزر الأيمن على المشروع → **Manage NuGet Packages**
- ابحث عن `BCrypt.Net-Next`
- ثبِّت الإصدار `4.0.3`

### ❌ خطأ: "PresentationFramework not found" أو أخطاء WPF

**السبب:** SDK المثبّت لا يدعم Windows.

**الحل:**
1. افتح **Visual Studio Installer**
2. عدِّل Visual Studio 2022
3. تأكد من تثبيت Workload: **.NET desktop development**

---

## 🗂️ هيكل المشروع

```
RestaurantMS.Desktop/
├── 📄 RestaurantMS.Desktop.csproj    ← ملف المشروع الرئيسي
├── 📄 appsettings.json               ← إعدادات الاتصال
├── 📄 App.xaml / App.xaml.cs         ← نقطة بداية التطبيق + الأنماط
│
├── 📁 Assets/
│   └── itqansoft_logo.png            ← شعار itQAN Soft
│
├── 📁 Data/
│   └── DbHelper.cs                   ← مساعد قاعدة البيانات (Dapper)
│
├── 📁 Models/
│   └── CurrentUser.cs                ← نموذج المستخدم الحالي
│
├── 📁 Helpers/
│   └── PrintHelper.cs                ← طباعة الإيصالات والتذاكر
│
├── 📁 Database/
│   └── setup_sqlserver.sql           ← سكريبت إنشاء قاعدة البيانات
│
└── 📁 Views/
    ├── LoginWindow.xaml(.cs)          ← شاشة تسجيل الدخول
    ├── MainWindow.xaml(.cs)           ← النافذة الرئيسية + التنقل
    │
    ├── 📁 Dashboard/                  ← لوحة التحكم
    ├── 📁 Pos/                        ← نقطة البيع
    ├── 📁 Kitchen/                    ← إدارة المطبخ
    ├── 📁 Menu/                       ← القائمة والتصنيفات
    ├── 📁 Inventory/                  ← المخزون
    ├── 📁 Customers/                  ← العملاء والولاء
    ├── 📁 Suppliers/                  ← الموردون والمشتريات
    ├── 📁 Sales/                      ← المبيعات والفواتير
    ├── 📁 Reservations/               ← الحجوزات
    ├── 📁 Reports/                    ← التقارير
    └── 📁 Admin/                      ← الإدارة والإعدادات
```

---

## 📦 جداول قاعدة البيانات (20 جدول)

| الجدول | الوصف |
|--------|-------|
| `roles` | أدوار المستخدمين |
| `branches` | الفروع |
| `users` | المستخدمون وبيانات الدخول |
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
| `purchase_order_items` | عناصر طلبات الشراء |
| `audit_logs` | سجل الأحداث |

---

## 🎨 دليل الهوية البصرية — itQAN Soft

| العنصر | القيمة |
|--------|-------|
| اللون البرتقالي | `#F7941D` |
| اللون الأزرق | `#1B4E9E` |
| خلفية النظام | `#0d1117` |
| خلفية البطاقات | `#161b22` |
| لون النصوص الفرعية | `#8b949e` |
| لون الحدود | `#30363d` |
| لون النجاح | `#3fb950` |
| لون الخطأ | `#f85149` |

---

## 📞 الدعم التقني

**شركة itQAN Soft — لنظم المعلومات والحلول البرمجية**

> *"نبتكر الحلول — لنتقن التنفيذ — نصنع الفرق"*

---

*© 2025 itQAN Soft. جميع الحقوق محفوظة.*
