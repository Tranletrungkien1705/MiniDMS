-- MiniDMS Database Schema
-- Run this BEFORE 002_seed.sql
-- Compatible: SQL Server 2019+ / LocalDB

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'MiniDMS')
    CREATE DATABASE MiniDMS;
GO

USE MiniDMS;
GO

-- ── Identity tables are created by EF Migrations.
-- ── Run: dotnet ef database update (from src/MiniDMS.Web)
-- ── Or use the seed-only approach below if you prefer raw SQL.

-- ProductCategories
IF OBJECT_ID('ProductCategories', 'U') IS NULL
CREATE TABLE ProductCategories (
    Id   INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(20)  NULL
);

-- Products
IF OBJECT_ID('Products', 'U') IS NULL
CREATE TABLE Products (
    Id                 INT IDENTITY PRIMARY KEY,
    SKU                NVARCHAR(50)    NOT NULL,
    Name               NVARCHAR(200)   NOT NULL,
    Description        NVARCHAR(1000)  NULL,
    ImageUrl           NVARCHAR(500)   NULL,
    CategoryId         INT             NOT NULL REFERENCES ProductCategories(Id),
    CostPrice          DECIMAL(18,2)   NOT NULL DEFAULT 0,
    SalePrice          DECIMAL(18,2)   NOT NULL DEFAULT 0,
    Unit               NVARCHAR(20)    NOT NULL DEFAULT N'cái',
    LowStockThreshold  INT             NOT NULL DEFAULT 5,
    IsActive           BIT             NOT NULL DEFAULT 1,
    CreatedAt          DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Products_SKU UNIQUE (SKU)
);

-- StockTransactions
IF OBJECT_ID('StockTransactions', 'U') IS NULL
CREATE TABLE StockTransactions (
    Id          INT IDENTITY PRIMARY KEY,
    ProductId   INT             NOT NULL REFERENCES Products(Id),
    Type        TINYINT         NOT NULL,   -- 0=In, 1=Out, 2=Adjust
    Quantity    INT             NOT NULL,
    Note        NVARCHAR(500)   NULL,
    RefNo       NVARCHAR(100)   NULL,
    CreatedBy   NVARCHAR(200)   NOT NULL,
    CreatedAt   DATETIME        NOT NULL DEFAULT GETDATE()
);

CREATE INDEX IX_StockTx_Product ON StockTransactions(ProductId, Type);
CREATE INDEX IX_StockTx_Date    ON StockTransactions(CreatedAt);

-- Customers
IF OBJECT_ID('Customers', 'U') IS NULL
CREATE TABLE Customers (
    Id           INT IDENTITY PRIMARY KEY,
    Code         NVARCHAR(30)   NOT NULL,
    Name         NVARCHAR(200)  NOT NULL,
    Phone        NVARCHAR(20)   NULL,
    Email        NVARCHAR(200)  NULL,
    Address      NVARCHAR(500)  NULL,
    DebtBalance  DECIMAL(18,2)  NOT NULL DEFAULT 0,
    CreatedAt    DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Customers_Code UNIQUE (Code)
);

-- Orders
IF OBJECT_ID('Orders', 'U') IS NULL
CREATE TABLE Orders (
    Id            INT IDENTITY PRIMARY KEY,
    OrderNo       NVARCHAR(50)   NOT NULL,
    CustomerId    INT            NOT NULL REFERENCES Customers(Id),
    OrderDate     DATETIME       NOT NULL DEFAULT GETDATE(),
    Status        TINYINT        NOT NULL DEFAULT 0,  -- 0=Draft,1=Confirmed,2=Delivered,3=Cancelled
    PaymentStatus TINYINT        NOT NULL DEFAULT 0,  -- 0=Unpaid,1=Partial,2=Paid
    TotalAmount   DECIMAL(18,2)  NOT NULL DEFAULT 0,
    PaidAmount    DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Note          NVARCHAR(500)  NULL,
    CreatedBy     NVARCHAR(200)  NOT NULL,
    CONSTRAINT UQ_Orders_OrderNo UNIQUE (OrderNo)
);

CREATE INDEX IX_Orders_Customer ON Orders(CustomerId);
CREATE INDEX IX_Orders_Date     ON Orders(OrderDate);

-- OrderLines
IF OBJECT_ID('OrderLines', 'U') IS NULL
CREATE TABLE OrderLines (
    Id         INT IDENTITY PRIMARY KEY,
    OrderId    INT           NOT NULL REFERENCES Orders(Id),
    ProductId  INT           NOT NULL REFERENCES Products(Id),
    Quantity   INT           NOT NULL,
    UnitPrice  DECIMAL(18,2) NOT NULL
);

PRINT 'Schema created OK';
GO
