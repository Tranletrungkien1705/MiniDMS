-- MiniDMS Seed Data
-- Run AFTER 001_schema.sql AND after EF Identity migration

USE MiniDMS;
GO

-- Categories
IF NOT EXISTS (SELECT 1 FROM ProductCategories)
BEGIN
    INSERT INTO ProductCategories (Name, Code) VALUES
        (N'Áo',       N'AO'),
        (N'Quần',     N'QUAN'),
        (N'Đầm váy',  N'DAM'),
        (N'Phụ kiện', N'PK');
    PRINT 'Categories seeded';
END
GO

-- Products (50 mẫu)
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    DECLARE @ao   INT = (SELECT Id FROM ProductCategories WHERE Code = 'AO');
    DECLARE @quan INT = (SELECT Id FROM ProductCategories WHERE Code = 'QUAN');
    DECLARE @dam  INT = (SELECT Id FROM ProductCategories WHERE Code = 'DAM');
    DECLARE @pk   INT = (SELECT Id FROM ProductCategories WHERE Code = 'PK');

    INSERT INTO Products (SKU, Name, CategoryId, CostPrice, SalePrice, Unit) VALUES
    (N'AO-001', N'Áo sơ mi trắng basic nam',    @ao,   120000, 259000, N'cái'),
    (N'AO-002', N'Áo polo nam cotton',           @ao,   150000, 329000, N'cái'),
    (N'AO-003', N'Áo thun oversize unisex',      @ao,    80000, 199000, N'cái'),
    (N'AO-004', N'Áo khoác denim nữ',            @ao,   250000, 549000, N'cái'),
    (N'AO-005', N'Áo blazer công sở nữ',         @ao,   350000, 749000, N'cái'),
    (N'QUAN-001', N'Quần jeans slim nam',         @quan, 200000, 459000, N'cái'),
    (N'QUAN-002', N'Quần kaki công sở nam',       @quan, 180000, 399000, N'cái'),
    (N'QUAN-003', N'Quần short thể thao',         @quan,  90000, 199000, N'cái'),
    (N'QUAN-004', N'Quần âu nữ ống rộng',        @quan, 220000, 479000, N'cái'),
    (N'DAM-001', N'Đầm maxi hoa nhí',            @dam,  200000, 449000, N'cái'),
    (N'DAM-002', N'Váy midi công sở',            @dam,  250000, 529000, N'cái'),
    (N'PK-001', N'Thắt lưng da nam',             @pk,    80000, 189000, N'cái'),
    (N'PK-002', N'Túi tote vải canvas',          @pk,   100000, 229000, N'cái'),
    (N'PK-003', N'Mũ bucket unisex',             @pk,    60000, 149000, N'cái'),
    (N'PK-004', N'Tất cotton basic 3 đôi',       @pk,    30000,  79000, N'set');

    PRINT 'Products seeded';
END
GO

-- Customers demo
IF NOT EXISTS (SELECT 1 FROM Customers)
BEGIN
    INSERT INTO Customers (Code, Name, Phone, Email, Address, DebtBalance) VALUES
    (N'KH001', N'Nguyễn Thị Lan',   N'0901234567', N'lan@gmail.com',   N'Hà Nội',   0),
    (N'KH002', N'Trần Văn Minh',    N'0912345678', N'minh@gmail.com',  N'TP.HCM',   500000),
    (N'KH003', N'Lê Thu Hương',     N'0923456789', N'huong@gmail.com', N'Đà Nẵng',  0),
    (N'KH004', N'Shop Thời Trang A',N'0934567890', NULL,               N'Hải Phòng',1200000);
    PRINT 'Customers seeded';
END
GO

-- Stock transactions demo
IF NOT EXISTS (SELECT 1 FROM StockTransactions)
BEGIN
    -- Nhập kho ban đầu
    INSERT INTO StockTransactions (ProductId, Type, Quantity, Note, RefNo, CreatedBy, CreatedAt)
    SELECT p.Id, 0, 100, N'Nhập kho ban đầu', N'NK-INIT-001', N'admin@minidms.com', GETDATE()
    FROM Products p;

    -- Xuất một số mặt hàng
    INSERT INTO StockTransactions (ProductId, Type, Quantity, Note, RefNo, CreatedBy, CreatedAt)
    SELECT TOP 5 Id, 1, 15, N'Xuất bán lẻ tháng 6', N'ORD-240601', N'sales@minidms.com', GETDATE()
    FROM Products ORDER BY NEWID();

    PRINT 'Stock transactions seeded';
END
GO

PRINT 'Seed completed';
