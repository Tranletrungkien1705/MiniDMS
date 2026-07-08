# Hướng dẫn cài đặt MiniDMS

## Yêu cầu
- .NET 8 SDK: https://dotnet.microsoft.com/download
- SQL Server LocalDB (cài cùng Visual Studio) hoặc SQL Server 2019+
- Visual Studio 2022 hoặc VS Code

## Bước 1 — Clone & chuẩn bị

```bash
git clone https://github.com/Tranletrungkien17/MiniDMS.git
cd MiniDMS
```

## Bước 2 — EF Migration (tạo bảng Identity + app tables)

```bash
cd src/MiniDMS.Web
dotnet tool install --global dotnet-ef        # nếu chưa có
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> Hoặc dùng Package Manager Console trong Visual Studio:
> `Add-Migration InitialCreate` → `Update-Database`

## Bước 3 — Seed dữ liệu demo

Chạy file `database/002_seed.sql` trong SSMS (sau khi EF đã tạo database).

## Bước 4 — Chạy

```bash
dotnet run
# Mở: https://localhost:5001
```

## Tài khoản demo

| Email | Password | Vai trò |
|---|---|---|
| admin@minidms.com | Admin@123 | Admin |
| warehouse@minidms.com | Kho@123 | Kho |
| sales@minidms.com | Sales@123 | Bán hàng |
| accounting@minidms.com | Acc@123 | Kế toán |

## Thử tính năng Import Excel

1. Đăng nhập tài khoản Warehouse
2. Vào **Kho → Import Excel**
3. Tạo file Excel với cột: `SKU | Quantity | Note`
4. Upload → Hệ thống tự nhập kho

## Cấu trúc thư mục

```
MiniDMS/
├── src/MiniDMS.Web/
│   ├── Controllers/        ← RBAC guards ở đây
│   ├── Services/           ← Business logic
│   │   ├── StockService.cs    (Xuất-Nhập-Tồn)
│   │   ├── ExcelService.cs   (Import/Export EPPlus)
│   │   ├── OrderService.cs   (Đơn hàng, công nợ)
│   │   └── ReportService.cs  (Báo cáo)
│   ├── Models/Entities/    ← Domain models
│   ├── Data/               ← DbContext + Seeder
│   └── Views/              ← Razor UI
└── database/
    ├── 001_schema.sql
    └── 002_seed.sql
```
