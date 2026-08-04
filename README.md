# MiniDMS — Mini Dealer Management System

> **Portfolio project** minh hoạ kiến trúc và kỹ thuật từ hệ thống quản lý bán hàng doanh nghiệp thực tế (phiên bản public, mã nguồn mở).

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4)](https://dotnet.microsoft.com)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2019+-CC2927)](https://www.microsoft.com/sql-server)
[![License: MIT](https://img.shields.io/badge/License-MIT-green)](LICENSE)

---

## Tính năng chính

| Module | Chức năng |
|---|---|
| **Quản lý sản phẩm (SKU)** | CRUD 400+ SKU, ảnh sản phẩm, phân loại, tìm kiếm |
| **Kho — Nhập (Stock In)** | Tạo phiếu nhập thủ công hoặc **import Excel hàng loạt** |
| **Kho — Xuất (Stock Out)** | Tạo phiếu xuất, gắn đơn bán hàng |
| **Tồn kho realtime** | Số dư theo SKU, cảnh báo ngưỡng tồn thấp |
| **Quản lý đơn hàng** | Tạo đơn, theo dõi trạng thái, lịch sử khách hàng |
| **CRM cơ bản** | Thông tin khách hàng, lịch sử giao dịch, công nợ |
| **Kế toán / Công nợ** | Theo dõi thanh toán, báo cáo doanh thu theo kỳ |
| **Báo cáo** | Export Excel — Admin tự thao tác, không cần IT |
| **RBAC** | 4 vai trò: Admin / Kho / Bán hàng / Kế toán |

---

## RBAC — Phân quyền theo vai trò

```
Admin       → Toàn quyền: user management + mọi module
Warehouse   → Nhập/Xuất kho, xem tồn kho, import Excel
Sales       → Tạo đơn hàng, xem sản phẩm, xem tồn kho (không edit)
Accounting  → Xem báo cáo công nợ/doanh thu, export Excel
```

Mỗi Controller/Action gắn `[Authorize(Roles = "...")]`. Không có quyền → redirect 403 với message rõ ràng.

---

## Kiến trúc

```
┌─────────────────────────────────────────────────────┐
│                  Presentation Layer                   │
│  ASP.NET Core 8 MVC · Razor Views · Bootstrap 5      │
├─────────────────────────────────────────────────────┤
│                   Service Layer                       │
│  ProductService · StockService · OrderService        │
│  ExcelService (EPPlus) · ReportService               │
├─────────────────────────────────────────────────────┤
│                   Data Layer                          │
│  Entity Framework Core 8 · SQL Server                │
│  Repository pattern · DbContext                      │
└─────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Web Framework | ASP.NET Core 8 MVC |
| ORM | Entity Framework Core 8 (Code-First) |
| Database | SQL Server 2019+ / LocalDB |
| Auth & RBAC | ASP.NET Core Identity + Role-based |
| Excel | EPPlus 7 (import/export) |
| UI | Bootstrap 5 + Bootstrap Icons |
| Charts | Chart.js (dashboard) |
| Language | C# 12 |

---

## Cài đặt & Chạy

### Yêu cầu
- .NET 8 SDK
- SQL Server 2019+ hoặc SQL Server LocalDB
- Visual Studio 2022 hoặc VS Code

### Bước 1 — Clone
```bash
git clone https://github.com/Tranletrungkien1705/MiniDMS.git
cd MiniDMS
```

### Bước 2 — Database
```bash
# Chạy script tạo schema + seed data
sqlcmd -S "(localdb)\MSSQLLocalDB" -i database/001_schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -i database/002_seed.sql
```

Hoặc mở `database/001_schema.sql` trong SSMS và chạy.

### Bước 3 — Cấu hình connection string
Sửa `src/MiniDMS.Web/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MiniDMS;Trusted_Connection=True;"
  }
}
```

### Bước 4 — Chạy
```bash
cd src/MiniDMS.Web
dotnet run
```
Mở trình duyệt: `https://localhost:5001`

### Tài khoản demo mặc định

| Role | Username | Password |
|---|---|---|
| Admin | admin@minidms.com | Admin@123 |
| Kho | warehouse@minidms.com | Kho@123 |
| Bán hàng | sales@minidms.com | Sales@123 |
| Kế toán | accounting@minidms.com | Acc@123 |

---

## Import Excel — Mẫu file

File mẫu import kho: `database/templates/StockImport_Template.xlsx`

Cột bắt buộc: `SKU | ProductName | Quantity | Unit | Note`

Admin/Warehouse tự upload — không cần IT can thiệp.

---

## Screenshots

> *(Xem demo video: [YouTube / Loom link])*

---

## License

MIT — tự do sử dụng, học tập, tham khảo.

---

## Liên hệ

**Kien Tran Le Trung** · kientlt59@gmail.com · [github.com/Tranletrungkien17](https://github.com/Tranletrungkien17)
